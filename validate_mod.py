#!/usr/bin/env python3
"""
Splicetails mod validator — run before building to catch common issues.
Usage:  python3 validate_mod.py
Exit 0 = all clear.  Exit 1 = errors found (do not build).
"""

import json, os, re, subprocess, sys, zlib
from pathlib import Path

PROJECT   = Path(__file__).parent
MOD_DATA  = PROJECT / "Assets/Mods/Splicetails/Data"
RESOURCES = PROJECT / "Assets/Mods/Splicetails/AssetBundles/Resources"
SCRIPTS   = PROJECT / "Assets/Mods/Splicetails/Scripts"

errors   = []
warnings = []

def err(msg):  errors.append(f"  ❌  {msg}")
def warn(msg): warnings.append(f"  ⚠️   {msg}")

# ── helpers ──────────────────────────────────────────────────────────────────

def load_json(path):
    try:
        with open(path) as f:
            return json.load(f)
    except json.JSONDecodeError as e:
        err(f"Invalid JSON in {path.name}: {e}")
        return None

def mat_name(path):
    return path.stem  # e.g. Lodge.Splicetails.mat → Lodge.Splicetails

def timbermesh_materials(tm_path):
    """Extract material name(s) stored in a .timbermesh (zlib+protobuf).
    Returns list of strings found after the material name length prefix."""
    try:
        raw = zlib.decompress(tm_path.read_bytes())
        # Material names are UTF-8 strings in the protobuf. Extract all
        # printable ASCII sequences of length > 3 that look like names.
        names = set()
        i = 0
        while i < len(raw):
            # Protobuf string field: tag byte, length varint, then bytes
            # We just scan for runs of printable chars as a heuristic.
            if 32 <= raw[i] < 127:
                j = i
                while j < len(raw) and 32 <= raw[j] < 127:
                    j += 1
                s = raw[i:j].decode('ascii', errors='ignore').strip()
                if len(s) > 3 and not s.startswith('//') and ' ' not in s:
                    names.add(s)
                i = j
            else:
                i += 1
        return names
    except Exception:
        return set()

# ── check 1: Timberborn not running ──────────────────────────────────────────
print("\nRunning Splicetails mod validation…\n")

# ── C# compile check (fast, runs before Unity build) ─────────────────────────
import subprocess as _sp
_csproj = PROJECT / "Timberborn.Splicetails.csproj"
if _csproj.exists():
    result = _sp.run(
        ["dotnet", "build", str(_csproj), "--nologo", "-v", "quiet"],
        capture_output=True, text=True
    )
    if result.returncode != 0:
        # Extract error lines from both stdout and stderr
        for line in (result.stdout + result.stderr).splitlines():
            if ": error " in line or "Build FAILED" in line:
                err(f"C# compile: {line.strip()}")
        if not errors:
            err(f"C# compile failed (exit {result.returncode}) — run: dotnet build Timberborn.Splicetails.csproj")
    else:
        print("  ✅  C# scripts compile without errors")
else:
    warn("Timberborn.Splicetails.csproj not found — skipping C# compile check")

# ── known harmless runtime messages (not caught by static analysis) ────────────
# These appear in the game log but indicate no mod error — safe to ignore:
#   "FMOD failed to switch back to normal output ... Cannot call this command
#    after System::init."  → macOS audio device change while game is running
#   "Can't Generate Mesh, No Font Asset has been assigned."
#    → Unity editor bug with missing font in editor UI, not a mod error

result = subprocess.run(["pgrep", "-x", "Timberborn"], capture_output=True)
if result.returncode == 0:
    err("Timberborn is running — close it before building (manifest.json lock → Win32 IO 997)")
else:
    print("  ✅  Timberborn not running")

# ── check 2: All blueprint JSON is valid ─────────────────────────────────────
bad_json = 0
all_blueprints = list(MOD_DATA.rglob("*.blueprint.json"))
for p in all_blueprints:
    if load_json(p) is None:
        bad_json += 1
if bad_json == 0:
    print(f"  ✅  All {len(all_blueprints)} blueprint JSON files are valid")

# ── check 3: TemplateCollection → files exist ────────────────────────────────
tc_path = MOD_DATA / "Specifications/TemplateCollections/TemplateCollection.Buildings.Splicetails.blueprint.json"
tc = load_json(tc_path)
if tc:
    custom_bps = [b for b in tc["TemplateCollectionSpec"]["Blueprints"] if "Splicetails" in b]
    missing = []
    for ref in custom_bps:
        if not (MOD_DATA / (ref + ".json")).exists():
            err(f"TemplateCollection references missing file: {ref}")
            missing.append(ref)
    if not missing:
        print(f"  ✅  All {len(custom_bps)} custom TemplateCollection files exist")

    # ── check 4: Lodge.Folktails MUST stay in TemplateCollection ─────────────────
    # BuildingTutorialStepDeserializer.Create calls BuildingService.GetBuildingTemplate
    # on every TemplateNames entry in every tutorial stage loaded at startup — including
    # the vanilla Folktails Housing tutorial which references Lodge.Folktails.
    # GetBuildingTemplate looks up by TemplateName only (not BackwardCompatibleTemplateNames).
    # If Lodge.Folktails is absent from TemplateCollection, the game crashes:
    #   ArgumentException: Building not found: Lodge.Folktails
    lodge_ref = "Buildings/Housing/Lodge/Lodge.Folktails.blueprint"
    if lodge_ref not in tc["TemplateCollectionSpec"]["Blueprints"]:
        err("Lodge.Folktails must remain in TemplateCollection — BuildingService.GetBuildingTemplate requires it to resolve the vanilla Housing tutorial step at startup")
    else:
        print("  ✅  Lodge.Folktails present in TemplateCollection (required for tutorial resolution)")

    # ── check 4b: Splicetails Housing tutorial stage must reference Lodge.Splicetails ──
    build_lodge_path = MOD_DATA / "Tutorials/Stages/Splicetails.Housing.BuildLodge.blueprint.json"
    if build_lodge_path.exists():
        build_lodge = load_json(build_lodge_path)
        if build_lodge:
            step1 = build_lodge.get("Children", {}).get("Step1", {})
            template_names = step1.get("BuildingTutorialStepSpec", {}).get("TemplateNames", [])
            if "Lodge.Splicetails" not in template_names:
                err("Splicetails.Housing.BuildLodge tutorial step must reference Lodge.Splicetails — found: " + str(template_names))
            elif "Lodge.Folktails" in template_names:
                err("Splicetails.Housing.BuildLodge still references Lodge.Folktails — update to Lodge.Splicetails only")
            else:
                print("  ✅  BuildLodge tutorial stage references Lodge.Splicetails correctly")
    else:
        err("Splicetails.Housing.BuildLodge.blueprint.json missing — required for Splicetails housing tutorial")

# ── check 5: MaterialCollection entries → .mat + bundle assignment ────────────
mc_path = MOD_DATA / "Specifications/MaterialCollections/MaterialCollection.Splicetails.blueprint.json"
mc = load_json(mc_path)
if mc:
    custom_mats = [m for m in mc["MaterialCollectionSpec"]["Materials"] if "/" not in m]
    mat_ok = True
    for name in custom_mats:
        mat_file  = RESOURCES / f"{name}.mat"
        meta_file = RESOURCES / f"{name}.mat.meta"
        if not mat_file.exists():
            err(f"MaterialCollection has '{name}' but {name}.mat not found in AssetBundles/Resources")
            mat_ok = False
        elif not meta_file.exists():
            err(f"{name}.mat.meta missing — Unity won't import it")
            mat_ok = False
        elif "assetBundleName: splicetails_mac" not in meta_file.read_text():
            err(f"{name}.mat.meta missing 'assetBundleName: splicetails_mac'")
            mat_ok = False
    if mat_ok:
        print(f"  ✅  All {len(custom_mats)} custom materials have .mat files and bundle assignment")

# ── check 6: Block count = Size.X × Size.Y × Size.Z ─────────────────────────
block_ok = True
for p in (MOD_DATA / "Buildings").rglob("*.blueprint.json"):
    bp = load_json(p)
    if not bp or "BlockObjectSpec" not in bp:
        continue
    spec = bp["BlockObjectSpec"]
    size = spec["Size"]
    expected = size["X"] * size["Y"] * size["Z"]
    actual   = len(spec.get("Blocks", []))
    if expected != actual:
        err(f"{p.name}: Size {size} requires {expected} blocks but has {actual}")
        block_ok = False
if block_ok:
    print("  ✅  All building block counts match their Size specs")

# ── check 7: DrivewayModelSpec direction/entrance consistency ─────────────────
driveway_ok = True
VALID_PAIRS = {
    "Down":  lambda y: y < 0,
    "Up":    lambda y: y > 0,
    "Left":  lambda y: True,   # X-axis — Y doesn't constrain
    "Right": lambda y: True,
}
for p in (MOD_DATA / "Buildings").rglob("*.blueprint.json"):
    bp = load_json(p)
    if not bp:
        continue
    driveway = bp.get("DrivewayModelSpec", {})
    entrance = bp.get("BlockObjectSpec", {}).get("Entrance", {}).get("Coordinates", {})
    direction = driveway.get("CustomDirection")
    ent_y = entrance.get("Y", 0)
    if direction == "Down" and ent_y >= 0:
        err(f"{p.name}: CustomDirection 'Down' with Entrance Y:{ent_y} will invert the arrow (needs Y < 0)")
        driveway_ok = False
    elif direction == "Up" and ent_y <= 0:
        err(f"{p.name}: CustomDirection 'Up' with Entrance Y:{ent_y} will invert the arrow (needs Y > 0)")
        driveway_ok = False
if driveway_ok:
    print("  ✅  All DrivewayModelSpec direction/entrance pairs are consistent")

# ── check 8: Custom timbermesh files exist for Splicetails-specific models ────
tm_ok = True
for p in (MOD_DATA / "Buildings").rglob("*.blueprint.json"):
    bp = load_json(p)
    if not bp:
        continue
    for child in bp.get("Children", {}).values():
        model = child.get("TimbermeshSpec", {}).get("Model", "")
        # Only check models inside our mod directory (custom ones).
        # Vanilla models (Folktails/IronTeeth) live in the game binary — skip.
        if model and ("Splicetails" in model):
            tm_path = MOD_DATA / (model + ".timbermesh")
            if not tm_path.exists():
                err(f"{p.name}: custom timbermesh '{model}' not found in mod Data/")
                tm_ok = False
if tm_ok:
    print("  ✅  All custom timbermesh model files exist")

# ── check 9: Custom timbermesh files don't use Meshy default material names ───
tm_mat_ok = True
for tm_path in MOD_DATA.rglob("*.timbermesh"):
    found = timbermesh_materials(tm_path)
    meshy_defaults = {s for s in found if re.match(r'^Material_\d', s)}
    if meshy_defaults:
        err(f"{tm_path.name}: Meshy default material name(s) {meshy_defaults} — rename in Blender before export")
        tm_mat_ok = False
if tm_mat_ok:
    print("  ✅  No Meshy default material names in timbermesh files")

# ── check 10: Localization file has no empty lines ────────────────────────────
loc_path = MOD_DATA / "Localizations/enUS.csv"
defined_keys = set()
if loc_path.exists():
    csv_lines = loc_path.read_text().splitlines()
    # First line must be the header that matches LocalizationRecord field names
    if csv_lines and csv_lines[0].strip() != "ID,Text,Comment":
        err(f"enUS.csv first line must be 'ID,Text,Comment' (got {csv_lines[0]!r}) — Timberborn CSV parser reads row 1 as column headers")
    empty_lines = [i+1 for i, l in enumerate(csv_lines) if not l.strip()]
    if empty_lines:
        err(f"enUS.csv has empty line(s) at: {empty_lines} — Timberborn will reject the file")
    # Extra trailing commas (,,) crash the validator
    extra_comma_lines = [i+1 for i, l in enumerate(csv_lines) if l.rstrip().endswith(',,')]
    if extra_comma_lines:
        err(f"enUS.csv has extra trailing comma(s) at lines: {extra_comma_lines} — use exactly one trailing comma per line")
    for line in csv_lines:
        if "," in line:
            defined_keys.add(line.split(",")[0].strip())

loc_ok = True
for p in (MOD_DATA / "Buildings").rglob("*.blueprint.json"):
    # Only check blueprints for Splicetails-specific buildings; vanilla LocKeys
    # (Building.Lodge.*, Building.MiniLodge.*, etc.) live in the game binary.
    if "Splicetails" not in p.stem:
        continue
    content = p.read_text()
    for key in re.findall(r'LocKey":\s*"([^"]+)"', content):
        if key and key not in defined_keys:
            err(f"Missing localization key '{key}' (referenced in {p.name})")
            loc_ok = False
if loc_ok:
    print(f"  ✅  All Splicetails building localization keys defined ({len(defined_keys)} keys in enUS.csv)")

# ── check 11: Construction base dimensions match building footprint ───────────
for p in (MOD_DATA / "Buildings").rglob("*.blueprint.json"):
    bp = load_json(p)
    if not bp:
        continue
    size = bp.get("BlockObjectSpec", {}).get("Size", {})
    bx, by = size.get("X", 0), size.get("Y", 0)
    unfinished = bp.get("Children", {}).get("#Unfinished", {}).get("Children", {})
    for key in unfinished:
        m = re.search(r'ConstructionBase(\d+)x(\d+)', key)
        if m:
            cx, cy = int(m.group(1)), int(m.group(2))
            # Base dimensions can be rotated (NxM or MxN) — accept either order
            if not ({cx, cy} == {bx, by} or {cx, cy} == {by, bx}):
                err(f"{p.name}: ConstructionBase{cx}x{cy} but building Size is {bx}x{by} — scaffolding will look wrong")

print("  ✅  All construction base dimensions match building footprints")

# ── check 13: Surface block settings that cause adjacent-build artifacts ────────
for p in (MOD_DATA / "Buildings").rglob("*.blueprint.json"):
    bp = load_json(p)
    if not bp or "BlockObjectSpec" not in bp:
        continue
    for b in bp["BlockObjectSpec"].get("Blocks", []):
        if not b.get("Underground"):
            if b.get("OccupyAllBelow"):
                warn(f"{p.name}: surface block has OccupyAllBelow:true — adjacent buildings will float")
                break
            # Stackable:BlockObject alone is fine; the floating issue requires OccupyAllBelow too

# ── check 14: C# — known bad API patterns that compile-fail ─────────────────
for cs_file in SCRIPTS.rglob("*.cs"):
    src = cs_file.read_text()
    # Every AddDecorator<TSpec, TComponent> for a MOD-OWNED type must have Bind<TComponent>
    # Vanilla types (from Timberborn.* DLLs) are pre-bound by the game's own configurators.
    # Without Bind<> for our own types, Bindito throws "No binding exists for type X".
    decorator_types = re.findall(r'AddDecorator<[^,]+,\s*(\w+)>', src)
    bind_types      = re.findall(r'Bind<(\w+)>\s*\(\)', src)
    # Collect all type names defined in our Scripts folder
    mod_types = set()
    for cs in SCRIPTS.rglob('*.cs'):
        for m in re.findall(r'class\s+(\w+)', cs.read_text()):
            mod_types.add(m)
    for dtype in decorator_types:
        if dtype in mod_types and dtype not in bind_types:
            err(f"{cs_file.name}: AddDecorator uses mod type '{dtype}' but no Bind<{dtype}>() — Bindito will crash at load")
    # Custom blueprint specs must be records extending ComponentSpec, not BaseComponent
    # If a class is used as a blueprint spec key (matches a JSON key in any blueprint),
    # it MUST be: public record MySpec : ComponentSpec — not BaseComponent
    if re.search(r'class\s+\w+Spec\s*:\s*BaseComponent', src):
        err(f"{cs_file.name}: *Spec class extends BaseComponent — must be 'record MySpec : ComponentSpec' for blueprint deserialization")
    # ReadOnlyHashSet<T>() default constructor leaves internal set null → NullReferenceException
    # Must use: new HashSet<T>().AsReadOnlyHashSet()
    if re.search(r'new\s+ReadOnlyHashSet<[^>]+>\s*\(\s*\)', src):
        err(f"{cs_file.name}: 'new ReadOnlyHashSet<T>()' leaves internal set null — use 'new HashSet<T>().AsReadOnlyHashSet()' instead")
    # IPreviewValidator only changes preview color (yellow warning) — does NOT block placement.
    # Use IBlockObjectValidator (MultiBind) to actually prevent placement.
    if 'IPreviewValidator' in src and 'IBlockObjectValidator' not in src:
        err(f"{cs_file.name}: implements IPreviewValidator but NOT IBlockObjectValidator — IPreviewValidator only shows warning color, it does NOT block placement; use IBlockObjectValidator + MultiBind to block")
    # ITerrainService.RemoveTerrain doesn't exist — use TerrainDestroyer.DestroyTerrain
    if "ITerrainService" in src and "RemoveTerrain" in src:
        err(f"{cs_file.name}: ITerrainService has no RemoveTerrain — use TerrainDestroyer.DestroyTerrain instead")
    # ITerrainPhysicsService.DestroyTerrain doesn't exist — DestroyTerrain is on TerrainDestroyer (concrete class)
    if "ITerrainPhysicsService" in src and "DestroyTerrain" in src:
        err(f"{cs_file.name}: ITerrainPhysicsService has no DestroyTerrain — inject TerrainDestroyer directly")
    # TerrainService.AddTerrain doesn't exist on the concrete class (it's buried on ColumnTerrainMap)
    if "TerrainService" in src and ".AddTerrain" in src:
        err(f"{cs_file.name}: TerrainService has no AddTerrain — use 1x1 footprint + natural terrain instead")
    # PositionedBlocks is not directly iterable — must call .GetAllCoordinates()
    if "PositionedBlocks" in src and re.search(r'foreach\s*\([^)]+\s+in\s+\w+\.PositionedBlocks\s*\)', src):
        err(f"{cs_file.name}: PositionedBlocks is not iterable directly — use .GetAllCoordinates()")

# ── check 16: C# — SerumDeliveryBehavior must be on WorkerSpec not buildings ──
configurator = SCRIPTS / "SplicetailsConfigurator.cs"
if configurator.exists():
    src = configurator.read_text()
    # Detect if SerumDeliveryBehavior is accidentally decorated on a building spec
    bad_pattern = re.search(r'AddDecorator<(\w+Spec),\s*SerumDeliveryBehavior>', src)
    if bad_pattern and bad_pattern.group(1) != "WorkerSpec":
        err(f"SerumDeliveryBehavior decorated on {bad_pattern.group(1)} — must be WorkerSpec (beavers), not buildings")
    else:
        print("  ✅  SerumDeliveryBehavior correctly registered on WorkerSpec")

# ── check 12: MutatableReacher approach radius ≤ 0.5 ─────────────────────────
reacher = SCRIPTS / "Resources/MutatableReacher.cs"
if reacher.exists():
    src = reacher.read_text()
    m = re.search(r'ApproachRadius\s*=\s*([\d.]+)', src)
    if m:
        radius = float(m.group(1))
        if radius > 0.5:
            err(f"MutatableReacher.ApproachRadius = {radius} — must be ≤ 0.5 (PositionDestination hard limit)")
        else:
            print(f"  ✅  MutatableReacher.ApproachRadius = {radius} (≤ 0.5)")

print()

# ── summary ───────────────────────────────────────────────────────────────────
print()
if warnings:
    for w in warnings:
        print(w)
if errors:
    print("─" * 55)
    for e in errors:
        print(e)
    print(f"\n  {len(errors)} error(s) — fix before building.\n")
    sys.exit(1)
else:
    print("  All checks passed — safe to build.\n")
    sys.exit(0)
