# Store of No Returns

Unity (URP) game. Boss-fight code lives in `Assets/Scripts/BossFight/` (runtime, rule classes in `Rules/`)
and `Assets/Editor/BossFight/` (builders); tests in `Assets/Tests/Editor/`.

## Unity CLI
- Keep the Unity Editor closed while running the CLI against this project.
- Tests: `~/.unity/bin/unity test --mode EditMode --non-interactive --timeout 900 --output Library/boss-test-results.xml .`
  It prints nothing on success; read the XML (`<test-run ... failed="0">`). Compiler errors only show in
  `~/Library/Logs/Unity/Editor.log` (`grep 'error CS'`).
- A run compiles new scripts and generates `.meta` files for new files/folders — commit those `.meta` files.
- Run an editor method headless: `unity run . -- -executeMethod BossFight.Editor.BuildAll.Run`. Do NOT pass
  `-quit` (the CLI manages it and errors). `Debug.Log` output lands in `~/Library/Logs/Unity/Editor.log`.

## Tests
- EditMode NUnit only (no `[UnityTest]`, PlayMode or asmdef); gameplay rules are plain C# classes so they can be tested.
- EditMode tests run with an untitled scene loaded; `RestoreSceneManagerSetup` cannot restore a path-less scene,
  so guard it (see `BuildAllTests.RestoreSceneSetup`).
- The suite executes `BuildAll` (via `BuildAllTests`), and hashes Boss Scene/prefabs across two runs: builder
  output under Boss Scene, `Assets/Prefabs/BossFight/` and `Assets/Settings/BossFight/` must be committed and
  builders must not rewrite unchanged files.

## Builders (scene/prefab/asset changes)
- Never hand-edit `.unity/.prefab/.asset/.controller` YAML and never modify `MainScene.unity`; changes come from
  idempotent builder steps listed in `BossFight.Editor.BuildAll.Steps`, each with a check in `BossRigValidator`.
- Shared helpers are in `BuilderUtil` (`FindInScene<T>`, `FindPlayer`, `FindByName`, `SetReference`,
  `RequireFields`, `EnsureLitMaterial`, `EnsureTransparentMaterial`). The player is whichever object has `PlayerMovement`.
- Materials are created once under `Assets/Settings/BossFight` via `Shader.Find("Universal Render Pipeline/...")`,
  throwing if the shader is missing. TMP labels made by builders need `font = TMP_Settings.defaultFontAsset`.
- Builders that "create when missing" (tools, bookshelves) don't move existing objects: delete the object in
  Boss Scene to regenerate after changing placement constants.
- New `BossFightSettings` fields need no asset edit: Unity fills the missing field from its initializer on load.

## Code gotchas
- Unity's `??`/`?.` don't respect Unity fake-null; use explicit `!= null` on `UnityEngine.Object`.
- Global-namespace scripts using BossFight code need `using BossFight;`; `BossBattleTest` has a static member
  named `BossFight`, so the namespace is only usable outside that class.
- `PlayerMovement.mouseSensitivity` is serialized as 100 in existing scenes; `MoveSmoothing.NormalizeSensitivity`
  maps values >5 to 0.1 deg/px, so don't rely on the serialized value.
- `character.fbx` (Human) has no skeleton. The project has no custom physics layers and the Default layer holds
  floor, walls and actors alike: layer-dependent code resolves an unnamed layer at runtime, and something that
  should touch only the ground needs its own layer plus a floor proxy (see `MonsterDrag.EnsureFloor`).
- The URP renderer has no Decal feature; use textured quads instead of `DecalProjector`.
- macOS `sed -i` needs `sed -i ''`.
