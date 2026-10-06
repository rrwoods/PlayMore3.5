## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles mapped to matching labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` and `docs/adr/` at the repo root). See `docs/agents/domain.md`.

## Formatting and conventions

### Line endings

All text files in this repository (including `*.md`, `*.cs`, `*.csv`, and `*.ps1`) must use Windows CRLF (`\r\n`) line endings as configured in `.gitattributes`. When creating or editing files, always ensure CRLF line endings are used.

To verify line endings across the repository:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-crlf.ps1
```

To automatically fix any non-CRLF line endings:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-crlf.ps1 -Fix
```
