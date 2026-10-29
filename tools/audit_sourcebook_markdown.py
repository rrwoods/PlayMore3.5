#!/usr/bin/env python3
"""
Sourcebook Markdown Archive Audit Tool.

Audits markdown files in 'rules-md/' or workspace against formatting standards
defined in docs/agents/sourcebooks.md (Section 9):
  - YAML frontmatter presence and required fields
  - Heading-level page citations (## <Name> (p. <Folio>))
  - GFM table structure and column alignment
  - Heading syntax, empty headers, and encoding artifacts
  - Entity count validation against sourcebook TOC

Usage:
  python tools/audit_sourcebook_markdown.py <path/to/markdown.md> [--expected-count N] [--heading-level L]
"""

import argparse
import re
import sys
from pathlib import Path


def parse_page_range(range_str: str) -> tuple[int, int]:
    """Parse range string like '19-49' or single page '19' into (start, end)."""
    clean = range_str.strip().replace(" ", "")
    if "-" in clean:
        parts = clean.split("-", 1)
        return int(parts[0]), int(parts[1])
    num = int(clean)
    return num, num


def audit_frontmatter(content: str) -> tuple[bool, dict, list[str]]:
    """Verify YAML frontmatter presence and required keys."""
    errors = []
    data = {}
    if not content.startswith("---"):
        errors.append("File does not start with YAML frontmatter delimiter ('---').")
        return False, data, errors

    end_idx = content.find("\n---", 3)
    if end_idx == -1:
        errors.append("Unclosed YAML frontmatter delimiter ('---').")
        return False, data, errors

    fm_text = content[3:end_idx].strip()
    for line in fm_text.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if ":" in line:
            key, val = line.split(":", 1)
            data[key.strip()] = val.strip().strip('"').strip("'")

    required_keys = ["sourcebook", "source_file", "physical_pages", "pdf_pages", "date_extracted"]
    for req in required_keys:
        if req not in data or not data[req]:
            errors.append(f"Missing required frontmatter key: '{req}'")

    if "chapter" not in data and "section" not in data:
        errors.append("Missing required frontmatter key: 'chapter' or 'section'")

    if "date_extracted" in data:
        if not re.match(r"^\d{4}-\d{2}-\d{2}$", data["date_extracted"]):
            errors.append(f"date_extracted '{data['date_extracted']}' is not YYYY-MM-DD format.")

    return len(errors) == 0, data, errors


def audit_headings(
    content: str,
    heading_level: str,
    page_range: tuple[int, int] | None,
    exclude_pattern: str | None = None
) -> tuple[bool, list[tuple[str, int]], list[tuple[str, int]], list[str]]:
    """Verify heading-level citations, separate primary entities from intro sections, and check page ranges."""
    errors = []
    entities = []
    intro_sections = []

    pattern = rf"^{re.escape(heading_level)}\s+(.+)$"
    matches = list(re.finditer(pattern, content, re.MULTILINE))

    if not matches:
        errors.append(f"No headings found at level '{heading_level}'.")
        return False, entities, intro_sections, errors

    cite_pattern = re.compile(r"^(.*?)\s*\(p\.\s*(\d+)(?:-(\d+))?\)\s*$")
    exclude_re = re.compile(exclude_pattern) if exclude_pattern else None

    for m in matches:
        raw_title = m.group(1).strip()
        if not raw_title:
            errors.append(f"Empty heading found at level '{heading_level}'.")
            continue

        cite_match = cite_pattern.match(raw_title)
        if not cite_match:
            # Check if it's an introductory or general rules header
            if "general" in raw_title.lower() or "introduction" in raw_title.lower() or "rules" in raw_title.lower():
                if not re.search(r"\(p\.\s*\d+\)", raw_title):
                    errors.append(f"Heading missing '(p. <Folio>)' citation: '{heading_level} {raw_title}'")
                continue
            errors.append(f"Heading missing '(p. <Folio>)' citation: '{heading_level} {raw_title}'")
            continue

        name = cite_match.group(1).strip()
        start_p = int(cite_match.group(2))

        if exclude_re and exclude_re.search(name):
            intro_sections.append((name, start_p))
        else:
            entities.append((name, start_p))

        if page_range:
            min_p, max_p = page_range
            if start_p < min_p or start_p > max_p:
                errors.append(f"Citation p. {start_p} in '{name}' outside frontmatter range {min_p}-{max_p}.")

    return len(errors) == 0, entities, intro_sections, errors


def audit_tables(content: str) -> tuple[bool, int, list[str]]:
    """Verify GitHub-Flavored Markdown table syntax and column counts."""
    errors = []
    lines = content.splitlines()
    in_table = False
    table_lines = []
    table_count = 0

    def check_table(t_lines: list[tuple[int, str]]):
        nonlocal table_count
        table_count += 1
        if len(t_lines) < 2:
            start_num = t_lines[0][0]
            errors.append(f"Table at line {start_num} has only 1 row (missing delimiter/content).")
            return

        # Delimiter row should be second row (or third if title)
        delim_idx = -1
        for idx in range(min(3, len(t_lines))):
            row_text = t_lines[idx][1]
            if re.match(r"^\s*\|?\s*:?-+:?\s*(\|?\s*:?-+:?\s*)+\|?\s*$", row_text):
                delim_idx = idx
                break

        if delim_idx == -1:
            errors.append(f"Table starting at line {t_lines[0][0]} lacks a valid delimiter row (e.g. '| :---: | --- |').")
            return

        # Check column counts across all rows
        col_counts = []
        for line_no, row_str in t_lines:
            # Strip outer pipes and split
            stripped = row_str.strip()
            if stripped.startswith("|"):
                stripped = stripped[1:]
            if stripped.endswith("|"):
                stripped = stripped[:-1]
            cols = [c.strip() for c in stripped.split("|")]
            col_counts.append((line_no, len(cols)))

        expected_cols = col_counts[delim_idx][1]
        for line_no, c_len in col_counts:
            if c_len != expected_cols:
                errors.append(f"Table line {line_no} has {c_len} columns, expected {expected_cols} (defined by delimiter).")

    for i, line in enumerate(lines, start=1):
        is_pipe_line = "|" in line and not line.strip().startswith("```")
        if is_pipe_line:
            in_table = True
            table_lines.append((i, line))
        else:
            if in_table:
                check_table(table_lines)
                table_lines = []
                in_table = False

    if in_table:
        check_table(table_lines)

    return len(errors) == 0, table_count, errors


def audit_artifacts(content: str) -> tuple[bool, list[str]]:
    """Check for encoding artifacts and corrupted headers."""
    errors = []
    # Check replacement character
    if "\ufffd" in content:
        count = content.count("\ufffd")
        errors.append(f"Found {count} Unicode replacement character(s) (\\ufffd). Clean raw ligatures or encodings.")

    # Check for empty subheadings (#### or ### with nothing)
    for m in re.finditer(r"^(#{1,6})\s*$", content, re.MULTILINE):
        errors.append("Found empty markdown header with no title text.")

    # Check for unformatted raw tables that look like text dumps
    raw_tbl_match = re.search(r"\b1st[–-]10th\b.*\b11th[–-]14th\b", content)
    if raw_tbl_match and "|" not in raw_tbl_match.group(0):
        errors.append("Detected raw unformatted table data in text (e.g. level progression missing GFM table pipes).")

    return len(errors) == 0, errors


def main():
    parser = argparse.ArgumentParser(
        description="Audit primary source markdown documents against formatting and completeness standards."
    )
    parser.add_argument(
        "file",
        help="Path to markdown document to audit."
    )
    parser.add_argument(
        "--expected-count",
        type=int,
        default=None,
        help="Expected count of primary entities (e.g. 32 vestiges, 45 feats)."
    )
    parser.add_argument(
        "--heading-level",
        default="##",
        help="Heading level for major entities (default: '##')."
    )
    parser.add_argument(
        "--exclude-pattern",
        default=r"(?i)^(General|Introduction|Overview)",
        help="Regex pattern for non-entity headings to exclude from entity count (default: '(?i)^(General|Introduction|Overview)')."
    )

    args = parser.parse_args()
    target_path = Path(args.file)
    if not target_path.is_file():
        print(f"Error: Target file not found: {target_path}", file=sys.stderr)
        sys.exit(1)

    content = target_path.read_text(encoding="utf-8")
    line_count = len(content.splitlines())
    char_count = len(content)

    print(f"\nAuditing Markdown Document: {target_path.name}")
    print(f"File Path: {target_path.resolve()}")
    print(f"Metrics:   {line_count} lines, {char_count} characters\n" + "-" * 70)

    # 1. Frontmatter
    fm_pass, fm_data, fm_errors = audit_frontmatter(content)
    page_range = None
    if "physical_pages" in fm_data:
        try:
            page_range = parse_page_range(fm_data["physical_pages"])
        except ValueError:
            fm_errors.append(f"Invalid physical_pages format: '{fm_data['physical_pages']}'")

    print(f"1. YAML Frontmatter: {'PASS' if fm_pass else 'FAIL'}")
    if fm_data:
        for k, v in fm_data.items():
            print(f"   - {k}: {v}")
    for err in fm_errors:
        print(f"   [ERROR] {err}")

    # 2. Heading-level citations
    head_pass, entities, intro_sections, head_errors = audit_headings(
        content, args.heading_level, page_range, args.exclude_pattern
    )
    print(f"\n2. Heading Citations ({args.heading_level}): {'PASS' if head_pass else 'FAIL'}")
    print(f"   Primary entities verified: {len(entities)}")
    if intro_sections:
        print(f"   Introductory sections excluded: {len(intro_sections)} ({', '.join(s[0] for s in intro_sections)})")
    if entities:
        sample_count = min(6, len(entities))
        for name, p in entities[:sample_count]:
            print(f"   - {name} (p. {p})")
        if len(entities) > sample_count:
            print(f"   ... and {len(entities) - sample_count} more")
    for err in head_errors:
        print(f"   [ERROR] {err}")

    # 3. GFM Tables
    tbl_pass, tbl_count, tbl_errors = audit_tables(content)
    print(f"\n3. GFM Tables: {'PASS' if tbl_pass else 'FAIL'}")
    print(f"   Total valid tables verified: {tbl_count}")
    for err in tbl_errors:
        print(f"   [ERROR] {err}")

    # 4. Artifacts & Corrupted text
    art_pass, art_errors = audit_artifacts(content)
    print(f"\n4. Integrity & Encoding Artifacts: {'PASS' if art_pass else 'FAIL'}")
    for err in art_errors:
        print(f"   [ERROR] {err}")

    # 5. Entity count verification
    count_pass = True
    print("\n5. Entity Count Verification: ", end="")
    if args.expected_count is not None:
        if len(entities) == args.expected_count:
            print(f"PASS (Count matches expected: {args.expected_count})")
        else:
            count_pass = False
            print(f"FAIL (Found {len(entities)}, expected {args.expected_count})")
    else:
        print(f"SKIPPED (No --expected-count specified, found {len(entities)} entities)")

    overall_pass = fm_pass and head_pass and tbl_pass and art_pass and count_pass
    print("-" * 70)
    if overall_pass:
        print("OVERALL RESULT: ALL AUDIT CHECKS PASSED (Quality Gate Satisfied)")
        sys.exit(0)
    else:
        print("OVERALL RESULT: AUDIT FAILED (Review flagged defects above)")
        sys.exit(1)


if __name__ == "__main__":
    main()
