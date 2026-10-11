# Coding Standards

## Code Architecture and Test Seams

- **Preserve UI encapsulation**: Forms and presentation controls must not expose mutable state setters solely to satisfy unit test fixtures. Inject dependencies via constructor overloads (e.g. `MainForm(ICharacterSession session) : this()`) or test-visible factories.
- **Explicit constructor deserialization and schema stability (No shims)**: Domain types implement `IJsonable` with explicit `new Type(JObject)` constructors. Never introduce JSON fallbacks, adapter shims, or deprecated forwarding constructors (see `docs/adr/0008-explicit-constructor-deserialization-and-schema-stability.md`).

## WinForms and `Designer.cs` Layout

- **`ScrollableControl` layout resets**: Before calling `Controls.Clear()` or toggling `AutoScroll` inside `SuspendLayout()`, explicitly reset `AutoScrollPosition = Point.Empty` and `AutoScroll = false`. Otherwise, WinForms offsets newly added child control coordinates relative to the negative `DisplayRectangle.Y` scroll position, causing rows to drift out of bounds when `AutoScroll` is later disabled.
- **Sibling bounds and Z-order in `*.Designer.cs`**: When placing or resizing controls on a `TabPage` or `Panel`, verify bounding-box coordinates (`Location` and `Size`) against sibling controls on the same parent and check `Controls.Add(...)` Z-order so top-anchored or expanding panels are not occluded.
- **Navigating `*.Designer.cs`**: `MainForm.Designer.cs` separates control initialization from field declarations by ~1,800 lines. Run `git grep -n "this.<controlName>" -- PlayMore35/UI/MainForm.Designer.cs` first to locate exact line numbers rather than reading the file in sequential slices.
- **`.csproj` `<SubType>` metadata**: Every `Form`, `UserControl`, or `Component` file added to `PlayMore35/PlayMore35.csproj` must include the matching `<SubType>` child element (`Form`, `UserControl`, or `Component`) so Visual Studio does not dirty the project file on open (enforced by `BaselineTests.PlayMore35Csproj_WinFormsTypesHaveExpectedSubType`).

## Testing Performance

- **Lightweight control hosts for resize/layout loops**: Instantiating `new MainForm(session)` parses `rules.json`, builds 28 `DynamicPanel`s, and loads custom fonts. Host custom `UserControl`s or `Panel`s inside a bare `new Form()` for multi-step resize or scroll unit tests; reserve `new MainForm(session)` for coarse integration assertions (1–3 resize steps).

## Rules Provenance in Code and Tests

- **Applies to mechanics only**: Binds actual game rules logic (math formulas, stacking, prerequisites, modifiers, subsystem interactions, and `rules.json` entries). Does not apply to application presentation schema or UI layout categorization.
- **Never fabricate citations**: Citations must come exclusively from user-supplied primary sources in the plan or issue. Unit tests validating a game rule must cite the user-supplied primary source (book and page) in test comments or names.
