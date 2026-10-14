# Sourcebooks and PDF Extraction

Guidelines and tooling for agents consulting D&D 3.5e primary source PDFs.

## 1. Primary Source Library Location

Reference sourcebook PDFs are located at:
`C:/Users/rickr/Documents/D&D notes/resources/books/`

## 2. Dedicated Python Environment (`dnd_pdf`)

PDF processing is pre-configured in a dedicated virtual environment containing `pymupdf` (PyMuPDF) and image handling libraries:
- **Executable**: `C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe`
- **Installed Packages**: `pymupdf`, `pillow`, `reportlab`
- Do not attempt to `pip install` packages into the global Python environment. Always invoke Python via the `dnd_pdf` executable path.

## 3. Script Execution Protocol

Adhere strictly to repo and workspace script execution rules when inspecting PDFs:
- **No inline script commands**: Never pass multi-line or chained Python code via `python -c "..."`.
- **Scratch script workflow**:
  1. Create an empty script file in the conversation scratch directory using PowerShell `New-Item`:
     `New-Item -ItemType File -Force -Path '<scratch_dir>\extract_<topic>.py'`
  2. Populate the script using the write or edit tool.
  3. Execute the script file directly with the `dnd_pdf` python executable.

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
