from pathlib import Path
import shutil
from collections import defaultdict

ASSETS = Path(r"D:\Unity Projects\IdleOnDemo\Assets")
ART = ASSETS / "Art"

IMAGE_EXTS = {".png", ".jpg", ".jpeg"}
FONT_EXTS = {".ttf", ".otf"}
STRAY_EXTS = {".gd", ".tscn", ".tres", ".import", ".aseprite"}

DESTS = {
    "Characters/Player": ART / "Characters" / "Player",
    "Characters/Enemies": ART / "Characters" / "Enemies",
    "Environment/Tilesets": ART / "Environment" / "Tilesets",
    "Environment/Backgrounds": ART / "Environment" / "Backgrounds",
    "UI/Icons": ART / "UI" / "Icons",
    "UI/HUD": ART / "UI" / "HUD",
    "UI/Panels": ART / "UI" / "Panels",
    "UI/Fonts": ART / "UI" / "Fonts",
    "VFX": ART / "VFX",
    "Items": ART / "Items",
}

EXPECTED_ART_DIRS = {
    ART,
    ART / "Characters",
    ART / "Characters" / "Player",
    ART / "Characters" / "Enemies",
    ART / "Environment",
    ART / "Environment" / "Tilesets",
    ART / "Environment" / "Backgrounds",
    ART / "UI",
    ART / "UI" / "Icons",
    ART / "UI" / "HUD",
    ART / "UI" / "Panels",
    ART / "UI" / "Fonts",
    ART / "VFX",
    ART / "Items",
}

def rel(path: Path) -> str:
    try:
        return str(path.relative_to(ASSETS))
    except ValueError:
        return str(path)

def normalized(path: Path) -> str:
    return str(path).replace("\\", "/").lower()

def tokens(path: Path):
    text = normalized(path)
    for ch in "/\\._-()[]{}' ":
        text = text.replace(ch, " ")
    return {part for part in text.split() if part}

def unique_dest(dest_dir: Path, source: Path) -> Path:
    dest = dest_dir / source.name
    if not dest.exists():
        return dest

    stem = source.stem
    suffix = source.suffix
    parent_hint = source.parent.name.replace(" ", "_").replace("'", "").replace("(", "").replace(")", "")
    candidate = dest_dir / f"{parent_hint}_{source.name}"
    if not candidate.exists():
        return candidate

    i = 2
    while True:
        candidate = dest_dir / f"{parent_hint}_{stem}_{i}{suffix}"
        if not candidate.exists():
            return candidate
        i += 1

def is_inside(path: Path, folder: Path) -> bool:
    try:
        path.relative_to(folder)
        return True
    except ValueError:
        return False

def classify_asset(path: Path):
    text = normalized(path.relative_to(ASSETS))
    words = tokens(path.relative_to(ASSETS))
    name = path.name.lower()
    ext = path.suffix.lower()

    if ext in FONT_EXTS:
        return DESTS["UI/Fonts"], "font file"

    if ext not in IMAGE_EXTS:
        return None, None

    if any(token in text for token in ["kyrises", "raven fantasy icons", "/icons/"]) or {"icon", "icons"} & words:
        return DESTS["UI/Icons"], "icon asset"

    if any(token in text for token in ["pixel ui pack 3", "scrollbar", "scroll bar"]) or {"health", "hud", "bar"} & words:
        return DESTS["UI/HUD"], "HUD image"

    if any(token in text for token in ["uibundle", "cryo", "/ui/"]) or {"panel", "button", "window", "dialog", "slider", "checkbox"} & words:
        return DESTS["UI/Panels"], "UI panel/button image"

    if {"coin", "gem", "pickup", "pickups", "item", "items"} & words:
        return DESTS["Items"], "item image"

    if {"monster", "monsters", "creature", "creatures", "enemy", "enemies", "eagle", "frog", "oposum", "opossum", "slime", "bat", "rat", "projectile"} & words:
        return DESTS["Characters/Enemies"], "enemy sprite"

    if any(token in text for token in ["male_hero", "male-hero", "/player/"]) or {"player", "hero", "fox"} & words:
        return DESTS["Characters/Player"], "player sprite"

    character_anim_names = [
        "idle", "walk", "run", "jump", "fall", "hurt", "attack", "death",
        "dizzy", "lookup", "combo", "spritesheet", "roll", "victory", "wall", "grab"
    ]
    if any(token in words for token in character_anim_names):
        return DESTS["Characters/Player"], "character-like sprite"

    if any(token in text for token in ["background", "parallax", "/background/", "back.png", "middle.png"]) or {"background", "backgrounds", "sky", "cloud", "clouds", "horizon", "layer", "layers", "back", "middle"} & words:
        return DESTS["Environment/Backgrounds"], "background image"

    if {"tileset", "tilesets", "tile", "tiles", "terrain", "ground", "platform", "platforms", "props", "grass", "dirt", "world"} & words:
        return DESTS["Environment/Tilesets"], "tileset/environment image"

    return None, None

def should_be_moved(path: Path, dest_dir: Path) -> bool:
    if dest_dir is None:
        return False
    return not is_inside(path, dest_dir)

def move_with_meta(source: Path, dest_dir: Path, reason: str, moved, collisions):
    dest_dir.mkdir(parents=True, exist_ok=True)
    dest = unique_dest(dest_dir, source)

    if dest.name != source.name:
        collisions.append(f"{rel(source)} -> {rel(dest)}")

    old_meta = Path(str(source) + ".meta")
    new_meta = Path(str(dest) + ".meta")

    shutil.move(str(source), str(dest))
    meta_moved = False
    if old_meta.exists() and not new_meta.exists():
        shutil.move(str(old_meta), str(new_meta))
        meta_moved = True

    moved.append(f"{rel(source)} -> {rel(dest)} ({reason}; meta moved: {meta_moved})")

def delete_path(path: Path, deleted, errors):
    try:
        if path.is_dir():
            shutil.rmtree(path)
            deleted.append(f"{rel(path)} [folder]")
        else:
            path.unlink()
            deleted.append(rel(path))
            meta = Path(str(path) + ".meta")
            if meta.exists():
                meta.unlink()
                deleted.append(rel(meta))
    except Exception as exc:
        errors.append(f"Failed to delete {rel(path)}: {exc}")

def audit_state():
    findings = defaultdict(list)
    for path in ASSETS.rglob("*"):
        if path.is_dir():
            if path.name == "__MACOSX":
                findings["mac_metadata"].append(rel(path))
            continue

        ext = path.suffix.lower()
        if path.name == ".DS_Store":
            findings["mac_metadata"].append(rel(path))
        if ext in STRAY_EXTS:
            findings["stray_files"].append(rel(path))

        if ext in IMAGE_EXTS:
            parent = path.parent
            if parent == ASSETS:
                findings["images_in_assets_root"].append(rel(path))
            if parent == ART:
                findings["images_in_art_root"].append(rel(path))

            dest, reason = classify_asset(path)
            if dest and should_be_moved(path, dest):
                if "sprite" in reason:
                    findings["character_sprites_outside_characters"].append(f"{rel(path)} -> {rel(dest)}")
                elif "tileset" in reason or "background" in reason:
                    findings["environment_images_outside_environment"].append(f"{rel(path)} -> {rel(dest)}")
                elif "icon" in reason:
                    findings["icons_outside_icons"].append(f"{rel(path)} -> {rel(dest)}")
                elif "UI" in reason or "HUD" in reason:
                    findings["ui_images_outside_ui_targets"].append(f"{rel(path)} -> {rel(dest)}")

        if ext in FONT_EXTS:
            dest, _ = classify_asset(path)
            if should_be_moved(path, dest):
                findings["fonts_outside_fonts"].append(f"{rel(path)} -> {rel(dest)}")

    extra_art_dirs = []
    if ART.exists():
        for directory in ART.rglob("*"):
            if directory.is_dir() and directory not in EXPECTED_ART_DIRS:
                extra_art_dirs.append(rel(directory))
    findings["extra_art_dirs"] = extra_art_dirs
    return findings

def count_files():
    counts = {}
    for label, directory in DESTS.items():
        if directory.exists():
            counts[label] = sum(1 for p in directory.iterdir() if p.is_file() and not p.name.endswith(".meta"))
        else:
            counts[label] = 0
    return counts

def print_findings(title, findings):
    print(f"\n=== {title} ===")
    for key in [
        "images_in_assets_root",
        "images_in_art_root",
        "character_sprites_outside_characters",
        "environment_images_outside_environment",
        "icons_outside_icons",
        "ui_images_outside_ui_targets",
        "fonts_outside_fonts",
        "stray_files",
        "mac_metadata",
        "extra_art_dirs",
    ]:
        items = findings.get(key, [])
        print(f"{key}: {len(items)}")
        for item in items[:100]:
            print(f"  {item}")
        if len(items) > 100:
            print(f"  ... {len(items) - 100} more")

def main():
    moved = []
    deleted = []
    collisions = []
    errors = []

    for directory in EXPECTED_ART_DIRS:
        directory.mkdir(parents=True, exist_ok=True)

    before = audit_state()
    print_findings("Initial Audit", before)

    # Delete explicit Unity-unwanted metadata/source files first.
    mac_dirs = [p for p in ASSETS.rglob("__MACOSX") if p.is_dir()]
    ds_store = [p for p in ASSETS.rglob(".DS_Store") if p.is_file()]
    stray_files = [p for p in ASSETS.rglob("*") if p.is_file() and p.suffix.lower() in STRAY_EXTS]

    for path in sorted(mac_dirs, key=lambda p: len(p.parts), reverse=True):
        delete_path(path, deleted, errors)
    for path in sorted(ds_store):
        if path.exists():
            delete_path(path, deleted, errors)
    for path in sorted(stray_files):
        if path.exists():
            delete_path(path, deleted, errors)

    # Move misplaced images/fonts into the requested Art structure.
    files = [p for p in ASSETS.rglob("*") if p.is_file() and p.suffix.lower() in IMAGE_EXTS.union(FONT_EXTS)]
    for path in sorted(files):
        if not path.exists():
            continue
        dest, reason = classify_asset(path)
        if should_be_moved(path, dest):
            try:
                move_with_meta(path, dest, reason, moved, collisions)
            except Exception as exc:
                errors.append(f"Failed to move {rel(path)}: {exc}")

    after = audit_state()

    print("\n=== Moved Files ===")
    if moved:
        for item in moved:
            print(item)
    else:
        print("None")

    print("\n=== Deleted Files/Folders ===")
    if deleted:
        for item in deleted:
            print(item)
    else:
        print("None")

    print("\n=== Collision Renames ===")
    if collisions:
        for item in collisions:
            print(item)
    else:
        print("None")

    print_findings("Final Audit", after)

    print("\n=== Final File Counts Per Art Folder ===")
    for label, count in count_files().items():
        print(f"Art/{label}: {count}")

    print("\n=== Errors ===")
    if errors:
        for item in errors:
            print(item)
    else:
        print("None")

if __name__ == "__main__":
    main()
