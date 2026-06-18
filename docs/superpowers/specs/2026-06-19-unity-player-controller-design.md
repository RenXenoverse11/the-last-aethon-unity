# Player Controller / Gameplay Systems — Design Spec

**Sub-project #2** of the Unity redesign of "The Last Aethon" (see [[project_last_aethon_unity_redesign]]). Follows sub-project #1 ("Project foundation & scene flow" — `docs/superpowers/specs/2026-06-18-unity-foundation-scene-flow-design.md`), which is complete and pushed to `origin/main`.

## Source of truth

Original Phaser 3/TypeScript implementation at `../the-last-aethon` (sibling repo):
- `src/objects/Player.ts` — movement, jump, attack-trigger, HP/regen/hurt/knockback state machine
- `src/scenes/GameScene.ts` — world bounds, ground/platform layout, camera follow
- `src/objects/ParallaxBackground.ts` — 3-layer scrolling background (deferred, see Out of Scope)
- `src/ui/HUD.ts` — HP bar overlay

## Scope

### In scope

- `PlayerController` — movement (walk/run), jump, attack-trigger (animation only — no hit detection, since no enemies exist in either codebase yet), reading from the already-stubbed `Assets/Input/PlayerControls.inputactions` `Gameplay` action map
- `PlayerHealth` — HP/maxHP (100/100), regen (+5 every 5s while not hurt), `TakeDamage()` with knockback + hurt state, ported 1:1 from the original's values
- A 6-state Animator Controller (Idle/Walk/Run/Jump/Attack/Hurt) driving a placeholder sprite (tint/squash per state) — state-machine plumbing only; real sprite animation clips swap in during a future art pass
- A simple HP HUD (bar + number), Canvas-based, consistent with the MainMenu UI work from sub-project #1
- Level geometry: ground + 5 floating platforms (primitive colliders, same relative layout as the original `buildGround()`), world 4 screens wide
- Camera: Cinemachine virtual camera following the player (Main Camera converted to Orthographic + `CinemachineBrain`)
- Input bindings filled into the existing `Gameplay` action map, plus a new debug-only `Debug` action map with a damage-test key

### Explicitly out of scope (deferred to future sub-projects)

- Real art: Ren/Vesper sprite sheets, the 3-layer parallax background. This sub-project uses placeholder visuals (flat-colored primitives) throughout — confirmed deliberately, to decouple controller/physics work from art-import work.
- Vesper — a sprite sheet with idle/walk/run/attack/jump/hurt animations exists in the source's `PreloadScene.ts`, but no `Vesper` behavior class exists in the original codebase. There's nothing to port; Vesper isn't created in this sub-project.
- Enemies, combat hit-detection, and the dialogue system — none of these are wired up in the original source either (`takeDamage()` has no caller; `triggerIntroDialogue()` is defined but never called in `GameScene.create()`).
- Automated test infrastructure (Unity Test Framework) — see Testing section.

## Architecture

### Player controller

New namespace `TheLastAethon.Gameplay`, alongside the existing `TheLastAethon.Core` and `TheLastAethon.UI` from sub-project #1.

- **`Assets/Scripts/Gameplay/PlayerController.cs`** — reads `Move`/`Run`/`Jump`/`Attack` actions from the Input System, drives a `Rigidbody2D` (walk/run horizontal velocity, jump impulse, gravity), sets Animator parameters (`Speed`, `IsRunning`, `IsGrounded`, `Attack` trigger), blocks movement/attack input while `PlayerHealth.IsHurt` is true — mirrors the original's `if (isHurt) return` guard.
- **`Assets/Scripts/Gameplay/PlayerHealth.cs`** — `hp`/`maxHp` fields, a regen timer (+5 HP every 5000ms while not hurt, paused while hurt — exact values from the original), `TakeDamage(amount)` (clamps HP to 0, sets hurt flag, applies knockback velocity away from facing direction, fires a `Hurt` animator trigger). Kept independent of `PlayerController`'s movement implementation so each can be understood/tested on its own.
- **`Assets/Animations/Player/PlayerAnimatorController.controller`** — 6 states (Idle/Walk/Run/Jump/Attack/Hurt). Each state's clip tints a placeholder `SpriteRenderer` square a distinct color (and applies a small scale squash for Jump/Attack) — no real sprite frames yet, but the same transition graph (idle↔walk↔run, jump as an overlay/interrupt, attack/hurt as one-shot states returning to idle) that real animation clips will plug into later.

**Deviation from the original, called out explicitly:** the original resizes the physics hitbox per-animation (`setBodySize`/`setOffset` in `Player.ts`) to match each sprite frame's silhouette. Since there are no real frames yet, the placeholder uses one fixed `BoxCollider2D` size across all states. Per-state hitbox tuning is deferred to the art pass, once real frame dimensions exist.

**Units note:** the original uses pixel-space velocities (walk 160, run 280, jump impulse magnitude 600, `gravityY` 200 — all px/s in Phaser's Arcade Physics). Unity's 2D physics is meter-based, so these convert proportionally (approximately 3.2 m/s walk, 5.6 m/s run, 9 m/s jump impulse) rather than porting the raw pixel numbers verbatim. Exact constants are tuned by feel during implementation and Play Mode verification, not treated as fixed requirements.

### Level (`Assets/Scenes/Game.unity`)

- Ground: one long `BoxCollider2D` + flat-color `SpriteRenderer`, spanning 4 screen-widths (proportional to the original's `width * 4` world bounds)
- 5 floating platforms at the same relative x/y/width proportions as the original's `buildGround()` platform array, same primitive (collider + flat-color sprite) treatment
- Player spawns near the left edge of the world, standing on the ground

### Camera

- Add the `com.unity.cinemachine` package (not yet installed — confirmed via `Packages/manifest.json`)
- Main Camera: currently a default perspective 3D camera (`orthographic: 0`, confirmed in `Assets/Scenes/Game.unity`) — convert to Orthographic (`orthographic: 1`) and add a `CinemachineBrain`
- One `CinemachineCamera` following the Player, damping tuned to feel similar to the original's Phaser `startFollow(player, true, 0.1, 0.1)` lerp smoothing

### HUD

- `Assets/Scripts/UI/PlayerHUD.cs` (namespace `TheLastAethon.UI`) — a Canvas-based HP bar (width or fill-amount driven by `hp / maxHp`) and an HP number label, updating from `PlayerHealth`. Color thresholds match the original `HUD.ts`'s actual values exactly — `pct > 0.5` → `#C0392B`, `pct > 0.25` → `#E67E22`, else → `#E74C3C` (the source's own inline comment claims "green → yellow → red," but none of its three colors are green; porting the real values, not the misleading comment).

### Input

Filling in the currently-empty `Gameplay` action map in `Assets/Input/PlayerControls.inputactions` (which already has the 4 actions — `Move`/`Run`/`Jump`/`Attack` — stubbed from sub-project #1, with no bindings yet):

- `Move`: WASD + Arrow Keys (2D vector composite) + Gamepad Left Stick
- `Run`: Left Shift + Gamepad West button
- `Jump`: Space + Up Arrow + Gamepad South button
- `Attack`: Left Mouse Button + Z + Gamepad West button

New `Debug` action map (dev-only, separate from `Gameplay`):
- `DamageTest`: H key → calls `PlayerHealth.TakeDamage()` for manual verification of hurt/knockback/regen, since nothing else calls `TakeDamage()` yet (same situation as the original — `takeDamage()` exists but has no caller until a future combat sub-project adds enemies). No gamepad binding needed. The `Debug` map stays active in all builds for this sub-project — there's no release/build-configuration pipeline established yet to gate it behind, and gating can be added later once one exists.

## Testing approach

Following the precedent set in sub-project #1, verification is manual Play Mode end-to-end checking rather than introducing Unity Test Framework infrastructure: move/run/jump across platforms, attack animation trigger, H-key damage → hurt → knockback → regen cycle, camera follow smoothness, and HUD updates. This codebase has no automated gameplay tests yet, and this sub-project's logic is tightly coupled to physics and the `Update`/`FixedUpdate` loop, where integration-style manual checks are more informative than isolated unit tests would be.

## Global constraints carried over from sub-project #1

- Unity Editor 6000.5.0f1
- Namespaces: `TheLastAethon.Core`, `TheLastAethon.UI`, and now `TheLastAethon.Gameplay`
- No third-party tween/animation libraries (Cinemachine is a first-party Unity package, not third-party — does not violate this constraint)
- No audio system
- Resizable window, no enforced fullscreen, default 1920×1080
- `mcp__UnityMCP__*` tooling for all Editor-side work; `read_console` error-check after every script edit
- Independently re-verify raw scene/asset YAML after MCP tool calls rather than trusting "success" responses alone (see [[feedback_unity_mcp_trust_but_verify]] and [[project_unity_mcp_quirks]])
