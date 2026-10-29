#!/usr/bin/env python3
"""
Primary Source Sourcebook Extraction Helper Tool.

Extracts text blocks or renders high-resolution PNG page images from D&D 3.5e
primary source PDFs to support agent transcription into the persistent
markdown archive ('rules-md/').

Runs in the dedicated 'dnd_pdf' Python environment:
    & "C:\\Users\\rickr\\Envs\\dnd_pdf\\Scripts\\python.exe" tools/extract_sourcebook_pages.py [options]
"""

import argparse
import os
import re
import sys
from collections import Counter
from pathlib import Path
import pymupdf

DEFAULT_BOOKS_DIR = Path(r"C:/Users/rickr/Documents/D&D notes/resources/books")


def clean_ligatures(text: str) -> str:
    """Resolve typographic ligatures and normalize quotation marks and dashes."""
    text = text.replace("ﬁ ", "fi").replace("ﬂ ", "fl")
    text = text.replace("ﬁ", "fi").replace("ﬂ", "fl")
    text = text.replace("’", "'").replace("‘", "'")
    text = text.replace("“", '"').replace("”", '"')
    text = text.replace("—", "—").replace("–", "–")
    return text


def clean_soft_hyphens(text: str) -> str:
    """Resolve line-break hyphenations while preserving intended hyphens."""
    # Remove soft hyphens followed by whitespace or line break
    text = re.sub(r"(\w+)-\s*\n\s*(\w+)", r"\1\2", text)
    return text


def parse_page_ranges(range_str: str) -> list[int]:
    """Parse comma-separated page numbers and ranges (e.g., '1-5,8,11-13')."""
    pages = []
    for part in range_str.split(","):
        part = part.strip()
        if not part:
            continue
        if "-" in part:
            start_s, end_s = part.split("-", 1)
            start = int(start_s.strip())
            end = int(end_s.strip())
            if start <= end:
                pages.extend(range(start, end + 1))
            else:
                pages.extend(range(start, end - 1, -1))
        else:
            pages.append(int(part))
    return sorted(list(dict.fromkeys(pages)))


def sort_blocks_by_column(blocks: list[tuple], page_width: float) -> list[tuple]:
    """
    Sort text blocks for multi-column pages.
    Full-width blocks (spanning past middle) stay in vertical y0 order.
    Column blocks are partitioned into left (x0 < mid) and right (x0 >= mid).
    """
    mid_x = page_width / 2.0
    # Determine full-width vs column blocks
    full_width_threshold = page_width * 0.65

    # Group blocks into vertical bands or order:
    # A standard 2-column layout:
    # Sort blocks by: is_left_column (0 for span/left, 1 for right), then y0
    def block_key(b):
        x0, y0, x1, y1 = b[0], b[1], b[2], b[3]
        width = x1 - x0
        is_spanning = width >= full_width_threshold or (x0 < mid_x * 0.7 and x1 > mid_x * 1.3)
        if is_spanning:
            col = 0
        elif x0 < mid_x:
            col = 1
        else:
            col = 2
        return (col, y0)

    # Sort primarily by column, then top-to-bottom
    # But wait: if there's a spanning header at top, we want it before columns
    spanning_top = []
    col_left = []
    col_right = []
    spanning_bottom = []

    for b in blocks:
        x0, y0, x1, y1 = b[0], b[1], b[2], b[3]
        width = x1 - x0
        is_spanning = width >= full_width_threshold or (x0 < mid_x * 0.7 and x1 > mid_x * 1.3)
        if is_spanning:
            if y0 < 150:
                spanning_top.append(b)
            else:
                spanning_bottom.append(b)
        elif x0 < mid_x:
            col_left.append(b)
        else:
            col_right.append(b)

    spanning_top.sort(key=lambda b: b[1])
    col_left.sort(key=lambda b: b[1])
    col_right.sort(key=lambda b: b[1])
    spanning_bottom.sort(key=lambda b: b[1])

    return spanning_top + col_left + col_right + spanning_bottom


def resolve_pdf_path(book_arg: str, books_dir: Path) -> Path:
    """Resolve the book argument to a full PDF file path."""
    candidate = Path(book_arg)
    if candidate.is_file():
        return candidate.resolve()

    # Search in default books dir
    direct_match = books_dir / book_arg
    if direct_match.is_file():
        return direct_match.resolve()

    if not book_arg.lower().endswith(".pdf"):
        direct_pdf = books_dir / f"{book_arg}.pdf"
        if direct_pdf.is_file():
            return direct_pdf.resolve()

    # Case-insensitive / partial search
    book_lower = book_arg.lower()
    for f in books_dir.glob("*.pdf"):
        if book_lower in f.name.lower():
            return f.resolve()

    raise FileNotFoundError(f"Sourcebook PDF not found for '{book_arg}' in {books_dir}")


def run_calibration(doc: pymupdf.Document, pdf_path: Path, sample_pages_str: str | None = None) -> int:
    """
    Probe candidate PDF pages to detect digital text vs image scan,
    extract printed folio page numbers from headers/footers,
    and compute the recommended page offset.
    """
    total_pages = len(doc)
    print(f"\nCalibrating Page Offset: {pdf_path.name}")
    print(f"Total PDF Pages: {total_pages}")

    if sample_pages_str:
        candidate_pdf_pages = parse_page_ranges(sample_pages_str)
    else:
        # Default probe window: pages 5 through min(40, total_pages)
        candidate_pdf_pages = list(range(5, min(40, total_pages + 1)))

    scanned_count = 0
    text_count = 0
    matches = []
    samples = []

    for pdf_page in candidate_pdf_pages:
        if pdf_page < 1 or pdf_page > total_pages:
            continue
        page = doc[pdf_page - 1]
        text = page.get_text()
        if not text.strip():
            scanned_count += 1
            continue

        text_count += 1
        lines = [l.strip() for l in text.split("\n") if l.strip()]
        folio = None
        if lines and lines[0].isdigit():
            folio = int(lines[0])
        elif lines and lines[-1].isdigit():
            folio = int(lines[-1])
        else:
            blocks = page.get_text("blocks")
            for b in blocks:
                b_txt = b[4].strip()
                if b_txt.isdigit() and (b[1] < 80 or b[3] > page.rect.height - 80):
                    folio = int(b_txt)
                    break

        if folio is not None and 1 <= folio <= total_pages:
            offset = pdf_page - folio
            matches.append((pdf_page, folio, offset))
        else:
            first_preview = lines[:2] if len(lines) >= 2 else lines
            last_preview = lines[-2:] if len(lines) >= 2 else lines
            samples.append((pdf_page, first_preview, last_preview))

    print(f"\nProbe Results ({len(candidate_pdf_pages)} pages sampled):")
    print(f"  Pages with digital text: {text_count}")
    print(f"  Pages image-scanned:     {scanned_count}")

    if not matches:
        if scanned_count > text_count:
            print("\nWarning: Most sampled pages appear to be scanned images without a text layer.")
            print("Recommendation: Inspect rendered PNGs or verify if digital edition exists.")
        else:
            print("\nWarning: Could not automatically isolate numeric folio numbers from header/footer lines.")
            print("Sample page headers/footers:")
            for p, top, bot in samples[:5]:
                print(f"  PDF Page {p}: Top: {top} | Bottom: {bot}")
        return 0

    offset_counts = Counter(m[2] for m in matches)
    best_offset, count = offset_counts.most_common(1)[0]

    print(f"\nDetected Offset: {best_offset} (confidence: {count}/{len(matches)} folio matches)")
    print("Folio matches:")
    for p, f, off in matches[:8]:
        match_str = f"PDF Page {p} -> Printed Folio {f} (offset = {off})"
        if off == best_offset:
            print(f"  {match_str}")
        else:
            print(f"  {match_str} [OUTLIER]")
    if len(matches) > 8:
        print(f"  ... and {len(matches) - 8} more matches agreeing with offset {best_offset}")

    print(f"\nRecommended Extraction Option:")
    print(f"  --offset {best_offset}")
    return best_offset


def main():
    parser = argparse.ArgumentParser(
        description="Extract text blocks or render page PNGs from D&D 3.5e primary source PDFs."
    )
    parser.add_argument(
        "--book", "--pdf",
        dest="book",
        required=True,
        help="Path to PDF or sourcebook title/filename in primary source books directory."
    )
    parser.add_argument(
        "--calibrate",
        action="store_true",
        help="Run calibration mode to detect printed folio page numbers and compute PDF offset."
    )
    parser.add_argument(
        "--pages",
        default=None,
        help="Page numbers or ranges to extract (e.g. '24-28' or '15,18,22-25'). Default for --calibrate is '5-35'."
    )
    parser.add_argument(
        "--page-type",
        choices=["physical", "pdf"],
        default="physical",
        help="Whether --pages refers to physical book page numbers (default) or 1-indexed PDF page numbers."
    )
    parser.add_argument(
        "--offset",
        type=int,
        default=0,
        help="Page offset (PDF page minus physical page number). Default is 0. Used when --page-type is physical."
    )
    parser.add_argument(
        "-o", "--output-dir",
        default="scratch",
        help="Output directory for extracted text or images (default: 'scratch')."
    )
    parser.add_argument(
        "--dpi",
        type=int,
        default=150,
        help="DPI for rendering scanned page images (default: 150)."
    )
    parser.add_argument(
        "--force-render",
        action="store_true",
        help="Force rendering pages to PNG even if extractable text is detected."
    )
    parser.add_argument(
        "--books-dir",
        default=str(DEFAULT_BOOKS_DIR),
        help=f"Base directory containing primary source PDFs (default: {DEFAULT_BOOKS_DIR})."
    )

    args = parser.parse_args()

    books_dir = Path(args.books_dir)
    pdf_path = resolve_pdf_path(args.book, books_dir)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    doc = pymupdf.open(str(pdf_path))

    if args.calibrate:
        run_calibration(doc, pdf_path, args.pages)
        doc.close()
        return

    if not args.pages:
        print("Error: --pages is required for extraction. Use --calibrate to inspect folio numbers.", file=sys.stderr)
        doc.close()
        sys.exit(1)

    input_pages = parse_page_ranges(args.pages)
    if not input_pages:
        print("Error: No valid pages specified.", file=sys.stderr)
        doc.close()
        sys.exit(1)

    print(f"Opening: {pdf_path.name}")
    total_pdf_pages = len(doc)
    print(f"Total PDF pages: {total_pdf_pages}")

    # Map input pages to (physical_page, pdf_index_0based, pdf_page_1based)
    page_mappings = []
    for p in input_pages:
        if args.page_type == "physical":
            phys_num = p
            pdf_page_1based = phys_num + args.offset
            pdf_idx = pdf_page_1based - 1
        else:
            pdf_page_1based = p
            pdf_idx = pdf_page_1based - 1
            phys_num = pdf_page_1based - args.offset

        if pdf_idx < 0 or pdf_idx >= total_pdf_pages:
            print(f"Warning: Page {p} ({args.page_type}) maps to out-of-bounds PDF page {pdf_page_1based}. Skipping.", file=sys.stderr)
            continue
        page_mappings.append((phys_num, pdf_idx, pdf_page_1based))

    if not page_mappings:
        print("Error: None of the requested pages fall within the PDF bounds.", file=sys.stderr)
        sys.exit(1)

    extracted_text_lines = []
    rendered_images = []
    text_page_count = 0
    scanned_page_count = 0

    header_bar = f"# Extraction: {pdf_path.name}\n# Pages: {args.pages} ({args.page_type})\n# Offset: {args.offset}\n"
    extracted_text_lines.append(header_bar)

    for phys_num, pdf_idx, pdf_page_1based in page_mappings:
        page = doc[pdf_idx]
        raw_text = page.get_text()
        has_text = len(raw_text.strip()) >= 50 and not args.force_render

        if has_text:
            text_page_count += 1
            page_banner = f"\n{'='*70}\n# Physical Page {phys_num} (PDF Page {pdf_page_1based})\n{'='*70}\n"
            extracted_text_lines.append(page_banner)

            # Extract blocks
            blocks = page.get_text("blocks")
            # Filter text blocks (type 0)
            text_blocks = [b for b in blocks if b[6] == 0 and b[4].strip()]
            sorted_blocks = sort_blocks_by_column(text_blocks, page.rect.width)

            for b in sorted_blocks:
                b_text = clean_ligatures(b[4])
                b_text = clean_soft_hyphens(b_text)
                extracted_text_lines.append(b_text.strip() + "\n")
        else:
            scanned_page_count += 1
            pix = page.get_pixmap(dpi=args.dpi)
            img_filename = f"page_p{phys_num}_pdf{pdf_page_1based}.png"
            img_path = output_dir / img_filename
            pix.save(str(img_path))
            rendered_images.append((phys_num, pdf_page_1based, img_path))
            scan_note = f"\n# Physical Page {phys_num} (PDF Page {pdf_page_1based}): Scanned image rendered to {img_filename}\n"
            extracted_text_lines.append(scan_note)

    # Write consolidated text file
    txt_out_path = output_dir / f"extracted_{pdf_path.stem}_p{input_pages[0]}-{input_pages[-1]}.txt"
    with open(txt_out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(extracted_text_lines))

    print("\nExtraction Summary:")
    print(f"  Sourcebook:     {pdf_path.name}")
    print(f"  Pages requested: {len(page_mappings)} pages")
    print(f"  Text-layer:      {text_page_count} pages")
    print(f"  Image scans:     {scanned_page_count} pages")
    print(f"  Output text:     {txt_out_path}")
    if rendered_images:
        print(f"  Rendered images: {len(rendered_images)} PNGs saved to {output_dir}")

    doc.close()


if __name__ == "__main__":
    main()
