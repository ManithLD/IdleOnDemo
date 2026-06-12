from pathlib import Path
import shutil
import zipfile
import re
from collections import defaultdict

ASSETS = Path(r"D:\Unity Projects\IdleOnDemo\Assets")
MOVE_THESE = ASSETS / "MOVE THESE"
STAGING = ASSETS / "_Staging"

KEEP_EXTS = {".png", ".jpg", ".jpeg", ".ttf", ".otf"}
SKIP_EXTS = {".gd", ".tscn", ".tres", ".import", ".aseprite"}

DESTS = {
    "player": ASSETS / "Art" / "Characters" / "Player",
    "enemies": ASSETS / "Art" / "Characters" / "Enemies",
    "tilesets": ASSETS / "Art" / "Environment" / "Tilesets",
    "backgrounds": ASSETS / "Art" / "Environment" / "Backgrounds",
    "icons": ASSETS / "Art" / "UI" / "Icons",
    "hud": ASSETS / "Art" / "UI" / "HUD",
    "panels": ASSETS / "Art" / "UI" / "Panels",
    "fonts": ASSETS / "Art" / "UI" / "Fonts",
    "items": ASSETS / "Art" / "Items",
}

REQUIRED_DIRS = [
    *DESTS.values(),
    ASSETS / "Art" / "VFX",
    ASSETS / "ScriptableObjects",
    ASSETS / "Tiles",
    ASSETS / "Resources",
]

def safe_name(name: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]+", "_", name).strip("_")

def source_label(zip_path: Path) -> str:
    stem = zip_path.stem
    compact = re.sub(r"[^A-Za-z0-9]+", "_", stem).strip("_")
    return compact or "Source"

def is_inside_macosx(path: Path) -> bool:
    return any(part == "__MACOSX" for part in path.parts)

def normalized(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", " ", text.lower())

def classify_sunny(path: Path):
    text = normalized(str(path))

    background_words = [
        "background", "parallax", "back", "bg", "layer", "sky",
        "cloud", "mountain", "forest", "horizon"
    ]
    enemy_words = [
        "enemy", "enemies", "slime", "monster", "creature",
        "opossum", "eagle", "frog", "bat", "bee", "snail"
    ]
    player_words = [
        "player", "fox", "hero", "character", "idle", "run",
        "walk", "jump", "fall", "hurt", "hit", "attack", "death"
    ]
    tileset_words = [
        "tile", "tileset", "terrain", "ground", "platform",
        "dirt", "grass", "props", "decor", "bridge"
    ]

    if any(word in text for word in background_words):
        return "backgrounds"
    if any(word in text for word in enemy_words):
        return "enemies"
    if any(word in text for word in player_words):
        return "player"
    if any(word in text for word in tileset_words):
        return "tilesets"

    return "tilesets"

def classify_file(zip_stem: str, relative_path: Path):
    z = normalized(zip_stem)
    ext = relative_path.suffix.lower()

    if "sunny land" in z or "sunny land files" in z or "sunny-land-files" in zip_stem.lower():
        if ext in {".png", ".jpg", ".jpeg"}:
            return classify_sunny(relative_path)

    if "monster creatures fantasy" in z or "monsters creatures fantasy" in z:
        if ext == ".png":
            return "enemies"

    if "male hero free" in z:
        if ext == ".png":
            return "player"

    if "multi platformer tileset free" in z:
        if ext == ".png":
            return "tilesets"

    if "kyrises 16x16 rpg icon pack" in z:
        if ext == ".png":
            return "icons"

    if "free raven fantasy icons" in z or "raven fantasy icons" in z:
        if ext == ".png":
            return "icons"

    if "uibundlefree" in z or "ui bundle free" in z:
        if ext == ".png":
            return "panels"

    if "pixel ui pack 3" in z:
        if ext == ".png":
            return "hud"

    if "cryo s mini gui" in z or "cryos mini gui" in z or "cryo mini gui" in z:
        if ext == ".png":
            return "panels"

    if "thaleahfat" in z or "thaleah fat" in z:
        if ext in {".ttf", ".otf"}:
            return "fonts"

    if "coin gems" in z or "coin_gems" in zip_stem.lower():
        if ext == ".png":
            return "items"

    return None

def unique_destination(dest_dir: Path, filename: str, label: str, collisions):
    candidate = dest_dir / filename

    if not candidate.exists():
        return candidate

    prefixed = dest_dir / f"{label}_{filename}"
    if not prefixed.exists():
        collisions.append((str(candidate), str(prefixed)))
        return prefixed

    stem = Path(filename).stem
    suffix = Path(filename).suffix
    i = 2
    while True:
        numbered = dest_dir / f"{label}_{stem}_{i}{suffix}"
        if not numbered.exists():
            collisions.append((str(candidate), str(numbered)))
            return numbered
        i += 1

def should_skip_file(path: Path):
    ext = path.suffix.lower()

    if is_inside_macosx(path):
        return "inside __MACOSX"
    if ext in SKIP_EXTS:
        return f"skipped extension {ext}"
    if ext not in KEEP_EXTS:
        return f"not an allowed asset type {ext or '[no extension]'}"

    return None

def main():
    summary = defaultdict(int)
    skipped = []
    errors = []
    fallback = []
    collisions = []

    if not MOVE_THESE.exists():
        raise FileNotFoundError(f"Missing source folder: {MOVE_THESE}")

    STAGING.mkdir(parents=True, exist_ok=True)

    for directory in REQUIRED_DIRS:
        directory.mkdir(parents=True, exist_ok=True)

    zip_files = sorted(MOVE_THESE.glob("*.zip"))
    if not zip_files:
        print(f"No zip files found in {MOVE_THESE}")
        return

    extracted_roots = []

    for zip_path in zip_files:
        label = source_label(zip_path)
        extract_root = STAGING / label
        extract_root.mkdir(parents=True, exist_ok=True)

        try:
            with zipfile.ZipFile(zip_path, "r") as zf:
                zf.extractall(extract_root)
            extracted_roots.append((zip_path, extract_root))
        except Exception as exc:
            errors.append(f"Failed to extract {zip_path}: {exc}")

    for zip_path, extract_root in extracted_roots:
        label = source_label(zip_path)

        for file_path in extract_root.rglob("*"):
            if not file_path.is_file():
                continue

            skip_reason = should_skip_file(file_path)
            if skip_reason:
                skipped.append(f"{file_path} -> {skip_reason}")
                continue

            rel = file_path.relative_to(extract_root)
            category = classify_file(zip_path.stem, rel)

            if category is None:
                fallback.append(f"{file_path} -> skipped, no mapping matched")
                skipped.append(f"{file_path} -> no mapping matched")
                continue

            dest_dir = DESTS[category]
            dest_path = unique_destination(dest_dir, file_path.name, label, collisions)

            try:
                shutil.move(str(file_path), str(dest_path))
                summary[str(dest_dir.relative_to(ASSETS))] += 1
            except Exception as exc:
                errors.append(f"Failed to move {file_path} -> {dest_path}: {exc}")

    try:
        if STAGING.exists():
            shutil.rmtree(STAGING)
    except Exception as exc:
        errors.append(f"Failed to delete staging folder {STAGING}: {exc}")

    try:
        if MOVE_THESE.exists():
            shutil.rmtree(MOVE_THESE)
    except Exception as exc:
        errors.append(f"Failed to delete source folder {MOVE_THESE}: {exc}")

    print("\n=== Placement Summary ===")
    for dest in sorted(DESTS.values(), key=lambda p: str(p)):
        key = str(dest.relative_to(ASSETS))
        print(f"{key}: {summary[key]} files")

    print("\n=== Files That Did Not Fit Mapping ===")
    if fallback:
        for item in fallback:
            print(item)
    else:
        print("None")

    print("\n=== Skipped Files ===")
    if skipped:
        for item in skipped:
            print(item)
    else:
        print("None")

    print("\n=== Filename Collisions Resolved ===")
    if collisions:
        for original, resolved in collisions:
            print(f"{original} -> {resolved}")
    else:
        print("None")

    print("\n=== Errors ===")
    if errors:
        for item in errors:
            print(item)
    else:
        print("None")

if __name__ == "__main__":
    main()
