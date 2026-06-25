# HUD Frame + Mana Bar — Design Spec

Follow-up to sub-project #2 (`docs/superpowers/specs/2026-06-19-unity-player-controller-design.md`), which shipped an HP-only HUD with no frame art. The user supplied an ornate fantasy frame image and asked for a Mana bar to sit alongside HP. Mana does not exist in the Phaser source, the current Unity code, or the GDD — this is new design work, not a port (see [[project_last_aethon_unity_redesign]]).

## Source assets

- `Assets/Art/UI/hud_player_frame.png` — 1536×299px, `Format32bppArgb` with genuine alpha transparency (confirmed via pixel sampling; corner/center pixels `A=0`). A double-track ornate bar: circular medallion on the left, two stacked horizontal lanes extending right, tapered arrowhead end-cap.
- `Assets/Art/UI/ren_portrait.png` — existing dialogue-system portrait asset, reused for the medallion.

### Measured geometry (source-pixel space, via alpha-channel scanning)

- Frame content spans roughly y=88 (top border) to y=234 (bottom border) of the 299px-tall image.
- Two lane interiors, stacked: **top/HP lane** y102-159 (57px tall), **bottom/Mana lane** y163-219 (56px tall), separated by a 4px divider.
- Both lanes share the same horizontal span: interior runs from approximately **x330 to x1365** (confirmed consistent at multiple y-samples within each lane, ±10px), bounded on the left by the medallion and on the right by a tapered arrowhead end-cap.
- The medallion is **one large oval portrait window spanning the full height of both lanes combined**, not a separate window per lane — hollow (transparent) interior centered at approximately **(208, 154)**, roughly 180px wide × 158px tall (slightly oval, not a perfect circle).

These measurements are a starting point for implementation, not a pixel-perfect contract — final alignment is verified visually in the Unity Editor against the actual imported sprite.

## Architecture

**Approach: layered UI Images, single frame texture drawn on top, no slicing.** The frame PNG's lanes and medallion window are already transparent in the source art, so fill bars and the portrait simply show through when layered underneath; the frame's opaque line-art/spikes render on top at the edges. This avoids cutting the source art into multiple sprites (which would be more flexible for dynamic resizing, but isn't needed — nothing requires the bar to resize today) and reuses the project's existing flat-`Image`-with-`fillAmount` pattern from the HP bar.

### GameObject hierarchy (`Assets/Scenes/Game.unity`, under `HUDCanvas`)

Replaces the current bare `HPBarBackground`/`HPLabel` pair:

```
HUDCanvas (existing, has PlayerHUD)
└── HUDFrame (new empty RectTransform, top-left anchored — same position the HP bar occupies today, displayed at ~480px wide / scale ≈0.31 of the 1536px source)
    ├── PortraitMask (Image + Mask component, circular sprite)
    │   └── PortraitImage (ren_portrait.png, sized to fill the mask)
    ├── HPFillImage (the existing HPBarBackground GameObject, reused in place — repositioned/resized to the top lane bounds, NOT recreated, so PlayerHUD's existing serialized reference stays valid)
    ├── ManaFillImage (new Image, fillMethod=Horizontal, sized to the bottom lane bounds)
    ├── HPLabel (existing TMP text GameObject, reused — repositioned over the top lane)
    ├── ManaLabel (new TMP text, same style as HPLabel, positioned over the bottom lane)
    └── FrameImage (Image, sprite = hud_player_frame.png — highest sibling index, renders on top of all of the above)
```

The portrait needs an actual circular `Mask` because `ren_portrait.png` is a plain rectangular character image — without masking, its corners would show through the medallion's transparent gaps. The fill bars need no masking; the frame art's lane interiors are already transparent there.

### `PlayerMana.cs` (new, `TheLastAethon.Gameplay`)

Minimal stat-only script — mirrors `PlayerHealth.cs`'s `maxHp`/`Hp` shape, but deliberately excludes `PlayerHealth`'s hurt/knockback/regen logic, since nothing in the game currently drains mana and that logic would be dead code (YAGNI):

```csharp
using UnityEngine;

namespace TheLastAethon.Gameplay
{
    public class PlayerMana : MonoBehaviour
    {
        [SerializeField] private int maxMana = 100;

        public int Mana { get; private set; }
        public int MaxMana => maxMana;

        private void Awake() => Mana = maxMana;

        public void SpendMana(int amount)
        {
            Mana = Mathf.Max(0, Mana - amount);
        }
    }
}
```

`SpendMana` clamps at 0 and exists now only so `DebugManaTrigger` (below) has something to call — no other caller exists yet, since no gameplay system consumes mana in this sub-project.

### `PlayerHUD.cs` (modified, `TheLastAethon.UI`)

Adds a parallel set of fields/logic for Mana alongside the existing HP block:

- `[SerializeField] private PlayerMana playerMana;`
- `[SerializeField] private Image manaFillImage;`
- `[SerializeField] private TextMeshProUGUI manaLabel;`
- In `Update()`: compute `manaPct = (float)playerMana.Mana / playerMana.MaxMana`, set `manaFillImage.fillAmount = manaPct`, set `manaLabel.text = $"{playerMana.Mana} / {playerMana.MaxMana}"`.
- Mana fill uses **one fixed color**, `#3498DB` (blue — the conventional mana color, visually distinct from HP's red/orange palette), with no low/mid/high tier shifting — there's no "danger" semantic for low mana the way there is for low HP.

`PortraitImage` is static at runtime (Ren's portrait doesn't change), so it needs no script wiring — its sprite is assigned directly in the Editor.

## Verifying it works: `DebugManaTrigger.cs` (new, `TheLastAethon.Gameplay`)

Mirrors the existing `DebugDamageTrigger.cs` pattern exactly, so the Mana fill bar's `fillAmount` logic can be visually verified at less than 100% (otherwise nothing in this sub-project ever changes `Mana`, and the bar would always render full):

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastAethon.Gameplay
{
    public class DebugManaTrigger : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerMana target;
        [SerializeField] private int debugDrainAmount = 10;

        private InputAction manaTestAction;

        private void Awake()
        {
            manaTestAction = inputActions.FindActionMap("Debug").FindAction("ManaTest");
        }

        private void OnEnable()
        {
            manaTestAction.Enable();
        }

        private void OnDisable()
        {
            manaTestAction.Disable();
        }

        private void Update()
        {
            if (manaTestAction.WasPressedThisFrame())
            {
                target.SpendMana(debugDrainAmount);
            }
        }
    }
}
```

### Input

New `ManaTest` action in the existing `Debug` action map (`Assets/Input/PlayerControls.inputactions`, alongside `DamageTest`/H): bound to **J** (adjacent key, unused elsewhere), no gamepad binding — same precedent as `DamageTest`.

## Scope

### In scope

- `hud_player_frame.png` imported as a Unity Sprite
- `PlayerMana.cs` — minimal current/max stat, starts full
- `PlayerHUD.cs` extended with Mana fill/label logic
- `DebugManaTrigger.cs` + `ManaTest` input binding (J key)
- HUD restructured under a new `HUDFrame` container: frame art on top, HP fill (reused) + new Mana fill + masked Ren portrait underneath
- Circular masking for the portrait

### Explicitly out of scope

- Mana regen or drain tied to actual gameplay (spells, abilities) — nothing in the game consumes mana yet; this sub-project only makes the stat and its display real, not its gameplay usage
- Slicing the frame art into independently-resizable pieces — deferred unless the HUD later needs to support dynamic resizing
- Repositioning the HUD elsewhere on screen — stays top-left, matching the current HP bar's position

## Global constraints carried over

- Unity Editor 6000.5.0f1
- Namespaces: `TheLastAethon.Gameplay`, `TheLastAethon.UI`
- `mcp__UnityMCP__*` tooling for all Editor-side work; `read_console` error-check after every script edit
- Independently re-verify raw scene/asset YAML after MCP tool calls rather than trusting "success" responses alone (see [[feedback_unity_mcp_trust_but_verify]] and [[project_unity_mcp_quirks]])

## Testing approach

Same precedent as sub-project #2: manual Play Mode verification, no automated test infrastructure. Check: frame art renders correctly aligned over both fill bars and the portrait at the intended on-screen size; HP bar still behaves exactly as before (H key drains HP, bar/label update); J key drains Mana and the Mana bar/label update in parallel; portrait shows cleanly through the medallion with no square corners visible past the mask.
