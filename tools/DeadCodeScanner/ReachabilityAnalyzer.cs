using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeadCodeScanner;

public class ReachabilityAnalyzer {
    public AnalysisResult Analyze(CSharpCompilation prodCompilation, CSharpCompilation testCompilation) {
        var prodMethods = new Dictionary<ISymbol, MethodNode>(SymbolEqualityComparer.Default);
        var outgoingCalls = new Dictionary<ISymbol, HashSet<ISymbol>>(SymbolEqualityComparer.Default);
        var incomingCalls = new Dictionary<ISymbol, HashSet<ISymbol>>(SymbolEqualityComparer.Default);
        var interfaceImplementations = new Dictionary<ISymbol, List<ISymbol>>(SymbolEqualityComparer.Default);
        var methodOverrides = new Dictionary<ISymbol, List<ISymbol>>(SymbolEqualityComparer.Default);

        void RegisterMethod(IMethodSymbol? methodSymbol) {
            if (methodSymbol == null) return;
            var original = methodSymbol.OriginalDefinition;
            if (prodMethods.ContainsKey(original)) return;

            var location = original.Locations.FirstOrDefault();
            var lineSpan = location?.GetLineSpan();
            var filePath = lineSpan?.Path ?? "";
            if (IsIgnoredSourceFile(filePath)) return;

            var line = (lineSpan?.StartLinePosition.Line ?? 0) + 1;
            bool isRoot = IsProductionRoot(original);
            string methodId = $"{original.ToDisplayString()}@{filePath}:{line}";
            var methodNode = new MethodNode(
                methodId,
                original.Name,
                original.ContainingType?.ToDisplayString() ?? "",
                filePath,
                line,
                isRoot
            );

            prodMethods[original] = methodNode;
            outgoingCalls[original] = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            incomingCalls[original] = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        }

        // 1. Collect all declared methods, constructors, and properties
        var allNamedTypes = new List<INamedTypeSymbol>();
        foreach (var tree in prodCompilation.SyntaxTrees) {
            if (IsIgnoredSourceFile(tree.FilePath)) continue;
            var model = prodCompilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var node in root.DescendantNodes()) {
                if (node is MethodDeclarationSyntax methodDecl) {
                    RegisterMethod(model.GetDeclaredSymbol(methodDecl));
                } else if (node is ConstructorDeclarationSyntax ctorDecl) {
                    RegisterMethod(model.GetDeclaredSymbol(ctorDecl));
                } else if (node is PropertyDeclarationSyntax propDecl) {
                    var propSymbol = model.GetDeclaredSymbol(propDecl);
                    if (propSymbol != null) {
                        RegisterMethod(propSymbol.GetMethod);
                        RegisterMethod(propSymbol.SetMethod);
                    }
                } else if (node is ClassDeclarationSyntax or StructDeclarationSyntax or InterfaceDeclarationSyntax) {
                    var typeSymbol = model.GetDeclaredSymbol(node) as INamedTypeSymbol;
                    if (typeSymbol != null) {
                        allNamedTypes.Add(typeSymbol);
                    }
                }
            }
        }

        // Register static constructors and implicit constructors for named types
        foreach (var type in allNamedTypes) {
            if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) {
                if (type.StaticConstructors.FirstOrDefault() is { } staticCtor) {
                    RegisterMethod(staticCtor);
                }
                foreach (var ctor in type.InstanceConstructors) {
                    if (ctor.IsImplicitlyDeclared) {
                        RegisterMethod(ctor);
                    }
                }
            }
        }

        // Index interface implementations and virtual overrides
        foreach (var type in allNamedTypes) {
            if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) {
                foreach (var iface in type.AllInterfaces) {
                    foreach (var member in iface.GetMembers().OfType<IMethodSymbol>()) {
                        var impl = type.FindImplementationForInterfaceMember(member) as IMethodSymbol;
                        if (impl != null) {
                            var ifaceOrig = member.OriginalDefinition;
                            var implOrig = impl.OriginalDefinition;
                            if (!interfaceImplementations.TryGetValue(ifaceOrig, out var implList)) {
                                implList = new List<ISymbol>();
                                interfaceImplementations[ifaceOrig] = implList;
                            }
                            implList.Add(implOrig);
                        }
                    }
                }

                // Overrides
                foreach (var member in type.GetMembers().OfType<IMethodSymbol>()) {
                    if (member.IsOverride && member.OverriddenMethod != null) {
                        var baseOrig = member.OverriddenMethod.OriginalDefinition;
                        if (!methodOverrides.TryGetValue(baseOrig, out var overrideList)) {
                            overrideList = new List<ISymbol>();
                            methodOverrides[baseOrig] = overrideList;
                        }
                        overrideList.Add(member.OriginalDefinition);
                    }
                }
            }
        }

        // 2. Build production call graph
        void AddCallEdge(ISymbol caller, ISymbol target) {
            if (prodMethods.ContainsKey(target) && !SymbolEqualityComparer.Default.Equals(caller, target)) {
                outgoingCalls[caller].Add(target);
                incomingCalls[target].Add(caller);
            }

            // If target is an interface member, add edges to its implementations
            if (interfaceImplementations.TryGetValue(target, out var implementations)) {
                foreach (var impl in implementations) {
                    if (prodMethods.ContainsKey(impl) && !SymbolEqualityComparer.Default.Equals(caller, impl)) {
                        outgoingCalls[caller].Add(impl);
                        incomingCalls[impl].Add(caller);
                    }
                }
            }

            // If target is overridden, add edges to derived implementations
            if (methodOverrides.TryGetValue(target, out var overrides)) {
                foreach (var ovr in overrides) {
                    if (prodMethods.ContainsKey(ovr) && !SymbolEqualityComparer.Default.Equals(caller, ovr)) {
                        outgoingCalls[caller].Add(ovr);
                        incomingCalls[ovr].Add(caller);
                    }
                }
            }
        }

        // Wire class initialization to static constructor
        foreach (var type in allNamedTypes) {
            if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) {
                if (type.StaticConstructors.FirstOrDefault() is { } staticCtor) {
                    var staticCtorOrig = staticCtor.OriginalDefinition;
                    // Calling any instance constructor initializes the class
                    foreach (var ctor in type.InstanceConstructors) {
                        AddCallEdge(ctor.OriginalDefinition, staticCtorOrig);
                    }
                    // Calling any static method initializes the class
                    foreach (var member in type.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsStatic && m.MethodKind == MethodKind.Ordinary)) {
                        AddCallEdge(member.OriginalDefinition, staticCtorOrig);
                    }
                    // Calling any static property initializes the class
                    foreach (var prop in type.GetMembers().OfType<IPropertySymbol>().Where(p => p.IsStatic)) {
                        if (prop.GetMethod != null) AddCallEdge(prop.GetMethod.OriginalDefinition, staticCtorOrig);
                        if (prop.SetMethod != null) AddCallEdge(prop.SetMethod.OriginalDefinition, staticCtorOrig);
                    }
                }
            }
        }

        void RecordCallsFromNode(ISymbol callerOriginal, SyntaxNode node, SemanticModel model) {
            foreach (var callSite in node.DescendantNodesAndSelf()) {
                ISymbol? target = null;
                if (callSite is InvocationExpressionSyntax invocation) {
                    target = model.GetSymbolInfo(invocation).Symbol;
                } else if (callSite is ObjectCreationExpressionSyntax creation) {
                    target = model.GetSymbolInfo(creation).Symbol;
                } else if (callSite is ConstructorInitializerSyntax init) {
                    target = model.GetSymbolInfo(init).Symbol;
                } else if (callSite is IdentifierNameSyntax or MemberAccessExpressionSyntax) {
                    target = model.GetSymbolInfo(callSite).Symbol;
                }

                if (target is IMethodSymbol targetMethod) {
                    var actualMethod = targetMethod.ReducedFrom ?? targetMethod;
                    AddCallEdge(callerOriginal, actualMethod.OriginalDefinition);
                } else if (target is IPropertySymbol targetProp) {
                    bool isAssignmentTarget = callSite.Parent is AssignmentExpressionSyntax assign && assign.Left == callSite;
                    var propMethod = isAssignmentTarget ? targetProp.SetMethod : targetProp.GetMethod;
                    if (propMethod != null) {
                        AddCallEdge(callerOriginal, propMethod.OriginalDefinition);
                    }
                } else if (target is IFieldSymbol targetField && targetField.IsStatic && targetField.ContainingType != null) {
                    if (targetField.ContainingType.StaticConstructors.FirstOrDefault() is { } fieldStaticCtor) {
                        AddCallEdge(callerOriginal, fieldStaticCtor.OriginalDefinition);
                    }
                }
            }
        }

        foreach (var tree in prodCompilation.SyntaxTrees) {
            if (IsIgnoredSourceFile(tree.FilePath)) continue;
            var model = prodCompilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var node in root.DescendantNodes()) {
                if (node is FieldDeclarationSyntax fieldDecl) {
                    bool isStatic = fieldDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
                    foreach (var variable in fieldDecl.Declaration.Variables) {
                        if (variable.Initializer == null) continue;
                        var fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                        var containingType = fieldSymbol?.ContainingType;
                        if (containingType == null) continue;

                        if (isStatic) {
                            if (containingType.StaticConstructors.FirstOrDefault() is { } staticCtor) {
                                RecordCallsFromNode(staticCtor.OriginalDefinition, variable.Initializer.Value, model);
                            }
                        } else {
                            foreach (var ctor in containingType.InstanceConstructors) {
                                RecordCallsFromNode(ctor.OriginalDefinition, variable.Initializer.Value, model);
                            }
                        }
                    }
                } else if (node is PropertyDeclarationSyntax propDecl && propDecl.Initializer != null) {
                    bool isStatic = propDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
                    var propSymbol = model.GetDeclaredSymbol(propDecl);
                    var containingType = propSymbol?.ContainingType;
                    if (containingType != null) {
                        if (isStatic) {
                            if (containingType.StaticConstructors.FirstOrDefault() is { } staticCtor) {
                                RecordCallsFromNode(staticCtor.OriginalDefinition, propDecl.Initializer.Value, model);
                            }
                        } else {
                            foreach (var ctor in containingType.InstanceConstructors) {
                                RecordCallsFromNode(ctor.OriginalDefinition, propDecl.Initializer.Value, model);
                            }
                        }
                    }
                } else {
                    IMethodSymbol? callerSymbol = null;
                    if (node is MethodDeclarationSyntax methodDecl) {
                        callerSymbol = model.GetDeclaredSymbol(methodDecl);
                    } else if (node is ConstructorDeclarationSyntax ctorDecl) {
                        callerSymbol = model.GetDeclaredSymbol(ctorDecl);
                    } else if (node is AccessorDeclarationSyntax accessorDecl) {
                        callerSymbol = model.GetDeclaredSymbol(accessorDecl);
                    } else if (node is ArrowExpressionClauseSyntax && node.Parent is PropertyDeclarationSyntax parentPropDecl) {
                        var propSymbol = model.GetDeclaredSymbol(parentPropDecl);
                        callerSymbol = propSymbol?.GetMethod;
                    }

                    if (callerSymbol != null && prodMethods.ContainsKey(callerSymbol.OriginalDefinition)) {
                        RecordCallsFromNode(callerSymbol.OriginalDefinition, node, model);
                    }
                }
            }
        }

        // 3. Reachability from True Roots
        var queue = new Queue<ISymbol>();
        var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        foreach (var (symbol, node) in prodMethods) {
            if (node.IsRoot) {
                queue.Enqueue(symbol);
                visited.Add(symbol);
            }
        }

        while (queue.Count > 0) {
            var current = queue.Dequeue();
            if (outgoingCalls.TryGetValue(current, out var targets)) {
                foreach (var target in targets) {
                    if (visited.Add(target)) {
                        queue.Enqueue(target);
                    }
                }
            }
        }

        var liveMethods = new List<MethodNode>();
        var deadMethods = new List<MethodNode>();
        var deadSymbolsSet = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        foreach (var (symbol, node) in prodMethods) {
            if (visited.Contains(symbol)) {
                if (symbol is IMethodSymbol ms && ms.IsImplicitlyDeclared) {
                    continue;
                }
                liveMethods.Add(node);
            } else {
                if (symbol is IMethodSymbol ms && ms.IsImplicitlyDeclared) {
                    continue;
                }
                if (IsIgnoredSourceFile(node.FilePath)) {
                    continue;
                }
                deadMethods.Add(node);
                deadSymbolsSet.Add(symbol);
            }
        }

        // 4. Scan test compilation for calls to dead methods
        var deadMethodsByDisplayString = deadMethods.ToLookup(m => m.Id.Split('@')[0]);
        var testCallsToDead = new Dictionary<string, HashSet<MethodNode>>();
        foreach (var tree in testCompilation.SyntaxTrees) {
            var model = testCompilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var node in root.DescendantNodes()) {
                IMethodSymbol? testCallerSymbol = null;
                if (node is MethodDeclarationSyntax testMethodDecl) {
                    testCallerSymbol = model.GetDeclaredSymbol(testMethodDecl);
                }

                if (testCallerSymbol == null) {
                    continue;
                }

                var location = testCallerSymbol.Locations.FirstOrDefault();
                var lineSpan = location?.GetLineSpan();
                var filePath = lineSpan?.Path ?? "";
                var line = (lineSpan?.StartLinePosition.Line ?? 0) + 1;

                var testCallerNode = new MethodNode(
                    $"{testCallerSymbol.ToDisplayString()}@{filePath}:{line}",
                    testCallerSymbol.Name,
                    testCallerSymbol.ContainingType?.ToDisplayString() ?? "",
                    filePath,
                    line,
                    false
                );

                foreach (var callSite in node.DescendantNodes()) {
                    ISymbol? target = null;
                    if (callSite is InvocationExpressionSyntax invocation) {
                        target = model.GetSymbolInfo(invocation).Symbol;
                    } else if (callSite is ObjectCreationExpressionSyntax creation) {
                        target = model.GetSymbolInfo(creation).Symbol;
                    } else if (callSite is ConstructorInitializerSyntax init) {
                        target = model.GetSymbolInfo(init).Symbol;
                    } else if (callSite is IdentifierNameSyntax or MemberAccessExpressionSyntax) {
                        target = model.GetSymbolInfo(callSite).Symbol;
                    }

                    if (target is IPropertySymbol targetProp) {
                        bool isAssignmentTarget = callSite.Parent is AssignmentExpressionSyntax assign && assign.Left == callSite;
                        target = isAssignmentTarget ? targetProp.SetMethod : targetProp.GetMethod;
                    }

                    if (target is IMethodSymbol targetMethod) {
                        var actualMethod = targetMethod.ReducedFrom ?? targetMethod;
                        string targetDisplay = actualMethod.OriginalDefinition.ToDisplayString();
                        if (deadMethodsByDisplayString.Contains(targetDisplay)) {
                            foreach (var deadNode in deadMethodsByDisplayString[targetDisplay]) {
                                if (!testCallsToDead.TryGetValue(deadNode.Id, out var testSet)) {
                                    testSet = new HashSet<MethodNode>();
                                    testCallsToDead[deadNode.Id] = testSet;
                                }
                                testSet.Add(testCallerNode);
                            }
                        }
                    }
                }
            }
        }

        // 5. Cluster dead symbols into weakly connected components (Zombie Islands)
        var islands = new List<ZombieIsland>();
        var isolatedDead = new List<MethodNode>();
        var unvisitedDead = new HashSet<ISymbol>(deadSymbolsSet, SymbolEqualityComparer.Default);

        while (unvisitedDead.Count > 0) {
            var start = unvisitedDead.First();
            var component = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var compQueue = new Queue<ISymbol>();
            compQueue.Enqueue(start);
            component.Add(start);
            unvisitedDead.Remove(start);

            while (compQueue.Count > 0) {
                var current = compQueue.Dequeue();

                // Explore outgoing dead edges
                if (outgoingCalls.TryGetValue(current, out var outTargets)) {
                    foreach (var target in outTargets) {
                        if (deadSymbolsSet.Contains(target) && component.Add(target)) {
                            unvisitedDead.Remove(target);
                            compQueue.Enqueue(target);
                        }
                    }
                }

                // Explore incoming dead edges
                if (incomingCalls.TryGetValue(current, out var inCallers)) {
                    foreach (var caller in inCallers) {
                        if (deadSymbolsSet.Contains(caller) && component.Add(caller)) {
                            unvisitedDead.Remove(caller);
                            compQueue.Enqueue(caller);
                        }
                    }
                }
            }

            // In this component, find roots (nodes with 0 incoming calls from other nodes in this component)
            var componentRoots = new List<MethodNode>();
            var componentCascade = new List<MethodNode>();
            var componentLifeSupport = new HashSet<MethodNode>();

            foreach (var member in component) {
                var memberNode = prodMethods[member];
                int inDegreeFromComponent = 0;
                if (incomingCalls.TryGetValue(member, out var callers)) {
                    inDegreeFromComponent = callers.Count(c => component.Contains(c));
                }

                if (inDegreeFromComponent == 0) {
                    componentRoots.Add(memberNode);
                } else {
                    componentCascade.Add(memberNode);
                }

                if (testCallsToDead.TryGetValue(memberNode.Id, out var testCallers)) {
                    foreach (var tc in testCallers) {
                        componentLifeSupport.Add(tc);
                    }
                }
            }

            // If there's a pure cycle and no in-degree 0 node, promote one to root
            if (componentRoots.Count == 0 && componentCascade.Count > 0) {
                componentRoots.Add(componentCascade[0]);
                componentCascade.RemoveAt(0);
            }

            // Only report as ZombieIsland if it has internal cascade or test callers keeping it alive
            if (componentCascade.Count > 0 || componentLifeSupport.Count > 0) {
                string islandName = componentRoots.Count == 1
                    ? $"{componentRoots[0].ContainingType}.{componentRoots[0].Name}"
                    : (componentRoots.FirstOrDefault()?.ContainingType ?? component.First().ToDisplayString());

                islands.Add(new ZombieIsland(
                    islandName,
                    componentRoots,
                    componentCascade,
                    componentLifeSupport.ToList()
                ));
            } else {
                foreach (var root in componentRoots) {
                    isolatedDead.Add(root);
                }
            }
        }

        return new AnalysisResult(liveMethods, deadMethods, islands, isolatedDead);
    }

    private static bool IsIgnoredSourceFile(string filePath) {
        if (string.IsNullOrEmpty(filePath)) return false;
        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("NullableAttributes.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("Resources.Designer.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("Settings.Designer.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> CoreDomainEngineInterfaces = new(StringComparer.Ordinal) {
        "ICharacterSession",
        "IRulesCatalog",
        "ISubsystemEngine"
    };

    private static readonly HashSet<string> CoreDomainEngineImplementations = new(StringComparer.Ordinal) {
        "CharacterSession",
        "GameRulesCatalog",
        "SubsystemEngine"
    };

    private static readonly string[] CoreDomainEngineImplementationNames = {
        "CharacterSession",
        "GameRulesCatalog",
        "SubsystemEngine"
    };

    private static bool IsProductionRoot(IMethodSymbol symbol) {
        // Main entry point
        if (symbol.Name == "Main" && symbol.IsStatic) {
            return true;
        }

        // Domain engine contracts and implementations
        if (IsDomainEngineRoot(symbol)) {
            return true;
        }

        // Authorized domain API annotation or contract
        if (HasAuthorizedDomainApi(symbol)) {
            return true;
        }

        var containingType = symbol.ContainingType;
        if (containingType != null) {
            bool isForm = containingType.Name.EndsWith("Form") || IsSubclassOf(containingType, "Form");

            // Top-level Form constructors (like MainForm) are entry points
            if (isForm && symbol.MethodKind == MethodKind.Constructor && symbol.DeclaredAccessibility == Accessibility.Public) {
                return true;
            }

            // Lifecycle overrides on Forms/Controls (e.g. OnLoad, OnClosing, Dispose)
            if (symbol.IsOverride && (isForm || IsSubclassOf(containingType, "Control", "Component"))) {
                return true;
            }

            // Standard object overrides on public types (Equals, GetHashCode, ToString)
            if (symbol.IsOverride && symbol.DeclaredAccessibility == Accessibility.Public &&
                (symbol.Name is "Equals" or "GetHashCode" or "ToString")) {
                return true;
            }
        }

        return false;
    }

    private static bool IsDomainEngineRoot(IMethodSymbol symbol) {
        var containingType = symbol.ContainingType;
        if (containingType == null) return false;

        // 1. Declared on a core domain engine interface
        if (containingType.TypeKind == TypeKind.Interface) {
            if (CoreDomainEngineInterfaces.Contains(containingType.Name) ||
                containingType.AllInterfaces.Any(i => CoreDomainEngineInterfaces.Contains(i.Name))) {
                return symbol.DeclaredAccessibility == Accessibility.Public;
            }
        }

        // 2. Declared on a core domain engine implementation class
        if (CoreDomainEngineImplementations.Contains(containingType.Name) ||
            IsSubclassOf(containingType, CoreDomainEngineImplementationNames)) {
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        // 3. Implements a member of a core domain engine interface
        if (symbol.DeclaredAccessibility == Accessibility.Public || symbol.ExplicitInterfaceImplementations.Length > 0) {
            if (symbol.ExplicitInterfaceImplementations.Any(m => CoreDomainEngineInterfaces.Contains(m.ContainingType?.Name ?? ""))) {
                return true;
            }

            foreach (var iface in containingType.AllInterfaces) {
                if (CoreDomainEngineInterfaces.Contains(iface.Name)) {
                    foreach (var member in iface.GetMembers()) {
                        if (member is IMethodSymbol methodMember) {
                            var impl = containingType.FindImplementationForInterfaceMember(methodMember);
                            if (impl != null && SymbolEqualityComparer.Default.Equals(impl.OriginalDefinition, symbol)) {
                                return true;
                            }
                        } else if (member is IPropertySymbol propMember) {
                            if (propMember.GetMethod != null) {
                                var impl = containingType.FindImplementationForInterfaceMember(propMember.GetMethod);
                                if (impl != null && SymbolEqualityComparer.Default.Equals(impl.OriginalDefinition, symbol)) {
                                    return true;
                                }
                            }
                            if (propMember.SetMethod != null) {
                                var impl = containingType.FindImplementationForInterfaceMember(propMember.SetMethod);
                                if (impl != null && SymbolEqualityComparer.Default.Equals(impl.OriginalDefinition, symbol)) {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool HasAuthorizedDomainApi(IMethodSymbol symbol) {
        if (HasAuthorizedDomainApiAttribute(symbol)) {
            return true;
        }

        var containingType = symbol.ContainingType;
        if (containingType != null) {
            if (HasAuthorizedDomainApiAttribute(containingType)) {
                return true;
            }

            foreach (var iface in containingType.AllInterfaces) {
                if (HasAuthorizedDomainApiAttribute(iface)) {
                    var ifaceMethod = iface.GetMembers().OfType<IMethodSymbol>()
                        .FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(m)?.OriginalDefinition, symbol));
                    if (ifaceMethod != null) {
                        return true;
                    }
                } else {
                    var ifaceMethod = iface.GetMembers().OfType<IMethodSymbol>()
                        .FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(m)?.OriginalDefinition, symbol));
                    if (ifaceMethod != null && HasAuthorizedDomainApiAttribute(ifaceMethod)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool HasAuthorizedDomainApiAttribute(ISymbol symbol) {
        if (symbol.GetAttributes().Any(a =>
            a.AttributeClass != null &&
            (a.AttributeClass.Name == "AuthorizedDomainApiAttribute" || a.AttributeClass.Name == "AuthorizedDomainApi"))) {
            return true;
        }

        if (symbol is IMethodSymbol method && method.AssociatedSymbol != null) {
            return HasAuthorizedDomainApiAttribute(method.AssociatedSymbol);
        }

        return false;
    }

    private static bool IsSubclassOf(INamedTypeSymbol type, params string[] baseTypeNames) {
        var current = type.BaseType;
        while (current != null) {
            if (baseTypeNames.Contains(current.Name)) {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
