#!/usr/bin/env python3
"""
Primary Source Martial Maneuvers & Stances Extraction Tool.

Extracts all martial maneuvers and stances directly from Chapter 4 of
'Tome of Battle: The Book of Nine Swords' (pp. 47-94) using PyMuPDF.
Normalizes typographic ligatures, folds range, target, and duration into
'Mechanics', omits narrative flavor text, and formats entries into the
6-field Maneuver Reference Schema partitioned across the 9 martial disciplines.
"""

import argparse
import os
import re
import sys
from pathlib import Path
import pymupdf

DEFAULT_PDF_PATH = r"C:/Users/rickr/Documents/D&D notes/resources/books/Tome of Battle - Book of Nine Swords.pdf"

DISCIPLINES = [
    "Desert Wind", "Devoted Spirit", "Diamond Mind", "Iron Heart",
    "Setting Sun", "Shadow Hand", "Stone Dragon", "Tiger Claw", "White Raven"
]

DISC_MAP = {
    "desert wind": "desert-wind.md",
    "devoted spirit": "devoted-spirit.md",
    "diamond mind": "diamond-mind.md",
    "iron heart": "iron-heart.md",
    "setting sun": "setting-sun.md",
    "shadow hand": "shadow-hand.md",
    "stone dragon": "stone-dragon.md",
    "tiger claw": "tiger-claw.md",
    "white raven": "white-raven.md"
}

KNOWN_HEADERS = {
    "Level", "Class", "Prerequisite", "Prerequisites", "Initiation Action", "Range",
    "Target", "Targets", "Area", "Effect", "Duration", "Saving Throw", "Save"
}

KNOWN_HYPHENATED = {
    "bone-crushing", "evil-aligned", "good-aligned", "flat-footed", "one-handed", "two-handed"
}

disc_re = re.compile(rf"^({'|'.join(DISCIPLINES)})\s*\((Strike|Boost|Counter|Stance|Other)\)(?:\s*\[([^\]]+)\])?", re.IGNORECASE)


def clean_ligatures(text: str) -> str:
    """Resolve typographic ligatures and normalize quotation marks and dashes."""
    text = text.replace("ﬁ ", "fi").replace("ﬂ ", "fl")
    text = text.replace("ﬁ", "fi").replace("ﬂ", "fl")
    text = text.replace("’", "'").replace("‘", "'")
    text = text.replace("—", "—").replace("–", "–")
    return text


def is_title_line(line_dict: dict) -> bool:
    """Return True if line has maneuver title font and size."""
    return "LeashXL" in line_dict.get("font", "") and 11.0 <= line_dict.get("size", 0.0) <= 12.0


def append_header_text(existing: str, next_line: str) -> str:
    """Append multi-line header continuation, resolving soft hyphens."""
    next_line = next_line.strip()
    if not existing:
        return next_line
    if existing.endswith("-"):
        w1_match = re.search(r"([A-Za-z]+)-$", existing)
        w2_match = re.match(r"^([A-Za-z]+)", next_line)
        if w1_match and w2_match:
            compound = f"{w1_match.group(1).lower()}-{w2_match.group(1).lower()}"
            if compound in KNOWN_HYPHENATED:
                return existing + next_line
        return existing[:-1] + next_line
    return existing + " " + next_line


def clean_line_breaks(text: str) -> str:
    """Join line breaks and resolve soft hyphens while preserving intended compound words."""
    def replace_hyphen(m):
        w1, w2 = m.group(1), m.group(2)
        lower_compound = f"{w1.lower()}-{w2.lower()}"
        if lower_compound in KNOWN_HYPHENATED:
            return f"{w1}-{w2}"
        return f"{w1}{w2}"

    text = re.sub(r"([A-Za-z]+)-\s*\n\s*([A-Za-z]+)", replace_hyphen, text)
    text = re.sub(r"([A-Za-z]+)-\s+([A-Za-z]+)", replace_hyphen, text)
    text = re.sub(r"\s*\n\s*", " ", text)
    return text.strip()


def title_case_name(name: str) -> str:
    """Format maneuver names into standard Title Case while preserving hyphens and contractions."""
    words = name.split()
    tc = []
    for w in words:
        if "-" in w:
            tc.append("-".join(part.capitalize() for part in w.split("-")))
        elif "'" in w:
            parts = w.split("'")
            tc.append(parts[0].capitalize() + "'" + parts[1].lower())
        else:
            if w.upper() in ["OF", "THE", "IN", "AND", "FROM", "AT", "TO", "A", "AN"] and tc:
                tc.append(w.lower())
            else:
                tc.append(w.capitalize())
    res = " ".join(tc)
    res = re.sub(r"\s*,\s*", ", ", res)
    return res


def format_level(level_str: str) -> str:
    """Capitalize initiator class names in the Level field."""
    level_str = clean_line_breaks(level_str)
    words = level_str.split()
    fixed = []
    for w in words:
        low = w.lower().rstrip(",")
        has_comma = w.endswith(",")
        if low in ["crusader", "swordsage", "warblade"]:
            fixed.append(low.capitalize() + ("," if has_comma else ""))
        else:
            fixed.append(w)
    return " ".join(fixed)


def format_meta_field(name: str, val: str) -> str:
    """Format metadata field prefix for folding into Mechanics."""
    val = clean_line_breaks(val).strip()
    if val.endswith("."):
        return f"{name}: {val}"
    return f"{name}: {val}."


def extract_stream_lines(doc: pymupdf.Document):
    """Extract ordered text lines with font and styling metadata from Chapter 4 (pp. 53-95)."""
    all_lines = []
    for p in range(52, 95): # pages 53 to 95 in book
        page = doc[p]
        page_num = p # printed page number
        d = page.get_text("dict")
        for b in d["blocks"]:
            if b.get("type") == 0 and "lines" in b:
                block_text = clean_ligatures("".join(s["text"] for l in b["lines"] for s in l["spans"])).strip()
                if "Illus. by" in block_text or "FRFell" in str(b) or "Pterra" in str(b):
                    continue
                if b["bbox"][1] > 750 or (b["bbox"][1] < 60 and len(block_text) < 10):
                    continue
                if "LeashXL" in str(b) and any(s["size"] > 15 for l in b["lines"] for s in l["spans"]):
                    continue
                if "INCORPOREAL SUBTYPE" in block_text:
                    continue
                if "key skill" in block_text.lower() and any(d.lower() in block_text.lower() for d in DISCIPLINES):
                    continue
                for line in b["lines"]:
                    spans = line["spans"]
                    line_text = clean_ligatures("".join(s["text"] for s in spans)).strip()
                    if not line_text:
                        continue
                    primary_font = spans[0]["font"]
                    primary_size = round(spans[0]["size"], 1)
                    first_is_italic = "It" in primary_font or (spans[0]["flags"] & 2 != 0)
                    any_is_italic = any("It" in s["font"] or (s["flags"] & 2 != 0) for s in spans if s["text"].strip())
                    is_all_italic = all("It" in s["font"] or (s["flags"] & 2 != 0) for s in spans if s["text"].strip())
                    all_lines.append({
                        "bbox": line["bbox"],
                        "text": line_text,
                        "font": primary_font,
                        "size": primary_size,
                        "first_is_italic": first_is_italic,
                        "any_is_italic": any_is_italic,
                        "is_all_italic": is_all_italic,
                        "page": page_num
                    })
    return all_lines


def parse_maneuvers(all_lines):
    """Parse raw line stream into structured maneuver records."""
    maneuvers = []
    i = 0
    n = len(all_lines)

    while i < n:
        line = all_lines[i]
        if is_title_line(line):
            title_lines = [line["text"]]
            title_page = line["page"]
            j = i + 1
            while j < n and is_title_line(all_lines[j]):
                title_lines.append(all_lines[j]["text"])
                j += 1
            
            if j < n:
                m = disc_re.match(all_lines[j]["text"])
                if m:
                    raw_title = " ".join(title_lines)
                    clean_title = title_case_name(raw_title)
                    disc_line = all_lines[j]["text"]
                    disc_match = m
                    j += 1
                    
                    header_fields = {}
                    current_field = None
                    
                    while j < n:
                        cur = all_lines[j]
                        if is_title_line(cur):
                            break
                        if cur["first_is_italic"] or cur["any_is_italic"]:
                            break
                        
                        text = cur["text"]
                        if text == "Initiation" and j + 1 < n and all_lines[j+1]["text"].startswith("Action:"):
                            text = "Initiation " + all_lines[j+1]["text"]
                            j += 1
                        
                        f_match = re.match(r"^([A-Z][A-Za-z ]+):\s*(.*)", text)
                        if f_match and f_match.group(1).strip() in KNOWN_HEADERS:
                            key = f_match.group(1).strip()
                            if key == "Class":
                                key = "Level"
                            elif key == "Save":
                                key = "Saving Throw"
                            elif key == "Targets":
                                key = "Target"
                            elif key == "Prerequisites":
                                key = "Prerequisite"
                            current_field = key
                            val = f_match.group(2).strip()
                            header_fields[current_field] = val
                        elif current_field:
                            header_fields[current_field] = append_header_text(header_fields[current_field], text)
                        else:
                            break
                        j += 1
                    
                    # Flavor text lines: lines before mechanics starts
                    flavor_lines = []
                    while j < n:
                        cur = all_lines[j]
                        if is_title_line(cur):
                            break
                        if cur["first_is_italic"] or cur["any_is_italic"]:
                            flavor_lines.append(cur["text"])
                            j += 1
                        else:
                            break
                    
                    # Mechanics lines: rules text until next maneuver
                    mechanics_lines = []
                    while j < n:
                        cur = all_lines[j]
                        if is_title_line(cur):
                            k = j + 1
                            while k < n and is_title_line(all_lines[k]):
                                k += 1
                            if k < n and disc_re.match(all_lines[k]["text"]):
                                break
                        mechanics_lines.append(cur["text"])
                        j += 1
                    
                    maneuvers.append({
                        "name": clean_title,
                        "page": title_page,
                        "discipline_line": disc_line,
                        "discipline": disc_match.group(1),
                        "headers": header_fields,
                        "flavor": " ".join(flavor_lines),
                        "mechanics_raw": "\n".join(mechanics_lines)
                    })
                    i = j
                    continue
        i += 1
    return maneuvers


def write_discipline_files(maneuvers, output_dir: Path):
    """Write parsed maneuvers to discipline markdown files using 6-field reference entry schema."""
    output_dir.mkdir(parents=True, exist_ok=True)
    
    disc_maneuvers = {}
    for m in maneuvers:
        d = m["discipline"].lower()
        fname = DISC_MAP[d]
        if fname not in disc_maneuvers:
            disc_maneuvers[fname] = []
        disc_maneuvers[fname].append(m)

    for fname, m_list in sorted(disc_maneuvers.items()):
        m_list.sort(key=lambda x: x["name"])
        disc_title = m_list[0]["discipline"]
        filepath = output_dir / fname
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(f"# {disc_title} Maneuvers and Stances\n\n")
            f.write("Primary Source: *Tome of Battle: The Book of Nine Swords*, Chapter 4.\n\n")
            for m in m_list:
                headers = m["headers"]
                level = format_level(headers.get("Level", ""))
                init_action = clean_line_breaks(headers.get("Initiation Action", "1 standard action"))
                prereq = clean_line_breaks(headers.get("Prerequisite", "None"))
                if not prereq:
                    prereq = "None"
                
                mech_parts = []
                for field in ["Range", "Target", "Area", "Effect", "Duration", "Saving Throw"]:
                    if field in headers and headers[field].strip():
                        mech_parts.append(format_meta_field(field, headers[field]))
                
                raw_rules = clean_line_breaks(m["mechanics_raw"])
                if raw_rules:
                    mech_parts.append(raw_rules)
                
                mechanics_str = " ".join(mech_parts)
                
                f.write(f"### {m['name']}\n")
                f.write(f"- **Source**: *Tome of Battle*, p. {m['page']}\n")
                f.write(f"- **Discipline**: {m['discipline_line']}\n")
                f.write(f"- **Level**: {level}\n")
                f.write(f"- **Initiation Action**: {init_action}\n")
                f.write(f"- **Prerequisites**: {prereq}\n")
                f.write(f"- **Mechanics**: {mechanics_str}\n\n")


def main():
    parser = argparse.ArgumentParser(description="Extract martial maneuvers from Tome of Battle into discipline markdown files.")
    parser.add_argument("--pdf", default=DEFAULT_PDF_PATH, help="Path to Tome of Battle PDF")
    default_out = Path(__file__).resolve().parent.parent / "docs" / "rules" / "content" / "maneuvers"
    parser.add_argument("--output", default=str(default_out), help="Target output directory")
    args = parser.parse_args()

    pdf_path = Path(args.pdf)
    if not pdf_path.exists():
        sys.exit(f"Error: PDF not found at '{pdf_path}'.")

    output_dir = Path(args.output)
    print(f"Opening '{pdf_path}'...")
    doc = pymupdf.open(str(pdf_path))
    print(f"Extracting stream lines from Chapter 4...")
    all_lines = extract_stream_lines(doc)
    print(f"Parsing maneuvers from {len(all_lines)} lines...")
    maneuvers = parse_maneuvers(all_lines)
    print(f"Total maneuvers parsed: {len(maneuvers)}")

    print(f"Writing discipline reference files to '{output_dir}'...")
    write_discipline_files(maneuvers, output_dir)
    print("Done!")


if __name__ == "__main__":
    main()
