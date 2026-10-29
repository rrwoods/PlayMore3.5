---
name: extract-sourcebook
description: Extract rules content from a D&D 3.5e sourcebook PDF into a clean, audited verbatim markdown file in the persistent archive (rules-md/).
disable-model-invocation: true
---

# Extract Sourcebook

Extract primary source rules from reference PDFs into the persistent external markdown archive (`rules-md/`), adhering to the Primary Source Verbatim Mirror Invariant.

## Preconditions & Environment

1. **Direct Archive Access & Sandbox Scope**: Antigravity has read access to `C:\Users\rickr\Documents\D&D notes\resources` and write access to `C:\Users\rickr\Documents\D&D notes\resources\rules-md`. Native tools (`read_file`, `write_file`, `edit_file`) operate directly on `rules-md/`. However, executing extraction scripts via the dedicated `dnd_pdf` virtual environment requires `BypassSandbox: true` because `C:\Users\rickr\Envs` resides outside the sandbox boundary.
2. **Dedicated Python Environment**: Always invoke Python via `C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe` (`dnd_pdf` venv with `pymupdf`).
3. **Encoding Discipline**: Route all text extraction and inspection through UTF-8 scratch files. Writing to UTF-8 files safely preserves typographic ligatures (`fi`, `fl`) against Windows console (`cp1252`) encoding constraints.

---

## Phase 1: Intake & Calibration

### 1. Intake Parameters
Identify or prompt for the three target parameters:
- **Book**: Title or filename (e.g. `Tome of Magic`, `Complete Warrior.pdf`).
- **Pages**: Target physical folio page range (e.g. `19-49`, `89-115`).
- **Chapter / Section**: Output section title (e.g. `Chapter 1 - Vestiges`, `Chapter 2 - Feats`).

### 2. Page Offset Calibration
Run the calibration mode in `tools/extract_sourcebook_pages.py`:
```powershell
& "C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe" tools/extract_sourcebook_pages.py --book "<Book>" --calibrate
```
- Note the detected `--offset` integer (`pdf_page - physical_page`).
- Note whether the document contains a digital text layer or consists of image scans.

---

## Phase 2: Extraction

Execute the extraction helper tool:
```powershell
& "C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe" tools/extract_sourcebook_pages.py --book "<Book>" --pages "<Pages>" --offset <Offset> -o scratch
```

### Execution Branching:
- **Text-layer pages**: Produces `scratch/extracted_<Book>_p<Start>-<End>.txt` with multi-column sorting, cleaned ligatures, and hyphen normalization.
- **Scanned image pages**: Produces high-resolution PNGs (`scratch/page_p<Phys>_pdf<PDF>.png`). Inspect rendered PNG images directly using `read_file`.

---

## Phase 3: Adaptive Transcription & Formatting

Author the clean markdown file at:
`C:/Users/rickr/Documents/D&D notes/resources/rules-md/<Book Name>/<Chapter or Section>.md`

### 1. Adaptive Sizing Strategy
- **Short Sections (1–5 pages)**: Transcribe directly to the destination file using `write_file` / `edit_file`.
- **Long / Repetitive Sections (6–30+ pages)**: Process high-volume text via a tailored parsing script in `scratch/` (e.g. `scratch/build_markdown.py`). Have the script read `scratch/extracted_*.txt`, apply regex structuring and paragraph smoothing, and output the destination markdown file directly.

### 2. Structural Standards (docs/agents/sourcebooks.md Section 9)
Every archive document must follow these invariants:

- **YAML Frontmatter**:
  ```yaml
  ---
  sourcebook: <Book Name>
  source_file: <Book.pdf>
  chapter: <Chapter Name>
  section: <Section Name>
  physical_pages: <Start>-<End>
  pdf_pages: <Start+Offset>-<End+Offset>
  date_extracted: YYYY-MM-DD
  ---
  ```
- **Heading-Level Folio Citations**: Attach the physical folio page number to major entity headings:
  ```markdown
  ## Shock Trooper (p. 112)
  ```
- **GitHub-Flavored Markdown Tables**: Render all stat blocks, class progressions, prerequisite matrices, and embedded tables as GFM tables with explicit column alignment (e.g. `| :---: | :--- |`).
- **Verbatim Text Fidelity**: Retain the complete, unabridged source rules text, descriptions, and prerequisites in full, preserving exact primary source wording and mechanics.

### 3. Paragraph Smoothing Rules
D&D sourcebooks frequently wrap columns around irregular illustrations, causing orphan line breaks or artificial double newlines (`\n\n`):
- Merge mid-sentence line breaks into continuous paragraphs, recognizing complete sentences by terminal punctuation (`.`, `!`, `?`, `:`, `—`).
- Resolve split hyphens (`word-\nnext` $\rightarrow$ `wordnext`).
- Strip orphan column header/footer remnants and drop-shadow title duplicates.

---

## Phase 4: Quality Gate & Integrity Audit

Run the reusable audit tool to verify formatting and completeness before concluding:
```powershell
& "C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe" tools/audit_sourcebook_markdown.py "C:\Users\rickr\Documents\D&D notes\resources\rules-md\<Book>\<File>.md" --expected-count <N>
```

### Audit Assertions:
1. **YAML Frontmatter**: All required fields present and valid.
2. **Heading Citations**: Every entity heading has `(p. <Folio>)` matching the declared page range.
3. **Table Syntax**: All delimiter rows and column counts are valid GFM.
4. **Artifact Check**: Clean Unicode encoding throughout, fully populated heading titles, and complete GFM table formatting across all tables.
5. **Entity Count**: Number of extracted entities matches TOC / expected count.

If any check fails, address the defect and re-audit until `OVERALL RESULT: ALL AUDIT CHECKS PASSED`.

---

## Phase 5: Cleanup & Completion Criteria

1. **Clean Workspace Scratch**: Delete temporary files in `scratch/` (`extracted_*.txt`, `page_*.png`, and scratch parsing scripts).
2. **Verify Repository Status**: Run `git status` to confirm a clean working tree.
3. **Report Summary**: Output the archived file path, total lines, character count, and verified entity tally.
