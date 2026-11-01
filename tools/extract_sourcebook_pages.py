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
    text = re.sub(r"(\w+)-\s*\n\s*(\w+)", r"\1\2", text)
    text = re.sub(r"(\w+)-\s+([a-z]+)", r"\1-\2", text)
    return text


def to_entity_title(name: str) -> str:
    """Convert an uppercase entity heading to title case, preserving minor words."""
    name = re.sub(r"\s+", " ", name.strip())
    minor = {"of", "and", "in", "the", "for", "to", "a", "an", "on", "at", "by", "from", "with"}
    words = name.split()
    titled = []
    for i, w in enumerate(words):
        if "-" in w:
            parts = [p.capitalize() if p.lower() not in minor or i == 0 else p.lower() for p in w.split("-")]
            titled.append("-".join(parts))
        else:
            wl = w.lower()
            if i > 0 and wl in minor:
                titled.append(wl)
            else:
                titled.append(wl.capitalize())
    return " ".join(titled)


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


def get_block_effective_y0(b: dict) -> float:
    """Calculate vertical position from the first non-empty text line."""
    for line in b.get("lines", []):
        line_text = "".join(s["text"] for s in line.get("spans", [])).strip()
        if line_text:
            return line["bbox"][1]
    return b.get("bbox", (0, 0, 0, 0))[1]


def sort_blocks_by_column(blocks: list[dict], page_width: float, page_height: float) -> list[dict]:
    """
    Sort text blocks for multi-column pages.
    Full-width blocks stay in vertical order.
    Column blocks are partitioned using horizontal midpoint (x0 + x1) / 2.0 < mid_x.
    Vertical order is calculated from the first non-empty text line.
    """
    mid_x = page_width / 2.0
    full_width_threshold = page_width * 0.65

    spanning_top = []
    col_left = []
    col_right = []
    spanning_bottom = []

    for b in blocks:
        x0, y0, x1, y1 = b["bbox"]
        width = x1 - x0
        is_spanning = width >= full_width_threshold or (x0 < mid_x * 0.7 and x1 > mid_x * 1.3)
        eff_y = get_block_effective_y0(b)

        # Skip headers/footers in outer margins
        all_txt = "".join(s["text"] for l in b.get("lines", []) for s in l.get("spans", [])).strip()
        if eff_y < 42 or y1 > page_height - 42:
            if all_txt.isdigit() or all_txt.startswith("CHAPTER") or all_txt == "SKILLS":
                continue
        if (x1 < 40 or x0 > page_width - 40) and width < 30:
            if all_txt.isdigit() or "chapter" in all_txt.lower() or "skills" in all_txt.lower():
                continue

        if is_spanning:
            if eff_y < page_height * 0.25:
                spanning_top.append(b)
            else:
                spanning_bottom.append(b)
        else:
            center_x = (x0 + x1) / 2.0
            if center_x < mid_x:
                col_left.append(b)
            else:
                col_right.append(b)

    spanning_top.sort(key=get_block_effective_y0)
    col_left.sort(key=get_block_effective_y0)
    col_right.sort(key=get_block_effective_y0)
    spanning_bottom.sort(key=get_block_effective_y0)

    return spanning_top + col_left + col_right + spanning_bottom


def process_page_to_markdown(page: pymupdf.Page, phys_num: int) -> str:
    """
    Process a single PDF page into pre-structured Markdown:
    - Sorts blocks using line-level geometry and horizontal midpoints.
    - Automatically detects entity headings (Pterra or size >= 11.5pt) and emits '## <Name> (p. <Folio>)'.
    - Automatically detects multi-column alignments and emits standard GFM tables (| ... |).
    """
    pw = page.rect.width
    ph = page.rect.height

    d = page.get_text("dict")
    raw_blocks = [b for b in d.get("blocks", []) if b.get("type") == 0]
    sorted_blocks = sort_blocks_by_column(raw_blocks, pw, ph)

    page_md_parts = []

    for b in sorted_blocks:
        lines = b.get("lines", [])
        if not lines:
            continue

        parsed_items = []
        for l in lines:
            ly0 = l["bbox"][1]
            spans = l.get("spans", [])
            if not spans:
                continue

            first_span = spans[0]
            font = first_span.get("font", "").lower()
            size = first_span.get("size", 0.0)
            full_line = "".join(s["text"] for s in spans).strip()
            if not full_line:
                continue

            # Major entity heading detection (Pterra font or size >= 11.5pt)
            if ("pterra" in font or size >= 11.5) and size >= 10.0 and ly0 > 45 and ly0 < ph - 45:
                if not full_line.startswith("CHAPTER") and not full_line.startswith("Table") and not full_line.isdigit():
                    raw_name = full_line.split("(")[0].split("[")[0].strip()
                    if len(raw_name) >= 2:
                        parsed_items.append(("heading", (raw_name, full_line)))
                        continue

            # Parse line cells with horizontal splitting (> 12pt gap indicates new column)
            cur_text = []
            cur_x0 = None
            cur_x1 = None
            cells = []
            for s in spans:
                stxt = s["text"]
                if not stxt.strip():
                    continue
                sx0, sy0, sx1, sy1 = s["bbox"]
                if cur_x0 is None:
                    cur_x0 = sx0
                    cur_x1 = sx1
                    cur_text = [stxt]
                else:
                    if sx0 - cur_x1 > 12.0:
                        cells.append((cur_x0, cur_x1, "".join(cur_text).strip()))
                        cur_x0 = sx0
                        cur_x1 = sx1
                        cur_text = [stxt]
                    else:
                        cur_x1 = max(cur_x1, sx1)
                        cur_text.append(stxt)
            if cur_x0 is not None:
                cells.append((cur_x0, cur_x1, "".join(cur_text).strip()))

            if cells:
                # Merge if y is within 3pt of previous row
                if parsed_items and parsed_items[-1][0] == "row" and abs(ly0 - parsed_items[-1][1][0]) <= 3.0:
                    parsed_items[-1][1][1].extend(cells)
                    parsed_items[-1][1][1].sort(key=lambda c: c[0])
                else:
                    parsed_items.append(("row", (ly0, cells)))

        idx = 0
        while idx < len(parsed_items):
            item_type, item_data = parsed_items[idx]
            if item_type == "heading":
                raw_name, full_line = item_data
                ent_title = to_entity_title(raw_name)
                page_md_parts.append(f"\n## {ent_title} (p. {phys_num})\n\n**{clean_ligatures(full_line)}**\n")
                idx += 1
                continue

            ly0, cells = item_data
            if len(cells) >= 2:
                tbl_start = idx
                tbl_end = idx
                while tbl_end < len(parsed_items):
                    if parsed_items[tbl_end][0] != "row":
                        break
                    r_cells = parsed_items[tbl_end][1][1]
                    if len(r_cells) >= 2:
                        tbl_end += 1
                    elif tbl_end > tbl_start and len(r_cells) == 1 and r_cells[0][0] > parsed_items[tbl_start][1][1][0][0] + 25:
                        tbl_end += 1
                    else:
                        break

                multi_count = sum(1 for j in range(tbl_start, tbl_end) if len(parsed_items[j][1][1]) >= 2)
                if multi_count >= 2:
                    tbl_rows = [parsed_items[j][1][1] for j in range(tbl_start, tbl_end)]
                    multi_rows = [r for r in tbl_rows if len(r) >= 2]
                    all_x0 = sorted(c[0] for r in multi_rows for c in r)
                    clusters = []
                    for x in all_x0:
                        if not clusters or x - clusters[-1][-1] > 12.0:
                            clusters.append([x])
                        else:
                            clusters[-1].append(x)
                    valid_col_starts = []
                    for cl in clusters:
                        min_c, max_c = min(cl), max(cl)
                        cnt = sum(1 for r in multi_rows if any(min_c - 2.0 <= c[0] <= max_c + 2.0 for c in r))
                        if cnt >= 2:
                            valid_col_starts.append(sum(cl) / len(cl))
                    valid_col_starts.sort()
                    cols = []
                    for s in valid_col_starts:
                        if not cols or s - cols[-1] >= 18.0:
                            cols.append(s)

                    if len(cols) >= 2:
                        num_cols = len(cols)
                        grid = []
                        for r in tbl_rows:
                            row_cells = ["" for _ in range(num_cols)]
                            for x0, x1, txt in r:
                                col_idx = 0
                                for ci, bound in enumerate(cols[1:], start=1):
                                    if x0 >= bound - 8.0:
                                        col_idx = ci
                                if row_cells[col_idx]:
                                    row_cells[col_idx] += " " + txt
                                else:
                                    row_cells[col_idx] = txt
                            grid.append(row_cells)

                        merged_grid = []
                        for r in grid:
                            non_empty = [ci for ci, c in enumerate(r) if c.strip()]
                            if merged_grid and len(non_empty) == 1 and non_empty[0] > 0 and not r[0].strip():
                                ci = non_empty[0]
                                merged_grid[-1][ci] = (merged_grid[-1][ci] + " " + r[ci]).strip()
                            else:
                                merged_grid.append(r)

                        # Prune empty columns
                        if merged_grid:
                            active_cols = [ci for ci in range(num_cols) if any(r[ci].strip() for r in merged_grid)]
                            if len(active_cols) >= 2 and len(active_cols) < num_cols:
                                merged_grid = [[r[ci] for ci in active_cols] for r in merged_grid]
                                num_cols = len(active_cols)

                        if len(merged_grid) >= 2:
                            header = [clean_ligatures(c.strip()) for c in merged_grid[0]]
                            aligns = [":---:" if any(header[ci].lower().startswith(k) for k in ["dc", "level", "time", "modifier"]) else ":---" for ci in range(num_cols)]
                            gfm = ["\n| " + " | ".join(header) + " |", "| " + " | ".join(aligns) + " |"]
                            for r in merged_grid[1:]:
                                cl_cells = [clean_soft_hyphens(clean_ligatures(c.strip())) for c in r]
                                gfm.append("| " + " | ".join(cl_cells) + " |")
                            page_md_parts.append("\n".join(gfm) + "\n")
                            idx = tbl_end
                            continue

            row_txt = " ".join(c[2] for c in cells)
            row_txt = clean_soft_hyphens(clean_ligatures(row_txt))
            page_md_parts.append(row_txt)
            idx += 1

    return "\n".join(page_md_parts)


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

            page_md = process_page_to_markdown(page, phys_num)
            extracted_text_lines.append(page_md + "\n")
        else:
            scanned_page_count += 1
            pix = page.get_pixmap(dpi=args.dpi)
            img_filename = f"page_p{phys_num}_pdf{pdf_page_1based}.png"
            img_path = output_dir / img_filename
            pix.save(str(img_path))
            rendered_images.append((phys_num, pdf_page_1based, img_path))
            scan_note = f"\n# Physical Page {phys_num} (PDF Page {pdf_page_1based}): Scanned image rendered to {img_filename}\n"
            extracted_text_lines.append(scan_note)

    # Write consolidated markdown and text files
    md_out_path = output_dir / f"extracted_{pdf_path.stem}_p{input_pages[0]}-{input_pages[-1]}.md"
    txt_out_path = output_dir / f"extracted_{pdf_path.stem}_p{input_pages[0]}-{input_pages[-1]}.txt"
    full_output = "\n".join(extracted_text_lines)
    with open(md_out_path, "w", encoding="utf-8") as f:
        f.write(full_output)
    with open(txt_out_path, "w", encoding="utf-8") as f:
        f.write(full_output)

    print("\nExtraction Summary:")
    print(f"  Sourcebook:      {pdf_path.name}")
    print(f"  Pages requested: {len(page_mappings)} pages")
    print(f"  Text-layer:       {text_page_count} pages")
    print(f"  Image scans:      {scanned_page_count} pages")
    print(f"  Output markdown:  {md_out_path}")
    print(f"  Output text:      {txt_out_path}")
    if rendered_images:
        print(f"  Rendered images:  {len(rendered_images)} PNGs saved to {output_dir}")

    doc.close()


if __name__ == "__main__":
    main()
