# Project Foundation & Scene Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Unity scene skeleton for "The Last Aethon" — a fully working `MainMenu` scene (title video, blinking prompt, version label, click/Enter/Space/gamepad to start) that transitions via a persistent `GameManager` into an empty `Game` scene scaffold, with an Input Actions asset in place for the next sub-project.

**Architecture:** Two scenes (`MainMenu`, `Game`) registered in Build Settings. A `GameManager` singleton (`DontDestroyOnLoad`) created in `MainMenu` owns a fade-to-black `CanvasGroup` overlay and scene transitions. UI interaction (click/Enter/Space/gamepad submit) rides on Unity's standard `EventSystem` + `InputSystemUIInputModule`, created via Unity's own "GameObject > UI > Event System" menu command rather than hand-wired — this gives the exact Submit/Cancel/Point/Click bindings the spec calls for (keyboard, mouse, gamepad) for free, with no fragile manual `InputActionReference` plumbing. A separate `PlayerControls.inputactions` asset holds only the `Gameplay` action map (Move/Run/Jump/Attack), stubbed with no bindings yet for the Player Controller sub-project to fill in.

**Tech Stack:** Unity 6000.5.0f1, URP 17.5.0, Input System 1.19.0, TextMeshPro (via `com.unity.ugui` 2.5.0), C#.

**Source spec:** `docs/superpowers/specs/2026-06-18-unity-foundation-scene-flow-design.md`

**Source asset for the title video:** `the-last-aethon/public/assets/ui/game_title.mp4` (sibling Phaser project, read-only reference — copy, don't move).

## Global Constraints

- Unity Editor version: 6000.5.0f1 — do not introduce APIs requiring a newer/older Editor.
- C# namespace convention for this sub-project: `TheLastAethon.Core` (GameManager) and `TheLastAethon.UI` (menu-related scripts).
- No third-party tween/animation libraries — fades and blinking are hand-rolled coroutines/`Update` loops (YAGNI, per spec Non-Goals).
- No audio system — the source Phaser game has none; do not add one.
- Window: resizable, fullscreen NOT enforced (per spec Decisions).
- Default windowed resolution: 1920×1080 (matches original Phaser canvas).
- All new scripts go under `Assets/Scripts/<Core|UI>/`, scenes under `Assets/Scenes/`, the Input Actions asset under `Assets/Input/`, imported art/video under `Assets/Art/`.
- Tooling: this project is wired to the UnityMCP server (`mcp__UnityMCP__*` tools) inside the active Claude Code session — use those tools for all Editor-side operations (scenes, GameObjects, components, assets) rather than hand-editing `.unity`/`.meta` YAML directly, except where a step explicitly says to write a file (e.g. the `.inputactions` JSON, which Unity auto-imports on refresh).
- After every script create/edit, call `read_console` (action `get`, filter for `error`) before moving on — do not proceed past a task with unresolved compile errors.
- Commit after every task with `git add` of the exact files touched (never `git add -A`).

---

### Task 1: Input Actions asset (Gameplay stub)

**Files:**
- Create: `Assets/Input/PlayerControls.inputactions`

**Interfaces:**
- Produces: an `InputActionAsset` named `PlayerControls` with one action map `Gameplay` containing actions `Move` (Vector2), `Run` (Button), `Jump` (Button), `Attack` (Button) — all with zero bindings. The Player Controller sub-project will add bindings and read these by name (`asset.FindActionMap("Gameplay").FindAction("Move")` etc.) — no other task in this plan touches this file again.

- [ ] **Step 1: Create the folder and the asset file**

Write `Assets/Input/PlayerControls.inputactions` with this exact content:

```json
{
    "name": "PlayerControls",
    "maps": [
        {
            "name": "Gameplay",
            "id": "8f1b6a2e-0002-4000-8000-000000000001",
            "actions": [
                {
                    "name": "Move",
                    "type": "Value",
                    "id": "8f1b6a2e-0002-4000-8000-000000000002",
                    "expectedControlType": "Vector2",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": true
                },
                {
                    "name": "Run",
                    "type": "Button",
                    "id": "8f1b6a2e-0002-4000-8000-000000000003",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Jump",
                    "type": "Button",
                    "id": "8f1b6a2e-0002-4000-8000-000000000004",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                },
                {
                    "name": "Attack",
                    "type": "Button",
                    "id": "8f1b6a2e-0002-4000-8000-000000000005",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": []
        }
    ],
    "controlSchemes": [
        {
            "name": "Keyboard&Mouse",
            "bindingGroup": "Keyboard&Mouse",
            "devices": [
                { "devicePath": "<Keyboard>", "isOptional": false, "isOR": false },
                { "devicePath": "<Mouse>", "isOptional": true, "isOR": false }
            ]
        },
        {
            "name": "Gamepad",
            "bindingGroup": "Gamepad",
            "devices": [
                { "devicePath": "<Gamepad>", "isOptional": false, "isOR": false }
            ]
        }
    ]
}
```

- [ ] **Step 2: Let Unity import it and verify**

Call `mcp__UnityMCP__refresh_unity`. Then call `mcp__UnityMCP__read_console(action="get", types=["error"])`. Expected: no errors mentioning `PlayerControls` or `InputActionAsset`. Then call `mcp__UnityMCP__manage_asset(action="get_info", path="Assets/Input/PlayerControls.inputactions")` and confirm it resolves with an asset type related to Input Actions (not "not found" / not a generic text asset).

- [ ] **Step 3: Commit**

```bash
git add "Assets/Input/PlayerControls.inputactions"
git commit -m "Add PlayerControls input actions asset with stubbed Gameplay map"
```

---

### Task 2: GameManager script

**Files:**
- Create: `Assets/Scripts/Core/GameManager.cs`

**Interfaces:**
- Produces: `TheLastAethon.Core.GameManager`, a `MonoBehaviour` with `public static GameManager Instance { get; }`, `public void LoadGame()`, and two serialized fields: `fadeCanvasGroup` (`CanvasGroup`) and `fadeDuration` (`float`, default `0.5`). Task 5 creates the GameObject this script attaches to and assigns `fadeCanvasGroup`. Task 8's `MainMenuController` calls `GameManager.Instance.LoadGame()`.
- Consumes: a scene literally named `"Game"` must exist and be registered in Build Settings (done in Task 3) before `LoadGame()` can succeed at runtime.

- [ ] **Step 1: Create the script**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="GameManager"`, `path="Assets/Scripts/Core"`, `contents`:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLastAethon.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadGame()
        {
            StartCoroutine(LoadSceneWithFade("Game"));
        }

        private IEnumerator LoadSceneWithFade(string sceneName)
        {
            yield return Fade(0f, 1f);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = to;
            fadeCanvasGroup.blocksRaycasts = to > 0.5f;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])`. Expected: no errors referencing `GameManager.cs`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Core/GameManager.cs"
git commit -m "Add GameManager singleton with fade-and-load scene transition"
```

---

### Task 3: Game scene scaffold

**Files:**
- Create: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: a scene named `Game`, registered in Build Settings, containing only `Main Camera` (tag `MainCamera`) and `Directional Light`. Task 2's `GameManager.LoadGame()` loads this scene by name. Every later sub-project (Player, World, Combat, Dialogue) adds its content here.

- [ ] **Step 1: Create the scene**

Call `mcp__UnityMCP__manage_scene(action="create", name="Game", path="Assets/Scenes")`.

- [ ] **Step 2: Confirm it's the active scene**

Call `mcp__UnityMCP__manage_scene(action="get_active")`. Expected: the active scene's name is `Game`. If not, call `mcp__UnityMCP__manage_scene(action="load", scene_name="Game", scene_path="Assets/Scenes/Game.unity")` before continuing.

- [ ] **Step 3: Add Camera and Directional Light**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="Main Camera",
  components_to_add=["Camera", "AudioListener"],
  tag="MainCamera",
  position=[0, 0, -10]
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="Directional Light",
  components_to_add=["Light"],
  component_properties={"Light": {"type": 1}},
  rotation=[50, -30, 0]
)
```

(`Light.type` enum: `1` = Directional.)

- [ ] **Step 4: Verify the hierarchy**

Call `mcp__UnityMCP__manage_scene(action="get_hierarchy")`. Expected: exactly two root objects, `Main Camera` and `Directional Light`.

- [ ] **Step 5: Save the scene and register it in Build Settings**

```
mcp__UnityMCP__manage_scene(action="save")
mcp__UnityMCP__manage_build(action="scenes", scenes="[\"Assets/Scenes/MainMenu.unity\", \"Assets/Scenes/Game.unity\"]")
```

(`MainMenu.unity` doesn't exist yet — that's fine, Task 4 creates it next; this call establishes the final build order with `MainMenu` at index 0. If the tool rejects a path that doesn't exist yet, re-run this exact call at the end of Task 4 instead.)

- [ ] **Step 6: Verify no console errors, then commit**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

```bash
git add "Assets/Scenes/Game.unity" "Assets/Scenes/Game.unity.meta" "ProjectSettings/EditorBuildSettings.asset"
git commit -m "Add empty Game scene scaffold (Camera + Directional Light)"
```

---

### Task 4: MainMenu scene scaffold

**Files:**
- Create: `Assets/Scenes/MainMenu.unity`

**Interfaces:**
- Produces: a scene named `MainMenu`, registered in Build Settings at index 0 (so it's the scene that loads on game launch), containing `Main Camera`, `Directional Light`, and an `EventSystem` (with `InputSystemUIInputModule`, auto-wired to Unity's default UI actions by the menu command in Step 3 — this is what makes Enter/Space/click/gamepad-submit work in Task 8 without any custom UI input-action wiring). Tasks 5–8 add content into this scene.

- [ ] **Step 1: Create the scene**

Call `mcp__UnityMCP__manage_scene(action="create", name="MainMenu", path="Assets/Scenes")`.

- [ ] **Step 2: Confirm it's active**

Call `mcp__UnityMCP__manage_scene(action="get_active")`. Expected: active scene is `MainMenu`. If not, load it explicitly as in Task 3 Step 2.

- [ ] **Step 3: Add Camera, Light, and EventSystem**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="Main Camera",
  components_to_add=["Camera", "AudioListener"],
  tag="MainCamera",
  position=[0, 0, -10]
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="Directional Light",
  components_to_add=["Light"],
  component_properties={"Light": {"type": 1}},
  rotation=[50, -30, 0]
)

mcp__UnityMCP__execute_menu_item(menu_path="GameObject/UI/Event System")
```

The third call creates an `EventSystem` GameObject with `InputSystemUIInputModule` already attached and wired to Unity's default UI actions (creating that default actions asset under `Assets/` the first time it's used, if one doesn't already exist) — this is standard Unity Editor behavior, not custom code.

- [ ] **Step 4: Verify the hierarchy**

Call `mcp__UnityMCP__manage_scene(action="get_hierarchy")`. Expected: root objects `Main Camera`, `Directional Light`, `EventSystem`.

- [ ] **Step 5: Save and register Build Settings**

```
mcp__UnityMCP__manage_scene(action="save")
mcp__UnityMCP__manage_build(action="scenes", scenes="[\"Assets/Scenes/MainMenu.unity\", \"Assets/Scenes/Game.unity\"]")
mcp__UnityMCP__manage_build(action="status")
```

Confirm the status output lists both scenes with `MainMenu.unity` at index 0.

- [ ] **Step 6: Verify no console errors, then commit**

```bash
git add "Assets/Scenes/MainMenu.unity" "Assets/Scenes/MainMenu.unity.meta" "ProjectSettings/EditorBuildSettings.asset"
git commit -m "Add MainMenu scene scaffold with EventSystem"
```

(If step 3's `Event System` menu command created a new default actions asset under `Assets/`, `git add` that file too — check `git status` and include any new untracked `.inputactions`/`.asset` file the menu command produced.)

---

### Task 5: GameManager GameObject + fade overlay

**Files:**
- Modify: `Assets/Scenes/MainMenu.unity` (adds GameObjects, no script changes)

**Interfaces:**
- Produces: a `GameManager` GameObject in the `MainMenu` scene with the `GameManager` component attached and its `fadeCanvasGroup` field wired to a child `FadeCanvas`'s `CanvasGroup`. Task 8's `MainMenuController` calls `GameManager.Instance.LoadGame()` at runtime — this task makes that call resolvable and functional.
- Consumes: `TheLastAethon.Core.GameManager` from Task 2.

- [ ] **Step 1: Confirm MainMenu is the active scene**

Call `mcp__UnityMCP__manage_scene(action="get_active")`; load `MainMenu` if it isn't active.

- [ ] **Step 2: Create the GameManager GameObject and attach the script**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="GameManager",
  components_to_add=["TheLastAethon.Core.GameManager"]
)
```

Note the returned instance ID as `<gameManagerId>`.

- [ ] **Step 3: Create the fade overlay as a child of GameManager**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="FadeCanvas",
  parent="GameManager",
  components_to_add=["Canvas", "CanvasScaler", "GraphicRaycaster", "CanvasGroup"],
  component_properties={
    "Canvas": {"renderMode": 0, "sortingOrder": 100},
    "CanvasScaler": {"uiScaleMode": 1, "referenceResolution": {"x": 1920, "y": 1080}, "screenMatchMode": 0},
    "CanvasGroup": {"alpha": 0, "blocksRaycasts": false}
  }
)
```

Note the returned instance ID as `<fadeCanvasId>`.

- [ ] **Step 4: Add the full-screen black image as a child of FadeCanvas**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="FadeImage",
  parent="FadeCanvas",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0, "g": 0, "b": 0, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}
  }
)
```

- [ ] **Step 5: Wire GameManager's fadeCanvasGroup field**

```
mcp__UnityMCP__manage_components(
  action="set_property", target="<gameManagerId>", component_type="TheLastAethon.Core.GameManager",
  property="fadeCanvasGroup", value="<fadeCanvasId>"
)
```

(Replace `<gameManagerId>` / `<fadeCanvasId>` with the actual instance IDs returned in Steps 2 and 3.)

- [ ] **Step 6: Verify wiring**

Read back the `GameManager` component on `<gameManagerId>` via the `mcpforunity://scene/gameobject/{id}/components` resource (or `find_gameobjects` + component inspection) and confirm `fadeCanvasGroup` is no longer null/unassigned.

- [ ] **Step 7: Save, check console, commit**

```
mcp__UnityMCP__manage_scene(action="save")
```

Call `read_console(action="get", types=["error"])` — expect none.

```bash
git add "Assets/Scenes/MainMenu.unity"
git commit -m "Wire GameManager and fade overlay into MainMenu scene"
```

---

### Task 6: Title video background

**Files:**
- Create: `Assets/Art/Video/game_title.mp4` (copied from `the-last-aethon/public/assets/ui/game_title.mp4`)
- Create: `Assets/Art/Video/TitleVideoRT.renderTexture`
- Modify: `Assets/Scenes/MainMenu.unity` (adds GameObjects)

**Interfaces:**
- Produces: a looping video rendered full-screen behind all other Main Menu UI. No other task depends on this one's internals, only on it existing visually.

- [ ] **Step 1: Copy the video asset**

```bash
mkdir -p "Assets/Art/Video"
cp "../the-last-aethon/public/assets/ui/game_title.mp4" "Assets/Art/Video/game_title.mp4"
```

- [ ] **Step 2: Import and verify**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect no import errors for `game_title.mp4`.

- [ ] **Step 3: Create the RenderTexture asset**

```
mcp__UnityMCP__manage_asset(
  action="create", path="Assets/Art/Video/TitleVideoRT.renderTexture",
  asset_type="RenderTexture",
  properties={"width": 1920, "height": 1080}
)
```

- [ ] **Step 4: Confirm MainMenu is active, then create the Canvas + RawImage**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="MainMenuCanvas",
  components_to_add=["Canvas", "CanvasScaler", "GraphicRaycaster"],
  component_properties={
    "Canvas": {"renderMode": 0, "sortingOrder": 0},
    "CanvasScaler": {"uiScaleMode": 1, "referenceResolution": {"x": 1920, "y": 1080}, "screenMatchMode": 0}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="VideoBackground",
  parent="MainMenuCanvas",
  components_to_add=["RawImage"],
  component_properties={
    "RawImage": {"texture": "Assets/Art/Video/TitleVideoRT.renderTexture"},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}
  }
)
```

- [ ] **Step 5: Create the VideoPlayer**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="TitleVideoPlayer",
  components_to_add=["VideoPlayer"],
  component_properties={
    "VideoPlayer": {
      "playOnAwake": true,
      "isLooping": true,
      "renderMode": 2,
      "targetTexture": "Assets/Art/Video/TitleVideoRT.renderTexture",
      "clip": "Assets/Art/Video/game_title.mp4"
    }
  }
)
```

(`VideoPlayer.renderMode` enum: `2` = RenderTexture — `CameraFarPlane=0, CameraNearPlane=1, RenderTexture=2, MaterialOverride=3, APIOnly=4`.)

- [ ] **Step 6: Verify in Play Mode**

```
mcp__UnityMCP__manage_editor(action="play")
```

Wait ~2 seconds, then call `mcp__UnityMCP__manage_camera` screenshot action (or visually confirm via the Editor Game view) to confirm the video is visibly playing full-screen. Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none. Then `mcp__UnityMCP__manage_editor(action="stop")`.

- [ ] **Step 7: Save, commit**

```
mcp__UnityMCP__manage_scene(action="save")
```

```bash
git add "Assets/Art/Video/game_title.mp4" "Assets/Art/Video/game_title.mp4.meta" "Assets/Art/Video/TitleVideoRT.renderTexture" "Assets/Art/Video/TitleVideoRT.renderTexture.meta" "Assets/Scenes/MainMenu.unity"
git commit -m "Add looping title video background to MainMenu"
```

---

### Task 7: Blinking prompt + version label

**Files:**
- Create: `Assets/Scripts/UI/BlinkingText.cs`
- Modify: `Assets/Scenes/MainMenu.unity`

**Interfaces:**
- Produces: `TheLastAethon.UI.BlinkingText`, a `[RequireComponent(typeof(Graphic))]` `MonoBehaviour` that pulses its `Graphic`'s alpha between `minAlpha` (default `0.2`) and `maxAlpha` (default `1`) on a sine wave over `cycleDuration` seconds (default `0.8`) — generic, reusable on any future blinking UI text. Also produces the `PromptText` GameObject (with a `Button` component) that Task 8's `MainMenuController` wires up.

- [ ] **Step 1: Create BlinkingText.cs**

```
mcp__UnityMCP__manage_script(action="create", name="BlinkingText", path="Assets/Scripts/UI", contents="<see below>")
```

Contents:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace TheLastAethon.UI
{
    [RequireComponent(typeof(Graphic))]
    public class BlinkingText : MonoBehaviour
    {
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float cycleDuration = 0.8f;

        private Graphic targetGraphic;

        private void Awake()
        {
            targetGraphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * (2f * Mathf.PI / cycleDuration)) + 1f) * 0.5f;
            Color color = targetGraphic.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            targetGraphic.color = color;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

`read_console(action="get", types=["error"])` — expect none referencing `BlinkingText.cs`.

- [ ] **Step 3: Import TMP Essential Resources**

```
mcp__UnityMCP__execute_menu_item(menu_path="Window/TextMeshPro/Import TMP Essential Resources")
```

This is a one-time project setup step; it creates `Assets/TextMesh Pro/` with the default font (LiberationSans SDF) used below. (Per spec's Main Menu content note, the default TMP font is an accepted resolution for the "monospace font" gap — swapping in a custom monospace font asset later is cosmetic polish, not required for this task.)

- [ ] **Step 4: Create the PromptText GameObject**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="PromptText",
  parent="MainMenuCanvas",
  components_to_add=["TextMeshProUGUI", "Button", "TheLastAethon.UI.BlinkingText"],
  component_properties={
    "TextMeshProUGUI": {"text": "[ PRESS ENTER TO BEGIN ]", "fontSize": 28, "alignment": 514, "color": {"r": 1, "g": 1, "b": 1, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0.5, "y": 0}, "anchorMax": {"x": 0.5, "y": 0}, "pivot": {"x": 0.5, "y": 0}, "anchoredPosition": {"x": 0, "y": 120}, "sizeDelta": {"x": 600, "y": 60}}
  }
)
```

(`alignment: 514` is TMP's `TextAlignmentOptions.Center`.) Note the returned instance ID as `<promptTextId>`.

- [ ] **Step 5: Point the Button's targetGraphic at its own TextMeshProUGUI**

```
mcp__UnityMCP__manage_components(
  action="set_property", target="<promptTextId>", component_type="Button",
  property="targetGraphic", value="<promptTextId>"
)
```

- [ ] **Step 6: Create the VersionText GameObject**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="VersionText",
  parent="MainMenuCanvas",
  components_to_add=["TextMeshProUGUI"],
  component_properties={
    "TextMeshProUGUI": {"text": "v0.1.0 — Act I: Ashenveil", "fontSize": 16, "color": {"r": 0.6, "g": 0.6, "b": 0.6, "a": 1}, "alignment": 516},
    "RectTransform": {"anchorMin": {"x": 1, "y": 0}, "anchorMax": {"x": 1, "y": 0}, "pivot": {"x": 1, "y": 0}, "anchoredPosition": {"x": -12, "y": 12}, "sizeDelta": {"x": 400, "y": 30}}
  }
)
```

(`alignment: 516` is TMP's `TextAlignmentOptions.Right`.)

- [ ] **Step 7: Verify hierarchy and console, save, commit**

`manage_scene(action="get_hierarchy")` — confirm `MainMenuCanvas` now has children `VideoBackground`, `PromptText`, `VersionText`. `read_console(action="get", types=["error"])` — expect none.

```
mcp__UnityMCP__manage_scene(action="save")
```

```bash
git add "Assets/Scripts/UI/BlinkingText.cs" "Assets/Scenes/MainMenu.unity"
git commit -m "Add blinking prompt and version label to MainMenu"
```

(If Step 3 created new files under `Assets/TextMesh Pro/`, `git add` those too.)

---

### Task 8: MainMenuController + end-to-end verification

**Files:**
- Create: `Assets/Scripts/UI/MainMenuController.cs`
- Modify: `Assets/Scenes/MainMenu.unity`

**Interfaces:**
- Produces: `TheLastAethon.UI.MainMenuController`, a `MonoBehaviour` with a serialized `Button startButton` field, wiring `startButton.onClick` to `GameManager.Instance.LoadGame()`.
- Consumes: `TheLastAethon.Core.GameManager.Instance.LoadGame()` (Task 2/5), the `Button` on `PromptText` (Task 7), Unity's default UI actions via the `EventSystem` (Task 4) for Enter/Space/gamepad submit.

- [ ] **Step 1: Create MainMenuController.cs**

```
mcp__UnityMCP__manage_script(action="create", name="MainMenuController", path="Assets/Scripts/UI", contents="<see below>")
```

Contents:

```csharp
using TheLastAethon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastAethon.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Start()
        {
            startButton.onClick.AddListener(HandleStart);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(HandleStart);
        }

        private void HandleStart()
        {
            GameManager.Instance.LoadGame();
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

`read_console(action="get", types=["error"])` — expect none referencing `MainMenuController.cs`.

- [ ] **Step 3: Attach it and wire startButton**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="MainMenuController",
  components_to_add=["TheLastAethon.UI.MainMenuController"]
)
```

Note the returned instance ID as `<controllerId>`. Then, using `<promptTextId>` from Task 7 Step 4:

```
mcp__UnityMCP__manage_components(
  action="set_property", target="<controllerId>", component_type="TheLastAethon.UI.MainMenuController",
  property="startButton", value="<promptTextId>"
)
```

- [ ] **Step 4: Save the scene**

```
mcp__UnityMCP__manage_scene(action="save")
```

- [ ] **Step 5: End-to-end Play Mode verification**

```
mcp__UnityMCP__manage_editor(action="play")
```

Confirm via the Game view / a screenshot (`manage_camera` screenshot action) that: the title video is playing, the prompt text is visibly pulsing, and the version label is visible bottom-right. Then simulate pressing Enter (or click the prompt) and confirm the scene fades to black and `Game` loads (check `manage_scene(action="get_active")` reports `Game` after the transition, and `manage_scene(action="get_hierarchy")` shows only `Main Camera` + `Directional Light`). Call `read_console(action="get", types=["error"])` throughout — expect none at any point. Then `mcp__UnityMCP__manage_editor(action="stop")`.

If Enter/click doesn't trigger the transition, check in order: (a) `EventSystem` exists in the scene (Task 4), (b) `PromptText`'s `Button.targetGraphic` and `MainMenuController.startButton` are both assigned (Steps 3/5 above and Task 7 Step 5), (c) `GameManager.fadeCanvasGroup` is assigned (Task 5 Step 5).

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/UI/MainMenuController.cs" "Assets/Scenes/MainMenu.unity"
git commit -m "Wire MainMenuController to drive the GameManager scene transition"
```

---

## Self-Review Notes

- **Spec coverage:** MainMenu scene with video/prompt/version/input (Tasks 4,6,7,8) ✓; Game scene scaffold (Task 3) ✓; GameManager + fade transition (Tasks 2,5) ✓; Input Actions asset with Gameplay stub (Task 1) ✓; folder conventions (Global Constraints, all tasks) ✓; window/build settings (Global Constraints; Player Settings resolution/fullscreen are project-wide settings not requiring a dedicated task since they're already Unity's defaults for a new project — resizable window with fullscreen unforced is the out-of-the-box behavior, so no extra task was needed) ✓.
- **Deviation from spec, called out explicitly:** the spec described one `PlayerControls.inputactions` asset with both a `UI` map and a `Gameplay` map. This plan keeps only the `Gameplay` stub in that asset and satisfies the `UI` map's required bindings (Submit/Cancel/Point/Click across keyboard, mouse, gamepad) via Unity's standard auto-generated default UI actions, wired through `EventSystem`/`InputSystemUIInputModule` (Task 4 Step 3). Same functional behavior, lower implementation risk — avoids hand-authoring `InputActionReference` wiring that the available tools can't reliably script.
- **Type/name consistency check:** `GameManager.Instance.LoadGame()` (Task 2) is called identically in `MainMenuController.HandleStart()` (Task 8). `fadeCanvasGroup` field name matches between Task 2's declaration and Task 5's `set_property` call. Scene name `"Game"` (Task 2's `LoadSceneWithFade` argument) matches the scene created in Task 3 and the Build Settings entries in Tasks 3/4.
