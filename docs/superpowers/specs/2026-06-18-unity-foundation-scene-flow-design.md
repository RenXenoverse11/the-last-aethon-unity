# Project Foundation & Scene Flow — Design

## Context

"The Last Aethon" was originally built as a Phaser 3 / TypeScript web game (sibling folder `../the-last-aethon`). The user wants to continue development of the game in Unity (`the-last-aethon-unity`), treating the Phaser version as design reference rather than a literal porting target — i.e. a fresh redesign informed by, but not bound to, the existing implementation.

The overall port has been decomposed into independent sub-projects, each with its own spec → plan → implementation cycle:

1. **Project foundation & scene flow** (this spec)
2. Player controller & animation
3. World & parallax background
4. Combat & HP system
5. Dialogue system
6. Narrative content

This spec covers only #1.

The Unity project currently contains a single empty scene with just a `Main Camera`. `Packages/manifest.json` already includes `com.unity.render-pipelines.universal` (URP), `com.unity.inputsystem`, and the 2D packages (sprite, animation, tilemap, etc.) — no additional package dependencies are required for this sub-project.

The original Phaser game has **no audio** — `PreloadScene.ts` contains no `load.audio` calls anywhere. This is not an oversight in this spec; an audio system is simply out of scope until/unless requested separately.

## Goals

- A `MainMenu` scene that is fully built and playable: looping title video background, blinking "press enter to begin" prompt, version label, and Enter/Space/click/gamepad-submit to proceed.
- A `Game` scene that exists and is reachable from the Main Menu, but contains no gameplay yet (placeholder scaffold only — Camera + Directional Light) — gameplay content is built by later sub-projects.
- A reusable scene-transition mechanism (fade-to-black) and a persistent `GameManager` singleton that owns it, so later sub-projects (dialogue pause/resume, death → menu, eventual save/load) have a place to hook in without retrofitting.
- An Input Actions setup (`PlayerControls.inputactions`) with a `UI` action map wired up now (driving Main Menu submit) and a `Gameplay` action map stubbed for the Player Controller sub-project to fill in later — designed to be control-scheme-agnostic (keyboard + gamepad bindings) per the target of supporting gamepad/handheld in the future without reworking input later.
- Folder/script conventions established for the rest of the port to follow.

## Non-Goals

- Any gameplay in the `Game` scene (player, world/parallax, combat, dialogue, HUD) — covered by later sub-projects.
- Audio/music system — the source material has none; not being introduced here.
- A graphics/options settings menu — not needed yet since fullscreen is not being enforced (see Decisions).
- WebGL or mobile/handheld builds — Windows standalone only for now; input is designed to not block adding control schemes later, but no other platform-specific work is in scope.
- A splash/logo screen before the Main Menu — explicitly skipped per user decision; can be added later as a small addition once the scene-transition mechanism exists.

## Decisions

These were confirmed with the user during brainstorming and are locked for this spec:

- **Window/fullscreen**: standard resizable window, fullscreen not enforced. (The Phaser version had a half-finished "fullscreen required" guard, fully commented out — not being revived.)
- **Platform**: PC desktop (Windows standalone) build target now. Input is built on Unity's Input System with action maps designed to be control-scheme-agnostic, so gamepad/handheld support can be added later without reworking the player controller.
- **Startup flow**: launch directly into `MainMenu` — no splash/logo screen.
- **Scope of this sub-project**: includes the real Main Menu content (not just empty scaffolding), since it's cheap to build once the scene/transition plumbing exists and gives an immediately presentable result.

## Architecture

### Scenes

- `Assets/Scenes/MainMenu.unity` — fully built per Goals.
- `Assets/Scenes/Game.unity` — Camera + Directional Light only (per UnityMCP convention for new scenes). Later sub-projects add content here.

### GameManager (persistent singleton)

- `Assets/Scripts/Core/GameManager.cs`. Lives in `MainMenu` scene, calls `DontDestroyOnLoad` on itself, persists across the `MainMenu` → `Game` transition.
- Owns scene loading: exposes a method (e.g. `LoadGame()`) that triggers `SceneManager.LoadSceneAsync` wrapped in a fade-to-black/fade-in transition.
- Fade transition is implemented via a full-screen `CanvasGroup` that GameManager controls directly (simple alpha lerp coroutine) — no third-party tween library is introduced, consistent with YAGNI.
- This is the integration point future sub-projects use for scene-level coordination (e.g., dialogue scene pause/resume, combat death → menu).

### Input System

- `Assets/Input/PlayerControls.inputactions`, with C# class generation enabled (type-safe action references, no string-based lookups).
- `UI` action map: `Submit`/`Cancel`, bound to Enter/Space/click (keyboard+mouse) and gamepad South/East buttons. Drives the Main Menu's "press enter to begin" flow.
- `Gameplay` action map: `Move`, `Run`, `Jump`, `Attack` — created now as empty stubs (action names + control type only, no behavior) so the Player Controller sub-project starts from an existing asset rather than creating its own input setup from scratch.

### Main Menu content

- Title video: `VideoPlayer` component playing the imported `game_title_video` clip, rendering to a `RenderTexture`, displayed on a full-screen `RawImage` inside a `Canvas` (Screen Space - Overlay). Looping, matching the original's behavior.
- Blinking prompt: a TextMeshPro text object, "[ PRESS ENTER TO BEGIN ]", alpha-pulsed between 1.0 and 0.2 on an ~800ms sine cycle via a small script (no tween library). Triggers `GameManager.Instance.LoadGame()` on `UI/Submit` or on pointer click.
- Version label: "v0.1.0 — Act I: Ashenveil", bottom-right corner, monospace TMP font.
- **Asset gap to resolve during implementation**: the original used a web monospace font; Unity needs an actual TMP font asset. A comparable monospace font (e.g. a freely-licensed one already on the system, or Unity's built-in fallback) will be sourced/imported as part of implementation — flagged here so it isn't a surprise mid-build.

### Project & build settings

- Player settings: resizable window, default windowed resolution 1920×1080 (matches the original Phaser canvas size), fullscreen not forced.
- Build target: Windows standalone (Mono or IL2CPP — default Unity choice, no special requirement here).
- URP: confirm a 2D Renderer asset exists and is assigned as the active Render Pipeline Asset in Graphics settings (package is present in the manifest; the asset/assignment itself has not yet been verified in-editor and will be checked/created during implementation if missing).

### Folder conventions

```
Assets/
  Scenes/        MainMenu.unity, Game.unity
  Scripts/
    Core/        GameManager.cs
    UI/          MainMenuController.cs
  Input/         PlayerControls.inputactions
  Art/           imported Phaser assets (video, logo, fonts, etc.)
```

Later sub-projects extend this structure (e.g. `Scripts/Player/`, `Scripts/World/`) rather than introducing a different convention.

## Testing

This sub-project is UI/flow plumbing with no gameplay logic, so automated tests provide little value here. Verification will be manual, via the Unity Editor (Play Mode) and a standalone build:

- Launching the project enters `MainMenu` directly (no splash).
- Title video plays and loops; blinking prompt pulses continuously.
- Enter, Space, mouse click, and a gamepad submit input all trigger the transition to `Game`.
- The fade transition plays smoothly in both directions without visual popping.
- `Game` scene loads with just Camera + Directional Light, no errors in the Console.
- The window is resizable and not locked to fullscreen.

## Open Questions

None outstanding — all decisions needed to implement this sub-project were resolved during brainstorming (see Decisions). The monospace font sourcing (noted under Main Menu content) is an implementation task, not an open design question — any reasonably close monospace font satisfies the design.
