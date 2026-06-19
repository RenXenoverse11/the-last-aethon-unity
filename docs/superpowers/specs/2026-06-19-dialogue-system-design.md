# Dialogue System — Design Spec

**Sub-project #3** of the Unity redesign of "The Last Aethon" (see [[project_last_aethon_unity_redesign]]). Follows sub-project #2 ("Player Controller / Gameplay Systems" — `docs/superpowers/specs/2026-06-19-unity-player-controller-design.md`), which is complete and pushed to `origin/main`.

## Source of truth

Original Phaser 3/TypeScript implementation at `../the-last-aethon` (sibling repo):
- `src/scenes/DialogueScene.ts` — the generic dialogue overlay: typewriter text, dual left/right portraits, dialogue-box repositioning, advance input, pause/resume choreography
- `src/scenes/GameScene.ts` (`triggerIntroDialogue()`) — the only dialogue content in the codebase; a 7-line intro sequence

**Important finding:** `triggerIntroDialogue()` is dead code in the original — it is defined but never called anywhere in `GameScene.ts` (no auto-fire in `create()`, no trigger zone, no key binding). There is no working trigger to replicate; only its reference content (the 7 lines) and the generic `DialogueScene` engine are real, exercised behavior.

## Scope

### In scope

- `DialogueUI` — a reusable dialogue-box engine: typewriter reveal, portrait swap (left = Ren, right = Vesper, both hidden for Narrator), dialogue-box repositioning opposite the active speaker, advance-input handling (skip-to-end while typing, advance-to-next-line when idle), `Play(DialogueLine[] lines, Action onComplete)` entry point.
- `GameplayIntroTrigger` — fires the original's 7-line intro sequence (ported verbatim) once, automatically, when the `Game` scene starts.
- A new `Dialogue` action map in `Assets/Input/PlayerControls.inputactions` with one `Advance` action (Z / Enter / Space / Left Click), and a pause mechanism that disables the `Gameplay` map while dialogue is active.
- Placeholder visual treatment for portraits and the dialogue box (flat-colored UI rectangles), consistent with sub-project #2's placeholder convention.

### Explicitly out of scope (deferred to future sub-projects)

- **Real art:** `ren_portrait.png`, `vesper_portrait.png`, `dialogue_box.png` already exist as finished static images in the original's `public/assets/ui/`, but this sub-project uses placeholder rectangles instead — confirmed deliberately, for consistency with sub-project #2's placeholder-everything convention. Swapping in the real images later is a drop-in `Image.sprite` change, not a structural one.
- **Generic trigger-zone / NPC interaction system:** the original has no such abstraction (no collider-based dialogue triggers, no NPC class). Building one now would be speculative; the intro fires automatically on scene start instead. A future sub-project that adds NPCs can build a trigger-zone system that calls the same `DialogueUI.Play()`.
- **Branching/choice-based dialogue:** the original's `DialogueLine`/`DialogueScene` is strictly linear (an ordered array of lines, no choice points, no conditional branching). This sub-project ports that linear model exactly; no choice UI is built.
- **Save/persistence of "has this dialogue played" state:** the original has no such concept (`introPlayed` exists as a field but is never read anywhere — also dead state). Each `Game` scene load re-plays the intro.
- **Automated test infrastructure (Unity Test Framework)** — see Testing section.

## Architecture

New namespace usage consistent with sub-projects #1-#2 (`TheLastAethon.Core`, `TheLastAethon.UI`, `TheLastAethon.Gameplay`):

- **`Assets/Scripts/UI/DialogueUI.cs`** (`TheLastAethon.UI`) — the generic engine. Knows nothing about *why* a dialogue is playing, only how to run whatever lines it's given. Exposes `Play(DialogueLine[] lines, Action onComplete)`.
- **`Assets/Scripts/Gameplay/GameplayIntroTrigger.cs`** (`TheLastAethon.Gameplay`) — on `Start()`, builds the 7-line intro `DialogueLine[]` and calls `DialogueUI.Play(lines, onComplete: () => {})`.

This mirrors the original's actual split (`DialogueScene` = generic engine, `GameScene.triggerIntroDialogue()` = specific content) so a future NPC/trigger-zone sub-project can call `DialogueUI.Play()` without modifying `DialogueUI` itself.

Data flow is one-directional: `GameplayIntroTrigger` → `DialogueUI.Play(lines, onComplete)` → `DialogueUI` runs to completion → `onComplete()` fires → `DialogueUI` deactivates its canvas and restores the `Gameplay` input map.

### Data model

```csharp
namespace TheLastAethon.UI
{
    public enum DialogueSpeaker { Narrator, Ren, Vesper }

    public struct DialogueLine
    {
        public DialogueSpeaker Speaker;
        public string Text;
    }
}
```

A direct port of the original's `{ speaker, text, portrait? }`, minus the separate optional `portrait` field — in the source, `portrait` is never used to show a different portrait than `speaker` implies, so `Speaker` alone determines portrait/name-label visibility: `Narrator` hides both portraits and the name label; `Ren`/`Vesper` show the matching portrait on the left/right respectively and reveal the name label.

### DialogueUI visual structure (placeholder treatment)

A `DialogueCanvas` (Screen Space Overlay, same convention as `HUDCanvas` from sub-project #2) containing:

- **`DimOverlay`** — full-screen `Image`, black, ~30% alpha (matches the original's dim rect).
- **`PortraitLeft` / `PortraitRight`** — placeholder `Image` rectangles, flat-colored with distinct tints for Ren vs. Vesper, anchored bottom-left / bottom-right. Hidden (`gameObject.SetActive(false)`) when not the active speaker's side, or when the current line's speaker is `Narrator`.
- **`DialogueBox`** — placeholder `Image` rectangle behind the text, repositioned left/center/right based on the active speaker — the box sits on the side *opposite* the active portrait (`Ren` active → box right-leaning; `Vesper` active → box left-leaning; `Narrator` → box centered), same rule as the original's `positionDialogueBox()`.
- **`NameLabel`** — `TextMeshProUGUI`, hidden for `Narrator` lines.
- **`DialogueText`** — `TextMeshProUGUI`, word-wrapped, revealed character-by-character via the typewriter effect.
- **`ContinueHint`** — `TextMeshProUGUI` reading "▶ Z / Enter", alpha-blinks once the current line finishes typing; hidden while typing.

**Typewriter:** revealed via a coroutine stepping one character per `WaitForSeconds(0.028f)` tick (28ms/char, matching the original), not a tweening library — satisfies the project's "no third-party tween libs" constraint trivially since no tweening is needed, just string-length stepping. Pressing `Advance` while a line is still typing instantly completes that line's text instead of advancing to the next line; pressing `Advance` once a line has finished typing advances to the next line (or ends the sequence on the last line) — exact behavior match to the original's `advance()`.

### Input wiring and pause behavior

`Assets/Input/PlayerControls.inputactions` gets a new **`Dialogue`** action map alongside the existing `Gameplay`/`Debug` maps, with one action:

- **`Advance`** — bound to `Z`, `Enter`, `Space` (keyboard), and Left Click (mouse) — matches the original's `keydown-Z/ENTER/SPACE` + `pointerdown` listeners.

`DialogueUI.Play()`:
1. Disables the `Gameplay` action map and enables `Dialogue`. `PlayerController.Update()` (sub-project #2) is untouched — its actions simply report no input while the `Gameplay` map is disabled, so movement/run/jump/attack naturally stop. This is the entire pause mechanism; no `Time.timeScale` change, no new flags on `PlayerController` or `PlayerHealth`.
2. Runs the typewriter/advance loop, reading `Dialogue/Advance.WasPressedThisFrame()` each frame.
3. On advancing past the last line: hides `DialogueCanvas`, re-enables `Gameplay`, disables `Dialogue`, and invokes `onComplete`.

### GameplayIntroTrigger

- A new `IntroDialogueTrigger` GameObject in `Assets/Scenes/Game.unity` carrying `GameplayIntroTrigger`.
- `Start()` builds the same 7-line sequence as the original's `triggerIntroDialogue()` verbatim:
  1. Narrator: "Ashenveil Forest. Ten years after the fall of the Aethon Clan."
  2. Ren: "Still here. Still breathing. That's enough for today."
  3. Narrator: "He had been running for as long as he could remember. But lately... something felt different."
  4. Ren: "The patrols are getting closer. They're not just searching anymore."
  5. Vesper: "You're not as hard to find as they say."
  6. Ren: "..."
  7. Vesper: "The last person you'll ever meet. Now stop talking."
- Calls `DialogueUI.Play(lines, onComplete: () => {})` — fires once per scene load, every time. No replay-prevention flag is needed: the original's `introPlayed` field was dead state (set but never read), so there is no real "play once" behavior to port.

## Testing approach

Following the precedent set in sub-projects #1-#2, verification is manual Play Mode end-to-end checking, not Unity Test Framework infrastructure. Exercise: scene load → intro auto-fires → dialogue box shows correct speaker/portrait/box-side per line → typewriter reveals text → `Advance` skips mid-type and advances between lines → `Gameplay` input is inert while dialogue is active (movement/jump/attack keys produce no Player movement) → on the final line's advance, dialogue closes and `Gameplay` input resumes normally.

**Known tooling limitation carried over from sub-project #2:** no key-press-simulation tool exists among `mcp__UnityMCP__*` tools. `Advance` is edge-detected (`WasPressedThisFrame()`), the same category as Jump/Attack in sub-project #2's Task 9 — manually pumping `InputSystem.Update()` does not reliably flip edge-detect state. Verification will use the same accepted workaround: directly invoke the exact downstream call the real input edge-detect would trigger (the same method body `DialogueUI`'s own update loop calls on a successful `Advance` read), then independently confirm consequences (revealed text length, active portrait, box position, canvas active state) via raw scene/asset YAML re-reads rather than live key-press sampling. This is a pre-accepted limitation, not a deviation introduced by this plan — see [[project_unity_mcp_quirks]].

**Console/error check:** `read_console` after every script edit, per the established mandate.

## Global constraints carried over from sub-projects #1-#2

- Unity Editor 6000.5.0f1
- Namespaces: `TheLastAethon.Core`, `TheLastAethon.UI`, `TheLastAethon.Gameplay`
- No third-party tween/animation libraries
- No audio system
- Resizable window, no enforced fullscreen, default 1920×1080
- `mcp__UnityMCP__*` tooling for all Editor-side work; `read_console` error-check after every script edit
- Independently re-verify raw scene/asset YAML after MCP tool calls rather than trusting "success" responses alone (see [[feedback_unity_mcp_trust_but_verify]] and [[project_unity_mcp_quirks]])
