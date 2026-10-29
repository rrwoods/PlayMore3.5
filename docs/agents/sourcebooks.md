# Sourcebooks and PDF Extraction

Guidelines and tooling for agents consulting D&D 3.5e primary source PDFs.

## Primary Source Exclusivity

Primary source PDFs in the reference library are the sole authoritative source of truth for all game rules and mechanics. When a requested sourcebook PDF is inaccessible, uncalibrated, or missing, immediately stop and surface the access issue to the user. All rules documentation and engine mechanics must derive directly from the primary source text or human arbitration.

## 1. Primary Source Library Location

Reference sourcebook PDFs are located at:
`C:/Users/rickr/Documents/D&D notes/resources/books/`

### Sandbox Bypass Requirement (`BypassSandbox: true`)
Because the library path resides outside the workspace boundary, any terminal command or Python script accessing this directory will fail with an access violation under standard sandbox isolation. Always set `BypassSandbox: true` on commands that read or extract from primary source PDFs.

## 2. Dedicated Python Environment (`dnd_pdf`)

PDF processing is pre-configured in a dedicated virtual environment containing `pymupdf` (PyMuPDF) and image handling libraries:
- **Executable**: `C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe`
- **Installed Packages**: `pymupdf`, `pillow`, `reportlab`
- Do not attempt to `pip install` packages into the global Python environment. Always invoke Python via the `dnd_pdf` executable path.

## 3. Script Execution Protocol

Adhere strictly to repo and workspace script execution rules when inspecting PDFs:
- **Sandbox bypass**: Always invoke PDF extraction scripts or inspection commands with `BypassSandbox: true` so the process can read from the primary source library.
- **No inline script commands**: Never pass multi-line or chained Python code via `python -c "..."`.
- **Scratch script workflow**:
  1. Create an empty script file in the conversation scratch directory using PowerShell `New-Item`:
     `New-Item -ItemType File -Force -Path '<scratch_dir>\extract_<topic>.py'`
  2. Populate the script using the write or edit tool.
  3. Execute the script file directly with the `dnd_pdf` python executable (`BypassSandbox: true`).

## 4. Text-Based vs. Image-Based (Scanned) PDFs

D&D 3.5e PDFs fall into two categories: searchable text-layer PDFs and image-only scanned PDFs.

### 4.1 Quick Detection
Check if a page has extractable text:
```python
import pymupdf

doc = pymupdf.open(pdf_path)
page = doc[page_index]
text = page.get_text()
# If text.strip() is empty, the page is an image scan.
```

### 4.2 Text-Layer PDFs
- **Direct extraction**: Extract text via `page.get_text()`.
- **UTF-8 file output**: Always write extracted text directly to a file with `encoding="utf-8"` in `scratch/`. Never rely on console `print()` to stdout, as D&D sourcebooks use typographic ligatures (e.g. `\ufb01` for "fi", `\ufb02` for "fl") that throw `UnicodeEncodeError` under Windows console encodings (cp1252).
- **Inspect via `view_file`**: Read the generated UTF-8 text file using `view_file`.

### 4.3 Image-Only / Scanned PDFs
When the PDF consists of scanned raster images without a usable text layer:
- **Render page to PNG**:
  ```python
  import pymupdf

  doc = pymupdf.open(pdf_path)
  page = doc[page_index]
  pix = page.get_pixmap(dpi=150)
  pix.save(r"<scratch_dir>\page_<num>.png")
  ```
- **Inspect via `view_file`**: Use the `view_file` tool directly on the rendered PNG file to read the primary source visual content.

## 5. Page Offset Calibration

Printed folio page numbers in 3.5e books rarely align with 1-indexed PDF page numbers due to covers, title pages, and tables of contents:
- Calibrate the offset first by scanning headers/footers or searching for a known chapter or section heading in the first 10–20 pages.
- Verify the physical page header/footer before extracting extended passages to ensure the citations record accurate book page numbers.

## 6. Primary Source Maneuver Extraction Tooling

Martial maneuvers and stances from *Tome of Battle* are extracted directly from primary source text into discipline reference files (`docs/rules/content/maneuvers/<discipline>.md`) using `tools/extract_maneuvers.py`.

### 6.1 Execution
Run the extraction tool using the dedicated `dnd_pdf` Python environment:
```powershell
& "C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe" tools/extract_maneuvers.py
```

### 6.2 Arguments
- `--pdf <path>`: Path to the *Tome of Battle* PDF (defaults to `C:/Users/rickr/Documents/D&D notes/resources/books/Tome of Battle - Book of Nine Swords.pdf`).
- `--output <dir>`: Target output directory (defaults to `docs/rules/content/maneuvers`).

## 7. Persistent Sourcebook Markdown Archive (`rules-md/`)

To eliminate repeated manual inspection of raw PDFs and avoid multi-column/ligature parsing errors during agent sessions, extracted sourcebook sections are permanently archived as clean, verbatim markdown files.

### 7.1 Archive Location
The external markdown archive resides alongside the primary sourcebooks:
`C:/Users/rickr/Documents/D&D notes/resources/rules-md/`

### 7.2 Directory and File Hierarchy
Files are organized by sourcebook, then by chapter or major section:
`rules-md/<Book Name>/<Chapter or Section>.md`

Examples:
- `rules-md/Complete Warrior/Chapter 2 - Feats.md`
- `rules-md/Dungeonscape/Chapter 1 - Factotum.md`
- `rules-md/Player's Handbook/Chapter 8 - Combat.md`

### 7.3 Primary Source Verbatim Mirror Invariant
The `rules-md/` archive serves as an authoritative verbatim mirror of the primary source text. Agents consult it directly to write citations and synthesize canonical Reference Entries in `docs/rules/`. Human arbitration is reserved for ambiguities, contradictions (rules dysfunctions), and table fiat per ADR-0001.

## 8. Sourcebook Ingestion

When a task requires rules or content from a sourcebook that does not yet exist in `docs/rules/` or `rules-md/`:
1. Run the `/extract-sourcebook` skill to calibrate, extract, transcribe, and audit the section into `rules-md/`.
2. Run the `/distill-rules` skill to resolve or register schemas, strip narrative fluff, and synthesize canonical Reference Entries into `docs/rules/`.
3. Run the `/verify-rules` skill to crosscheck and sanity-check the distilled entries against `rules-md/`.

## 9. Markdown Document Formatting Standards

Every document in `rules-md/` must follow these layout and structural standards:

### 9.1 YAML Frontmatter
Every file begins with structured machine-readable frontmatter:
```yaml
---
sourcebook: Complete Warrior
source_file: Complete Warrior.pdf
chapter: Chapter 2 - Feats
physical_pages: 89-115
pdf_pages: 91-117
date_extracted: 2026-10-29
---
```

### 9.2 Heading-Level Citations
To allow exact page citations without cluttering running text with page break banners, attach the physical folio page number directly to major section and entity headers:
```markdown
### Shock Trooper (p. 112)
```
For classes, prestige classes, and subsystems spanning multiple pages:
```markdown
## Factotum (p. 14)
```

### 9.3 Tables
Render class progression tables, feat prerequisite matrices, and statistics as standard GitHub-Flavored Markdown (GFM) tables:
```markdown
| Level | Base Attack Bonus | Fort Save | Ref Save | Will Save | Special |
| :---: | :---: | :---: | :---: | :---: | :--- |
| 1st | +0 | +0 | +2 | +0 | Cunning insight, cunning knowledge, trapfinding |
| 2nd | +1 | +0 | +3 | +0 | Arcane dilettante (1 spell) |
```

### 9.4 Sidebars and Callout Boxes
Format sidebars, developer side-notes ("Behind the Curtain"), and variant rule callouts using markdown blockquotes to clearly isolate them from the main rules flow:
```markdown
> **Sidebar: Behind the Curtain - Martial Adept Progression**
>
> When designing the martial adept classes, we wanted to ensure that multiclass characters...
```

### 9.5 Verbatim Text Fidelity
Retain complete verbatim rules text, descriptions, and prerequisites. Do not truncate, summarize, or substitute informal abbreviations.


