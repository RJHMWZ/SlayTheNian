# SlayTheNian development guide

- Unity version: **2022.3.62f2**. Keep the existing Unity/URP/package versions unless a task explicitly requires an upgrade.
- Game-owned assets live under `Assets/Sleigh`. Leave Unity templates under `Assets/Settings`, `Assets/Scenes/SampleScene.unity`, and all package/plugin internals alone.
- The playable scene is `Assets/Sleigh/Scenes/SleighTheSpire.unity`; `SleighProjectSetup.ScenePath` and `EditorBuildSettings` must agree.
- Preserve `.meta` files and GUIDs when moving assets. Never change gameplay data, battle rules, PlayerPrefs keys, serialized field names, or UI dimensions as incidental cleanup.
- Runtime scripts are grouped into Core, Gameplay, Battle, Data, UI, Presentation, Audio and Utilities. `Development/Test.cs` is a retained template, not a production entry point.
- `GameRoot` partial files remain in the same runtime assembly. **Do not move `Core/SleighSceneAuthoring.cs` into an Editor assembly**: its editor-only code accesses the runtime partial class's private members.
- `Resources/Sleigh/Prefabs`, `Audio/AudioBank`, font/localization keys, and root JSON catalog keys are compatibility boundaries. Sprite paths are defined by `ArtIndex.json`, `FestivalHudArt.json`, and `FestivalMapArt.json`; update those manifests when moving sprite files.
- Supplied music/effects live in `Audio/BGM` and `Audio/SFX`; generated effects live in `Audio/SFX/Generated`. `Editor/Audio/SleighSoundBaker.cs` binds these assets. `FestivalSoundSynth.cs` generates PCM offline; do not synthesize sounds in the player.
- `Audio/Legacy`, legacy/reference art, source font licenses, the sample scene and the archived reference importer are intentionally retained. Static non-use alone is insufficient evidence to delete an asset.
- UI elements are pooled through `PixelPoolBucket`/`PixelPoolElement`; clear event bindings on reuse. Clone mutable runtime materials and release owned instances; never animate shared material assets.
- Do not clean Library, Temp, Obj, Logs, or user/editor caches. Do not rebuild or save unrelated dirty scenes.

## Validation

1. Run `python Tools/audit_project.py` for GUIDs, package references, Resources paths, sprite manifests and build-scene paths.
2. In Unity, run **Sleigh > Verify Project Integrity**, then existing theme/rule/presentation/viewport/transition checks. Save user scene edits first, or use an isolated copy for batch validation.
3. Batch entry point: `Sleigh.Editor.SleighProjectChecks.ValidateBatch`. Run it against a copied project when an editor is already open; this checks real Unity scene/prefab loading and existing rules without changing the working scene.
4. `python Tools/compile_project.py` compiles current source using the local Unity-generated project references, without changing those generated `.csproj` files. Unity must already have generated the project files and installed its existing packages.
5. Inspect `git diff --check`, GUID/reference audit output and the actual changed file list. Preserve pre-existing user changes.

After lifecycle/input changes, compilation and Edit Mode checks are insufficient. Run `Sleigh.Editor.SleighPlayModeChecks.StartBatch` in the isolated `Temp/ProjectOrganizationAudit/UnityValidation` copy. It uses a separate PlayerPrefs scope, reloads scripts while the menu is open, verifies UI raycasts, and follows character selection, save slots, intro, map and combat. Editor script reload rebuilds runtime bindings and returns to the title; it must not leave a visible but unresponsive menu.

See `Docs/ProjectStructure.md` for layout, dependencies and retained cleanup candidates.
