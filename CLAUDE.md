# Splicetails Mod — Lessons Learned

## User Preferences

**Always perform Blender and Unity file operations directly** — the user is not experienced with Blender or Unity. Instead of instructing them to drag-and-drop, assign materials, set asset bundle dropdowns, or configure properties manually, do it by editing the relevant `.mat`, `.meta`, or `.blend` files directly (or via CLI tools). Only ask the user to interact with the UI when there is no programmatic alternative.

## Rule #1: Always Check Examples First
Before implementing ANY mod structure, asset paths, blueprints, or C# patterns — check `timberborn-modding-main` (in ~/Downloads) and the vanilla game DLLs first. Do not guess or assume. Past mistakes from guessing:
- Asset bundle path structure (missed the `Resources/` subdirectory requirement)
- Shader fileID for materials (`4800000` was wrong; correct is `-6465566751694194690` for ShaderGraph)
- Texture property names in BeaverURP shader
- `SimpleInputInventorySpec` doesn't exist
- `StartDrawing()`/`StopDrawing()` don't exist on `MeasurableAreaDrawer`

---

## Asset Pipeline

### Textures for Beaver Skins
- `FactionSpec.Textures` references **Texture2D** assets, NOT Material assets
- Place PNG/JPG files in `Data/Materials/Beavers/[Faction]/Adult/` and `Child/`
- File name determines the asset path (e.g. `BeaverAdult1.Splicetails.png` → loaded as `Materials/Beavers/Splicetails/Adult/BeaverAdult1.Splicetails`)
- `BeaverTextureSetter` loads these as `Texture2D` and sets `_BaseMap` shader property
- Vanilla textures are 2048×2048 for adults, 1024×1024 for children

### Asset Bundles
- Assets the game loads via `AssetLoader` must be in `AssetBundles/Resources/` (the `Resources/` subdirectory is required)
- `AssetPathHelper.NormalizeAssetPath` strips everything up to and including `/Resources/`, strips extension, lowercases → this becomes the lookup key
- Materials (Unity `.mat` files) **must** go in asset bundles (not Data/)
- Textures used **by materials** can live in `AssetBundles/Resources/` alongside the material

### Materials in Asset Bundles
- ShaderGraph shader fileID in `.mat` files: `-6465566751694194690` (not `4800000`)
- BeaverURP shader GUID: `f39ae455f01c34d4b9c2a3ca9e2f66ec`
- VegetationURP shader GUID: `d2386f32c82b00c4c8c9e0b8a205ebfd`
- BeaverURP albedo texture property: `Texture2D_25ddac2c4fcb4d64997b5fa5b950eba7`
- VegetationURP albedo texture property: `Texture2D_138C554F`
- After writing `.mat` files programmatically, open Unity and verify the shader isn't magenta; if it is, manually reassign the shader in the Inspector

### MaterialRepository (Timbermesh Materials)
- `MaterialRepository` does NOT automatically load all materials from asset bundles
- Materials must be listed in a `MaterialCollectionSpec` blueprint (e.g. `MaterialCollection.Splicetails.blueprint.json`)
- Use just the material name (e.g. `"SplicedTree"`) as the path entry — the asset loader normalizes it to find the bundle asset
- Missing from this collection = `Material X not found in repository` crash when hovering the planting tool

### Timbermesh
- Trees/buildings use `.timbermesh` format (protobuf + zlib)
- Material names in the timbermesh come directly from Blender material slot names
- Vanilla Pine tree uses material named `Pine` (not `Pine_D`)
- Use `Timberborn.Rendering.AreaTileDrawerFactory.Create(Color, GameObject)` for persistent colored tile overlays
- Vanilla tree examples are in `TimberbornExampleModels.blend` (in StreamingAssets/Modding)
- Headless Blender export works: `blender --background file.blend --python script.py`
- The Timbermesh Blender plugin is at `~/.dotnet/tools` after install

### Building Blueprints
- `ConstructionSiteProgressVisualizerSpec.ProgressThresholds` must have exactly (N construction stages) entries — if you remove all construction stages, set this to `[]`
- When replacing a vanilla building, add the original template name to `BackwardCompatibleTemplateNames` for save-game compatibility only — this does NOT help with tutorial resolution
- **`BackwardCompatibleTemplateNames` does NOT affect `BuildingService.GetBuildingTemplate`.** That method looks up by `TemplateName` only. `BackwardCompatibleTemplateNames` is only for loading saved games where a building was persisted under its old name.
- **`Lodge.Folktails` MUST stay in the Splicetails TemplateCollection.** `BuildingTutorialStepDeserializer.Create` calls `BuildingService.GetBuildingTemplate(name)` on every `TemplateNames` entry in every tutorial stage at startup — including the vanilla Folktails Housing tutorial. If `Lodge.Folktails` isn't in the TemplateCollection, the game crashes with `ArgumentException: Building not found: Lodge.Folktails`. There is no workaround short of overriding the vanilla tutorial stage.
- `SimpleInputInventorySpec` does NOT exist — use `ManufactorySpec` for input inventories
- **Custom building icons (`LabeledEntitySpec.Icon`) require three files** (see ShantySpeaker example in timberborn-modding-main):
  1. `BuildingNameIcon.png` — the PNG, placed in `Data/Buildings/.../`
  2. `BuildingNameIcon.png.meta` — Unity meta file (textureType: 8, spriteMode: 1, alphaIsTransparency: 1)
  3. `BuildingNameIcon.png.meta.json` — Timberborn sprite registration: `{ "isSprite": true }`
  4. `BuildingNameIcon.png.meta.json.meta` — Unity meta for the above JSON
  - Icon path in blueprint: `Buildings/.../BuildingNameIcon` (no extension, no faction suffix in filename)
  - **Never put `.FactionName` in the icon filename.** `DecalService.GetValidatedDecal` splits the filename on `.` and CamelCase-splits the last segment — `UndergroundLodgeIcon.Splicetails` → key `Tails` → `KeyNotFoundException: The given key 'Tails'`

---

## C# / Timberborn Architecture

### BaseComponent vs MonoBehaviour
- `BaseComponent` extends `UnityEngine.Object`, NOT `MonoBehaviour`
- No `transform` property — use `GameObject.transform`
- No `GetComponentsInChildren<T>()` — use `GameObject.GetComponentsInChildren<T>()`
- No bare `Physics.OverlapSphere` — use `UnityEngine.Physics.OverlapSphere`
- DO have `GameObject`, `GetComponent<T>()`, `Name`

### WorkplaceBehavior Pattern
- `WorkplaceBehavior.Decide(BehaviorAgent agent)` returns a `Decision`
- `Decision.TransferNow(behavior, in decision)` — requires TWO args; must call `other.Decide(agent)` first to get the second arg
- `Decision.ReleaseNow()` — releases the worker
- `Decision.ReturnWhenFinished(executor)` — waits for executor to finish

### WalkToReservableExecutor
- `Launch(ReservableReacher)` starts navigation; returns `ExecutorStatus`
- `SetTarget(reacher)` sets `_reservable = reacher.GetComponent<Reservable>()` — reacher and Reservable must be on the SAME GameObject
- `Tick()` calls `reacher.NotifyReservableReached(agent)` when walker stops

### TemplateModule Decorators
- `builder.AddDecorator<SpecType, ComponentType>()` adds a component to all entities with that spec
- Components added this way receive DI constructor injection
- `IAwakableComponent.Awake()` is called after construction for Unity-component setup
- **REQUIRED: every mod-owned type used in `AddDecorator` must ALSO have `Bind<T>().AsTransient()` in `Configure()`**
  - `AddDecorator` = WHEN to attach; `Bind<>` = HOW to create. Missing `Bind<>` → `BinditoException: No binding exists for type T` at load
  - Vanilla Timberborn types (e.g. `LaborWorkplaceBehavior`) are pre-bound by the game — no `Bind<>` needed for those

### IInitializableEntity
- Called after the entity is fully constructed and placed
- Use for initialization that needs the full component graph (e.g. getting world position for `PositionDestination`)

### Blueprint Spec Pattern
Custom specs used as blueprint JSON keys MUST be:
```csharp
using Timberborn.BlueprintSystem;
public record MySpec : ComponentSpec;   // NOT BaseComponent
```
If a class extends `BaseComponent` and is used as a blueprint key, the game throws `No type found for key MySpec` at load. Only `ComponentSpec` subclasses are registered with the blueprint deserializer.
The matching runtime component (decorator target) extends `BaseComponent`, not the spec itself.

### Terrain API (verified from DLL strings)
- `TerrainDestroyer.DestroyTerrain(Vector3Int)` — removes a terrain block. Inject the **concrete class** `TerrainDestroyer`, not the interface. Namespace: `Timberborn.TerrainPhysics`
- `ITerrainService` does NOT have `RemoveTerrain`
- `ITerrainPhysicsService` does NOT have `DestroyTerrain` — `DestroyTerrain` is only on the concrete `TerrainDestroyer` class
- `AddTerrain` does NOT exist on `TerrainService` (concrete) or `ITerrainService` — it is buried on the internal `ColumnTerrainMap` API and not usable from mods. To allow crops on a building footprint, use a **1×1 building** so surrounding tiles stay as natural terrain.
- `BlockObject.PositionedBlocks.GetAllCoordinates()` returns `IEnumerable<Vector3Int>` — iterate with this, not PositionedBlocks directly
- `ITerrainService` (Timberborn.TerrainSystem) — read-only terrain queries (height, height below, etc.)

### IPreviewValidator vs IBlockObjectValidator (verified from DLL decompilation)
These two interfaces look similar but do DIFFERENT things:

| Interface | Effect | Registration |
|---|---|---|
| `IPreviewValidator` | Changes preview **color** (yellow warning) — does NOT block placement | Component decorator: `AddDecorator<Spec, Validator>()` + `Bind<Validator>().AsTransient()` |
| `IBlockObjectValidator` | Actually **blocks placement** (red preview + prevents confirm) | Global service: `MultiBind<IBlockObjectValidator>().To<Validator>().AsSingleton()` |

Use `IBlockObjectValidator` whenever placement must be prevented. `IPreviewValidator` is only for UI warnings that still allow placement.

`IBlockObjectValidator` signature:
```csharp
using Timberborn.BlockSystem;
using UnityEngine;

public class MyValidator : IBlockObjectValidator {
    private readonly IBlockService _blockService;
    public MyValidator(IBlockService blockService) { _blockService = blockService; }

    public bool IsValid(BlockObject blockObject, out string errorMessage) {
        if (blockObject.GetComponent<MySpec>() == null) { errorMessage = null; return true; }
        // Check spatial constraints via IBlockService.GetFirstObjectWithComponentAt<T>(coord)
        // Skip dx==0,dy==0 to avoid self-detection when preview is in block service
        errorMessage = "reason";
        return false; // blocks placement
    }
}
```
Register: `MultiBind<IBlockObjectValidator>().To<MyValidator>().AsSingleton()` — no `Bind<>` or `AddDecorator` needed.

**`ReadOnlyHashSet<T>` gotcha (IPreviewValidator only):** `new ReadOnlyHashSet<T>()` compiles but leaves the internal set null → `NullReferenceException` in `GetEnumerator()` at runtime. Always use: `new HashSet<T>().AsReadOnlyHashSet()`

**`IBlockService.GetFirstObjectWithComponentAt<T>(Vector3Int)`** — returns the first component of type T on any `BlockObject` at that grid coordinate (placed buildings only, not preview objects unless temporarily added). Use this to check for nearby conflicting buildings without maintaining a registry.

### Building completion vs placement
- `IInitializableEntity.InitializeEntity()` — fires at entity **placement** (before beavers build). Use for one-time setup that should happen immediately.
- `IFinishedStateListener.OnEnterFinishedState()` — fires when **beavers complete construction**. Use for effects that should only happen after all inputs are delivered. Namespace: `Timberborn.BlockSystem`
- Underground excavation: building completion removes terrain at `coord.z` (first layer). To excavate one layer deeper use `coord.z - 2`, NOT `coord.z - 1` (which creates a 2-layer pit).

### District connectivity
- Buildings without `TransputProviderSpec` show "not connected to district" warning icon
- Add `TransputProviderSpec` covering all footprint tiles with appropriate Directions per tile (edges get cardinal directions, interior gets Bottom/Top only)
- Also add `MechanicalConnectorTargetSpec: {}` alongside it

### Key Namespaces (frequently needed)
| Type | Namespace |
|------|-----------|
| `BaseComponent`, `IAwakableComponent` | `Timberborn.BaseComponentSystem` |
| `IInitializableEntity` | `Timberborn.EntitySystem` |
| `WorkplaceBehavior`, `Decision`, `BehaviorAgent` | `Timberborn.WorkSystem` |
| `LaborWorkplaceBehavior`, `WaitInsideIdlyWorkplaceBehavior` | `Timberborn.LaborSystem` / `Timberborn.WorkSystem` |
| `ReservableReacher`, `WalkToReservableExecutor`, `Reservable` | `Timberborn.ReservableSystem` |
| `IDestination`, `PositionDestination`, `PositionDestinationFactory` | `Timberborn.WalkingSystem` |
| `NaturalResourceCenterProvider` | `Timberborn.NaturalResourcesModelSystem` |
| `LivingNaturalResource` | `Timberborn.NaturalResourcesLifecycle` |
| `Growable` | `Timberborn.Growing` |
| `TreeComponent`, `TreeComponentSpec` | `Timberborn.Forestry` |
| `ISaveableSingleton`, `ILoadableSingleton`, `EventBus` | `Timberborn.SingletonSystem` |
| `ISingletonLoader`, `ISingletonSaver` | `Timberborn.WorldPersistence` |
| `SingletonKey`, `ListKey<T>` | `Timberborn.Persistence` |
| `IBlockService`, `BlockObject` | `Timberborn.BlockSystem` |
| `TerrainAreaService` | `Timberborn.TerrainQueryingSystem` |
| `AreaHighlightingService` | `Timberborn.SelectionSystem` |
| `AreaTileDrawerFactory`, `AreaTileDrawer` | `Timberborn.Rendering` |
| `SelectionToolProcessorFactory`, `SelectionToolProcessor` | `Timberborn.SelectionToolSystem` |
| `MeasurableAreaDrawer` | `Timberborn.AreaSelectionSystemUI` |
| `ILoc` | `Timberborn.Localization` |
| `ITool`, `IToolDescriptor`, `ToolDescription`, `ToolGroupService` | `Timberborn.ToolSystem` |
| `ToolButtonFactory`, `ToolGroupButtonFactory`, `ToolGroupButton` | `Timberborn.ToolButtonSystem` |
| `IBottomBarElementsProvider`, `BottomBarElement`, `BottomBarModule` | `Timberborn.BottomBarSystem` |
| `IToolDescriptor`, `ToolDescription` | `Timberborn.ToolSystemUI` |
| `ToolGroupEnteredEvent`, `ToolGroupExitedEvent` | `Timberborn.ToolSystem` |
| `RootObjectProvider` | `Timberborn.RootProviders` |
| `MapEditorMode` | `Timberborn.MapStateSystem` |

### ToolGroupEnteredEvent / ToolGroupExitedEvent
- `e.ToolGroup` CAN be null — always null-check before accessing `.Id`

### MeasurableAreaDrawer
- Only has `AddMeasurableCoordinates(Vector3Int)` — no `StartDrawing()`/`StopDrawing()`
- Auto-clears each frame via `LateUpdateSingleton()`

### MaterialRepository (C# side)
- `GetMaterials()` iterates `MaterialCollectionSpec` blueprints via `ISpecService`
- Uses `((Object)material).name` (the Unity object name) as the dictionary key
- Does NOT scan asset bundles directly

---

## Game Mechanics

### Faction Textures
- Beaver textures are loaded as `Texture2D`, not `Material`
- The game picks a random texture from `FactionSpec.Textures` and sets it as `_BaseMap` on the beaver shader
- All 5 adult variants (1-5) and 3 child variants (1-3) must exist

### Production Circular Flows
- If a building both produces AND consumes the same good, haulers will immediately return the produced goods back to the building → net zero output
- Solution: separate the production and consumption into different buildings

### Vertex Animations
- Timberborn uses baked vertex animations (not skeleton rigs) for characters
- No standard skeleton/rig is accessible for retargeting
- Faction visual identity should use texture overrides, not mesh replacement

### Area Marking Tools
- **`AreaTileDrawer` has THREE methods** (verified from `TreeCuttingAreaVisualizer` source):
  - `UpdateArea(coords)` — sets WHICH tiles to draw (does NOT make them visible by itself)
  - `ShowAllTiles()` — makes the tiles visible (must call after `UpdateArea`)
  - `HideAllTiles()` — hides all tiles
  - Correct pattern: `UpdateArea(coords)` then `ShowAllTiles()`. Calling only `UpdateArea` without `ShowAllTiles` shows nothing.
- Preview tiles during selection use `AreaHighlightingService.DrawTile(coord, color)` — cleared when tool exits
- Wire show/hide to `ToolGroupEnteredEvent`/`ToolGroupExitedEvent` with the tool group ID

### Cursor Keys
- Valid: `"CutTreeCursor"`, `"CancelCursor"` — do NOT use `"CutTreeCursorSmall"` (doesn't exist)

---

## Workflow

### Known harmless runtime messages (not mod errors)
| Message | Cause | Action |
|---|---|---|
| `FMOD failed to switch back to normal output … Cannot call this command after System::init.` | macOS audio device changed (headphones, etc.) while game runs | Ignore |
| `Can't Generate Mesh, No Font Asset has been assigned.` | Unity editor bug with missing editor font | Ignore |
| `IOException: Win32 IO returned 997` | Timberborn running during mod build (locks manifest.json) | Close game, rebuild |

### Error report protocol — every reported error must produce a new test
When a runtime or build error is reported (paste of a crash/exception), the fix is not complete until:
1. **Root cause is identified** and documented in the relevant section of this file.
2. **A new check is added to `validate_mod.py`** that would have caught the error before the build. If the error is only detectable at runtime (not statically), note that in a comment and add the closest static approximation that catches the same category of bug.

Pattern for adding a check:
```python
# In validate_mod.py, inside the appropriate check block:
if <pattern that indicates the bug>:
    err(f"<filename>: <clear description of what's wrong and how to fix it>")
```

Examples of errors that became checks:
- `BinditoException: No binding exists for type X` → check AddDecorator types have matching Bind<T>
- `NullReferenceException in ReadOnlyHashSet.GetEnumerator` → check for `new ReadOnlyHashSet<T>()` empty constructor
- `InvalidDataException: Empty line / Unnecessary comma` → check CSV format
- `ArgumentException: Material X not found in repository` → check MaterialCollection entries

### Pre-build validation
Run `python3 validate_mod.py` from the project root before every build. Catches:
JSON errors, missing TemplateCollection files, Lodge.Folktails absence, missing material bundle assignments,
block count mismatches, direction/entrance mismatches, missing timbermesh files, Meshy default material names,
empty CSV lines, OccupyAllBelow on surface blocks, SerumDeliveryBehavior on wrong spec, MutatableReacher radius,
C# compile errors, missing Bind<T> for AddDecorator types, ReadOnlyHashSet empty constructor.

### When Adding a New Building
1. Check vanilla blueprint for the right spec structure
2. Create blueprint in `Data/Buildings/[Category]/[Name]/`
3. Add timbermesh to same folder under `Mesh/`
4. Add template to `TemplateCollection.Buildings.Splicetails.blueprint.json`
5. Add material to `MaterialCollection.Splicetails.blueprint.json` AND create `.mat` + `.meta` with `assetBundleName: splicetails_mac`
6. Add backward-compatible template names if replacing a vanilla building
7. Add localization strings to `Data/Localizations/enUS.csv`:
   - First line MUST be `ID,Text,Comment` (LocalizationRecord header)
   - Each data line: `key,value,` (exactly ONE trailing comma)
   - Values with commas MUST be quoted: `key,"value, with comma",`
   - No empty lines. Never edit with naive comma-splitting — use csv module.
8. Run `python3 validate_mod.py` — fix all errors before building
9. Rebuild mod in Unity

### When Adding Custom Models (Meshy.ai Pipeline)

#### Meshy.ai export
- Download the `.blend` file (not FBX) — textures are PACKED INSIDE the `.blend`, not in the separate `textures/` folder
- The `textures/` folder in Downloads belongs to the BEAVER Meshy export, not buildings — do not reuse it
- Extract embedded textures headlessly: `blender --background file.blend --python-expr "import bpy,os; os.makedirs('/tmp/tex',exist_ok=True); [i.save(filepath=f'/tmp/tex/{i.name}.png') for i in bpy.data.images if i.size[0]>0]"`
- Texture names: `Baked_BaseColor` = albedo, `normal` = normal map, `Baked_MetallicRoughness` = ORM, `Baked_Emit` = emissive

#### Headless export script
Use `/tmp/fix_normals_export.py` as the canonical export script. It:
1. Translates vertices by (-1,-1,0) to shift the mesh center to Unity (1,0,1) — required for a 2x2 building footprint
2. Recalculates normals outward via bmesh (fixes Meshy inverted-normal artifacts)
3. Renames Blender material to the target Unity material name (e.g. `Lodge.Splicetails`)
4. Creates the required collection and exports via the Timbermesh plugin
Run: `blender --background file.blend --python /tmp/fix_normals_export.py -- /path/to/Mesh/`

#### Coordinate system and mesh orientation (VERIFIED on Lodge.Splicetails)
- Apply **180° Z rotation** to Meshy models before export so door faces Timberborn "Down" (south)
- The export script does this with: `v.co.x = -v.co.x - 1.0; v.co.y = -v.co.y - 1.0` (rotation + 2x2 translation combined)
- For NxM buildings: the combined line is `v.co.x = -v.co.x - (N/2); v.co.y = -v.co.y - (M/2)`
- Do NOT try to determine door direction from Blender viewport — verify in-game and adjust rotation

#### Blueprint entrance (VERIFIED working for Lodge.Splicetails)
- `CustomDirection: "Down"`, `Entrance: {X:1, Y:-1}` — arrow on south face pointing toward building ✓
- Direction and Entrance MUST match the same face. Mismatching (e.g. "Down" + Y:2) inverts the arrow.
- For lateral fine-tuning use `HasCustomCoordinates: true` + `CustomCoordinates: {X:0, Y:-1, Z:0}` to shift arrow left by one tile
- Fractional CustomCoordinates (e.g. X:0.5) may work for sub-tile adjustments
- Confirmed working DrivewayModelSpec for Lodge.Splicetails:
  ```json
  "Driveway": "NarrowLeft",
  "HasCustomCoordinates": true,
  "CustomCoordinates": {"X": 0, "Y": -1, "Z": 0},
  "CustomDirection": "Down",
  "DrivewayMode": "Unidirectional"
  ```

#### Building block settings
- `Stackable: "None"` + `Occupations: "Bottom, Corners, Path, Middle"` (no "Top") = no buildings on top, plants can grow
- `Stackable: "BlockObject"` + `Occupations: "Bottom, Corners, Path, Middle"` (no "Top") = buildings AND plants allowed on top ✓
- `Stackable: "None"` + no "Top" = plants CANNOT grow (plants need `MatterBelow: "GroundOrStackable"` satisfied; "None" fails that check)
- **The floating-adjacent-building issue is caused by `OccupyAllBelow: true`, NOT by `Stackable: "BlockObject"`** — do not confuse these
- `Stackable: "None"` + `Occupations: "Bottom, Top, Corners, Path, Middle"` (has "Top") = nothing on top (door tile)
- **`OccupyAllBelow: true` on surface blocks (Underground: false) causes adjacent buildings to float in air** — only use OccupyAllBelow on underground (Underground: true) blocks
- enUS.csv must have NO empty lines — Timberborn's LocalizationCsvValidator hard-fails on any empty line

#### Material setup (one-time per custom model)
1. Copy albedo PNG and normal PNG to `Assets/Mods/Splicetails/AssetBundles/Resources/`
2. Create `.meta` files with unique GUIDs (use `python3 -c "import uuid; print(uuid.uuid4().hex)"`)
3. Normal map meta: set `sRGBTexture: 0` and `textureType: 1`
4. Create `MaterialName.mat` using VegetationURP shader (GUID: `d2386f32c82b00c4c8c9e0b8a205ebfd`, fileID: `-6465566751694194690`)
5. Set `Texture2D_138C554F` = albedo GUID, `_BumpMap` = normal GUID, `_MainTex` = albedo GUID
6. Set all wind/flutter/sway floats to 0 (buildings don't animate)
7. Set material `.meta` assetBundleName to `splicetails_mac`
8. Add material name to `MaterialCollection.Splicetails.blueprint.json`
9. Rebuild asset bundle AND mod

#### Timbermesh Blender plugin fix (Blender 5.x)
- Plugin at `~/Library/Application Support/Blender/5.1/scripts/addons/timbermesh_blender_plugin/`
- `__init__.py` lines 18-19 must use relative imports: `from . import timbermesh_exporter` (not absolute)
- Export is via RIGHT-CLICK on a sub-collection in the Outliner (not File > Export)
- In headless mode, call `Exporter.export_collection()` directly (operator not available)

### UnityPy Asset Extraction
- Extract vanilla textures: `UnityPy.load(resources_assets)` then iterate `Texture2D` objects using `m_Name`
- Extract timbermesh models: not possible via UnityPy — use `TimberbornExampleModels.blend` instead
- Game assets path: `~/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app/Contents/Resources/Data/resources.assets`
