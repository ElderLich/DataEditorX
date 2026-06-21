#!/usr/bin/env python3
"""Import MDPro3 locale card terms into DataEditorX data files."""

from __future__ import annotations

import argparse
import re
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOCALE_ROOT = Path(r"D:\Programs\GitHub\MDPro3_Data\Data\locales")
DEFAULT_FALLBACK_DATA = Path(r"D:\Modding\Programs\DataEditor\data")


@dataclass(frozen=True)
class Locale:
    folder: str
    cardinfo_name: str
    mse_name: str
    mse_language: str
    base_cardinfo: str


LOCALES = [
    Locale("de-DE", "german", "German", "DE", "english"),
    Locale("en-US", "english", "English", "EN", "english"),
    Locale("es-ES", "spanish", "Spanish", "ES", "english"),
    Locale("fr-FR", "french", "French", "FR", "english"),
    Locale("it-IT", "italian", "Italian", "IT", "english"),
    Locale("ja-JP", "japanese", "Japanese", "JP", "english"),
    Locale("ko-KR", "korean", "Korean", "KR", "english"),
    Locale("pt-PT", "portuguese", "Portuguese", "PT", "english"),
    Locale("th-TH", "thai", "Thai", "TH", "english"),
    Locale("zh-CN", "chinese", "Chinese-Simplified", "CN", "chinese"),
    Locale("zh-TW", "chinese-traditional", "Chinese-Traditional", "TW", "english"),
]

EXTRA_MSE_VARIANTS = [
    Locale("en-US", "english", "English-Omega", "EN", "english"),
]


ATTRIBUTE_SYSTEM_IDS = {
    0x1: 1010,
    0x2: 1011,
    0x4: 1012,
    0x8: 1013,
    0x10: 1014,
    0x20: 1015,
    0x40: 1016,
}

RACE_SYSTEM_IDS = {
    0x1: 1020,
    0x2: 1021,
    0x4: 1022,
    0x8: 1023,
    0x10: 1024,
    0x20: 1025,
    0x40: 1026,
    0x80: 1027,
    0x100: 1028,
    0x200: 1029,
    0x400: 1030,
    0x800: 1031,
    0x1000: 1032,
    0x2000: 1033,
    0x4000: 1034,
    0x8000: 1035,
    0x10000: 1036,
    0x20000: 1037,
    0x40000: 1038,
    0x80000: 1039,
    0x100000: 1040,
    0x200000: 1041,
    0x400000: 1042,
    0x800000: 1043,
    0x1000000: 1044,
    0x2000000: 1045,
}

TYPE_SYSTEM_IDS = {
    0x1: 1050,
    0x2: 1051,
    0x4: 1052,
    0x10: 1054,
    0x20: 1055,
    0x40: 1056,
    0x80: 1057,
    0x100: 1058,
    0x200: 1059,
    0x400: 1060,
    0x800: 1061,
    0x1000: 1062,
    0x2000: 1063,
    0x4000: 1064,
    0x10000: 1066,
    0x20000: 1067,
    0x40000: 1068,
    0x80000: 1069,
    0x100000: 1070,
    0x200000: 1071,
    0x400000: 1072,
    0x800000: 1073,
    0x1000000: 1074,
    0x2000000: 1075,
    0x4000000: 1076,
}

CATEGORY_SYSTEM_IDS = {1 << index: 1100 + index for index in range(32)}

RACE_ORDER = list(RACE_SYSTEM_IDS)
GENERATED_CARDINFO_MARKER = "# Generated from MDPro3 locale strings.conf"


TYPE_ORDER = [
    0x1,
    0x2,
    0x4,
    0x8,
    0x10,
    0x20,
    0x40,
    0x80,
    0x100,
    0x200,
    0x400,
    0x800,
    0x1000,
    0x2000,
    0x4000,
    0x8000,
    0x10000,
    0x20000,
    0x40000,
    0x80000,
    0x100000,
    0x200000,
    0x400000,
    0x800000,
    0x1000000,
    0x2000000,
    0x4000000,
]


@dataclass
class CardInfo:
    preamble: list[str]
    sections: list[tuple[str, list[tuple[int, str]]]]


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    try:
        path.write_text(text, encoding="utf-8", newline="\n")
    except PermissionError:
        temp = path.with_name(f"{path.name}.tmp")
        temp.write_text(text, encoding="utf-8", newline="\n")
        temp.replace(path)


def parse_id(value: str) -> int:
    return int(value, 0)


def format_id(value: int) -> str:
    return str(value) if value < 0 else f"0x{value:x}"


def clean_label(value: str) -> str:
    return value.strip().rstrip(":：").strip()


def parse_mdpro3_strings(path: Path) -> tuple[dict[int, str], list[tuple[int, str]]]:
    systems: dict[int, str] = {}
    setnames: list[tuple[int, str]] = []
    for line in read_text(path).splitlines():
        if not (line.startswith("!") or line.startswith("#setname ") or line.startswith("#!setname ")):
            continue

        parts = line.split(maxsplit=2)
        if len(parts) != 3:
            continue

        tag, key, value = parts
        if tag == "!system":
            systems[parse_id(key)] = value.strip()
        elif tag in ("!setname", "#setname", "#!setname"):
            setnames.append((parse_id(key), value.strip()))

    return systems, setnames


def parse_cardinfo(path: Path) -> CardInfo:
    preamble: list[str] = []
    sections: list[tuple[str, list[tuple[int, str]]]] = []
    current_name: str | None = None
    current_entries: list[tuple[int, str]] = []

    for line in read_text(path).splitlines():
        if line == "#end":
            break

        if line.startswith("##"):
            if current_name is not None:
                sections.append((current_name, current_entries))
            current_name = line[2:].strip()
            current_entries = []
            continue

        if current_name is None:
            if line == GENERATED_CARDINFO_MARKER:
                continue
            preamble.append(line)
            continue

        if "\t" not in line:
            continue

        key, value = line.split("\t", 1)
        key_parts = key.split(maxsplit=1)
        if len(key_parts) == 2:
            key, value = key_parts[0], key_parts[1]
        current_entries.append((parse_id(key), value.rstrip()))

    if current_name is not None:
        sections.append((current_name, current_entries))

    return CardInfo(preamble, sections)


def entries_to_dict(entries: list[tuple[int, str]]) -> dict[int, str]:
    return dict(entries)


def apply_system_terms(
    entries: list[tuple[int, str]],
    system_ids: dict[int, int],
    systems: dict[int, str],
    *,
    leading_label: str | None = None,
) -> list[tuple[int, str]]:
    result: list[tuple[int, str]] = []
    seen: set[int] = set()

    for key, value in entries:
        if key == 0 and leading_label:
            value = leading_label
        elif key in system_ids and system_ids[key] in systems:
            value = systems[system_ids[key]]
        result.append((key, value))
        seen.add(key)

    for key, system_id in system_ids.items():
        if key not in seen and system_id in systems:
            result.append((key, systems[system_id]))

    return result


def merge_setnames(
    base_entries: list[tuple[int, str]],
    systems: dict[int, str],
    md_setnames: list[tuple[int, str]],
) -> list[tuple[int, str]]:
    base = entries_to_dict(base_entries)
    result: list[tuple[int, str]] = []
    seen: set[int] = set()

    for key in (-1, 0):
        if key in base:
            value = base[key]
        elif key == -1:
            value = "Custom"
        else:
            value = clean_label(systems.get(1329, "Archetype"))
        result.append((key, value))
        seen.add(key)

    for key, value in md_setnames:
        if key not in seen:
            result.append((key, value))
            seen.add(key)

    return result


def build_cardinfo(base: CardInfo, systems: dict[int, str], md_setnames: list[tuple[int, str]]) -> str:
    attribute_label = clean_label(systems.get(1319, "Attribute"))
    race_label = clean_label(systems.get(1321, "Race"))

    lines = [GENERATED_CARDINFO_MARKER, *base.preamble]

    for section_name, entries in base.sections:
        lowered = section_name.lower()
        if lowered == "attribute":
            entries = apply_system_terms(
                entries,
                ATTRIBUTE_SYSTEM_IDS,
                systems,
                leading_label=attribute_label,
            )
        elif lowered == "race":
            entries = apply_system_terms(
                entries,
                RACE_SYSTEM_IDS,
                systems,
                leading_label=race_label,
            )
        elif lowered == "type":
            entries = apply_system_terms(entries, TYPE_SYSTEM_IDS, systems)
        elif lowered.startswith("category"):
            entries = apply_system_terms(entries, CATEGORY_SYSTEM_IDS, systems)
        elif lowered == "setname":
            entries = merge_setnames(entries, systems, md_setnames)

        lines.append(f"##{section_name}")
        for key, value in entries:
            lines.append(f"{format_id(key)}\t{value}")

    lines.append("#end")
    return "\n".join(lines) + "\n"


def find_base_cardinfo(data_dir: Path, fallback_data: Path, name: str) -> Path:
    candidates = [
        fallback_data / f"cardinfo_{name}.txt",
        data_dir / "languages" / f"cardinfo_{name}.txt",
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    raise FileNotFoundError(f"Missing base cardinfo_{name}.txt")


def extract_mse_values(lines: list[str], prefix: str) -> dict[int, str]:
    values: dict[int, str] = {}
    needle = f"{prefix} "
    for line in lines:
        if not line.startswith(needle):
            continue
        parts = line.split(maxsplit=2)
        if len(parts) == 3:
            values[parse_id(parts[1])] = parts[2].strip()
    return values


def system_value(
    key: int,
    system_ids: dict[int, int],
    systems: dict[int, str],
    fallback: dict[int, str],
) -> str:
    system_id = system_ids.get(key)
    if system_id is not None and system_id in systems:
        return systems[system_id]
    return fallback.get(key, "N/A")


def ordered_keys(primary: list[int], existing: dict[int, str], system_ids: dict[int, int]) -> list[int]:
    keys = list(primary)
    for key in existing:
        if key not in keys:
            keys.append(key)
    for key in system_ids:
        if key not in keys:
            keys.append(key)
    return keys


def update_mse_head(lines: list[str], locale: Locale) -> list[str]:
    updated: list[str] = []
    language_pattern = re.compile(r"(\\tlanguage: )[^\\r]+")
    replace_language_comment = False
    for line in lines:
        if line.startswith("# Magic Set Editor 2"):
            updated.append(line)
            replace_language_comment = True
        elif replace_language_comment and line.startswith("#"):
            updated.append(f"# {locale.mse_name}")
            replace_language_comment = False
        elif line.startswith("head = "):
            updated.append(language_pattern.sub(rf"\g<1>{locale.mse_language}", line))
        else:
            updated.append(line)
    return updated


def build_mse(
    template_lines: list[str],
    systems: dict[int, str],
    locale: Locale,
    english_mse_values: tuple[dict[int, str], dict[int, str]],
) -> str:
    template_lines = update_mse_head(template_lines, locale)
    race_values = extract_mse_values(template_lines, "race")
    type_values = extract_mse_values(template_lines, "type")
    english_race_values, english_type_values = english_mse_values

    race_fallback = english_race_values | race_values
    type_fallback = english_type_values | type_values
    race_keys = ordered_keys(RACE_ORDER, race_values, RACE_SYSTEM_IDS)
    type_keys = ordered_keys(TYPE_ORDER, type_values, TYPE_SYSTEM_IDS)

    try:
        race_index = template_lines.index("##race")
    except ValueError:
        race_index = len(template_lines)

    before_race = template_lines[:race_index]
    lines = list(before_race)
    lines.append("##race")
    for key in race_keys:
        lines.append(f"race {format_id(key)} {system_value(key, RACE_SYSTEM_IDS, systems, race_fallback)}")

    lines.append("###########################")
    lines.append("##type")
    for key in type_keys:
        lines.append(f"type {format_id(key)} {system_value(key, TYPE_SYSTEM_IDS, systems, type_fallback)}")
    lines.append("##########################")

    return "\n".join(lines).rstrip() + "\n"


def find_mse_template(data_dir: Path, fallback_data: Path, mse_name: str) -> Path:
    candidates = [
        data_dir / "mse" / f"mse_{mse_name}.txt",
        fallback_data / f"mse_{mse_name}.txt",
        data_dir / "mse" / "mse_English.txt",
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    raise FileNotFoundError("Missing mse_English.txt template")


def import_locales(locale_root: Path, data_dir: Path, fallback_data: Path) -> None:
    english_template = read_text(find_mse_template(data_dir, fallback_data, "English")).splitlines()
    english_mse_values = (
        extract_mse_values(english_template, "race"),
        extract_mse_values(english_template, "type"),
    )

    parsed_cardinfo: dict[str, CardInfo] = {}
    for name in {locale.base_cardinfo for locale in LOCALES}:
        parsed_cardinfo[name] = parse_cardinfo(find_base_cardinfo(data_dir, fallback_data, name))

    parsed_strings: dict[str, tuple[dict[int, str], list[tuple[int, str]]]] = {}

    for locale in LOCALES:
        locale_file = locale_root / locale.folder / "strings.conf"
        if not locale_file.exists():
            raise FileNotFoundError(locale_file)

        systems, setnames = parse_mdpro3_strings(locale_file)
        parsed_strings[locale.folder] = (systems, setnames)
        cardinfo = build_cardinfo(parsed_cardinfo[locale.base_cardinfo], systems, setnames)
        write_text(data_dir / "languages" / f"cardinfo_{locale.cardinfo_name}.txt", cardinfo)

        mse_template = read_text(find_mse_template(data_dir, fallback_data, locale.mse_name)).splitlines()
        mse = build_mse(mse_template, systems, locale, english_mse_values)
        write_text(data_dir / "mse" / f"mse_{locale.mse_name}.txt", mse)

    for locale in EXTRA_MSE_VARIANTS:
        systems, _ = parsed_strings[locale.folder]
        mse_template = read_text(find_mse_template(data_dir, fallback_data, locale.mse_name)).splitlines()
        mse = build_mse(mse_template, systems, locale, english_mse_values)
        write_text(data_dir / "mse" / f"mse_{locale.mse_name}.txt", mse)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--locale-root", type=Path, default=DEFAULT_LOCALE_ROOT)
    parser.add_argument("--data-dir", type=Path, default=REPO_ROOT / "DataEditorX" / "data")
    parser.add_argument("--fallback-data", type=Path, default=DEFAULT_FALLBACK_DATA)
    args = parser.parse_args()

    import_locales(args.locale_root, args.data_dir, args.fallback_data)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
