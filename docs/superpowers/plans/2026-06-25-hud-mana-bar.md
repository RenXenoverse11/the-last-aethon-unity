# HUD Frame + Mana Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the bare HP-only HUD with the ornate frame art, add a real Mana stat + bar alongside HP, and a debug key (J) to drain Mana for visual verification.

**Architecture:** A new `HUDFrame` container (under the existing `HUDCanvas`) holds, bottom-to-top: a circular-masked Ren portrait, the HP backdrop+fill pair (existing objects, reused in place), a new mirrored Mana backdrop+fill pair, both text labels, and the frame PNG on top (already transparent over the lanes/medallion in the source art, so no slicing is needed). `PlayerMana.cs` is a minimal stat script mirroring `PlayerHealth.cs`'s shape; `PlayerHUD.cs` gains a parallel Mana block in its existing `Update()`.

**Tech Stack:** Unity 6000.5.0f1, `UnityEngine.UI` (Image/Mask/Canvas), TextMeshPro, Unity Input System, `mcp__UnityMCP__*` MCP tools.

## Global Constraints

- Unity Editor 6000.5.0f1.
- Namespaces: `TheLastAethon.Gameplay`, `TheLastAethon.UI`.
- Use `mcp__UnityMCP__*` tooling for all Editor-side work (scene/GameObject/component/script/asset mutation).
- Run `mcp__UnityMCP__read_console(action="get", types=["error"])` after every script edit — expect zero errors referencing the touched file before moving on.
- Independently re-read the raw scene/asset YAML after every MCP mutation rather than trusting a `success:true` response alone (see [[feedback_unity_mcp_trust_but_verify]]).
- Reuse the existing `HPBarBackground`/`HPBarFill`/`HPLabel` GameObjects in place (reparent + reposition) rather than recreating them, so `PlayerHUD`'s existing serialized references stay valid.
- Fix the known real-defect `m_LocalScale` (`{2.123894, 2.123894, 2.123894}`) on `HPBarBackground` and `HPLabel` back to `{1,1,1}` as part of repositioning them — this is the project's catalogued "real defect" UI-scale quirk ([[project_unity_mcp_quirks]]), distinct from the separately-documented-as-harmless root-canvas `{0,0,0}` scale on `HUDCanvas` itself, which must NOT be touched.
- `git add` exact files only — never `-A` or `.`.

---

## Current scene state (verified by fresh read this session, not assumed from the spec)

- `HUDCanvas` (fileID `410150244`, ScreenSpaceOverlay, sortingOrder 10) has `PlayerHUD` wired: `playerHealth={fileID:455413817}`, `hpFillImage={fileID:1621496832}`, `hpLabel={fileID:1504382806}`.
- `HPBarBackground` (GameObject `1802765258`, RectTransform `1802765259`) is a **dark backdrop only** — plain `Image`, color `{0.15,0.15,0.15,0.8}`, `m_Type:0` (Simple), sprite none. Anchor/pivot `{0,1}`, anchoredPosition `{20,-20}`, sizeDelta `{300,30}`, **`m_LocalScale: {2.123894,2.123894,2.123894}` (defective, must be reset)**. Parent: `HUDCanvas`.
- `HPBarFill` (GameObject `1621496831`, RectTransform `1621496834`) is a **child of `HPBarBackground`**, stretch-anchored to fill it (`anchorMin{0,0}`, `anchorMax{1,1}`, `sizeDelta{0,0}`, scale `~1`). Its `Image` (fileID `1621496832`) is `m_Type:3` (Filled), `m_FillMethod:0` (Horizontal), color `{0.7529412,0.2235294,0.1686275,1}` — **this is the object `PlayerHUD.hpFillImage` actually targets**, confirmed by exact color match with `PlayerHUD.cs`'s `HighColor` constant.
  - **This corrects the design spec's hierarchy diagram**, which assumed `HPBarBackground` itself was the reused fill image — it isn't; it's the backdrop, and its child is the fill. See Self-Review Notes below.
- `HPLabel` (GameObject `1504382805`, RectTransform `1504382808`) — TMP text "100 / 100", fontSize 20, white, `m_HorizontalAlignment:1` (Left), font asset guid `8f586378b4e144a9851e7b34d9b748ee` (fileID `11400000`, type 2). Anchor/pivot `{0,1}`, anchoredPosition `{22,-29}`, sizeDelta `{300,30}`, **same `2.123894` scale defect**. Parent: `HUDCanvas`.
- `Player` GameObject (fileID `455413814`) already holds `PlayerHealth` (`455413817`), `PlayerController` (`455413816`), `DebugDamageTrigger` (`455413815`, `inputActions={fileID:-944628639613478452, guid:295a9007186f8ac4581661722fc28cd0}`, `target={fileID:455413817}`). `PlayerMana` and `DebugManaTrigger` will join this same GameObject.
- `Assets/Art/UI/hud_player_frame.png` (guid `a7254bc8b77778048b8dc43c3e9a4f76`) is already imported as a Sprite, but in **Multiple** mode with 3 auto-sliced fragments (`hud_player_frame_0` = the real 1415×290 body at offset `(35,0)`; `_1`/`_2` are two tiny stray alpha islands near the image's right edge, not anything we want). This plan reimports it as a **Single**, full-bounds, top-left-pivot sprite to avoid cropping/offset math.
- `Assets/Art/UI/ren_portrait.png` (guid `e6ba41c84430f85efcccefe28b1215a8`) is 1132×1390px, already Single-sprite mode (fileID `21300000`), already used elsewhere in the project (`PortraitLeft` in the dialogue UI) with `m_PreserveAspect:0` — i.e., this project's established convention is to stretch portraits to fill their box, not letterbox them. `PortraitImage` below follows the same convention.

### Measured geometry, converted to on-screen pixels at the spec's ~480px display width (scale factor `k = 480/1536 = 0.3125` exactly)

| Element | anchoredPosition (top-left pivot) | sizeDelta |
|---|---|---|
| `FrameImage` (whole frame) | `{0, 0}` | `{480, 93.4375}` |
| HP lane box | `{103.125, -31.875}` | `{323.4375, 17.8125}` |
| Mana lane box | `{103.125, -50.9375}` | `{323.4375, 17.5}` |
| Medallion/portrait box | `{36.875, -23.4375}` | `{56.25, 49.375}` |

All anchored at `anchorMin{0,1}`/`anchorMax{0,1}`/`pivot{0,1}` within `HUDFrame`, which itself sits at `anchoredPosition{20,-20}` within `HUDCanvas` (the exact position `HPBarBackground` occupies today). Per the spec's own caveat, these are starting values, not a pixel-perfect contract — Task 9 covers final visual confirmation.

---

### Task 1: `PlayerMana.cs` + attach to Player

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerMana.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `TheLastAethon.Gameplay.PlayerMana` — `MonoBehaviour`, `[SerializeField] int maxMana = 100`, `public int Mana { get; private set; }`, `public int MaxMana => maxMana`, `public void SpendMana(int amount)` (clamps at 0).
- Consumes: nothing new.

- [ ] **Step 1: Create the script**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="PlayerMana"`, `path="Assets/Scripts/Gameplay"`, `contents`:

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

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `PlayerMana.cs`.

- [ ] **Step 3: Attach to the Player GameObject**

```
mcp__UnityMCP__manage_gameobject(
  action="modify", target="Player",
  components_to_add=["TheLastAethon.Gameplay.PlayerMana"]
)
```

Note the new component's instance ID as `<playerManaId>` (if the call's response doesn't include it, re-read `Player`'s raw YAML and note the new `MonoBehaviour` block's fileID).

- [ ] **Step 4: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Assets/Scenes/Game.unity`, find `Player`'s component list — confirm a new `MonoBehaviour` block referencing `PlayerMana`'s script guid, with serialized field `maxMana: 100`.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Gameplay/PlayerMana.cs" "Assets/Scenes/Game.unity"
git commit -m "Add PlayerMana stat script and attach to Player"
```

---

### Task 2: `HUDFrame` container; reparent HP elements; fix localScale defect

**Files:**
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `HUDFrame` (RectTransform only) under `HUDCanvas`, at the screen position the HP bar currently occupies. All later HUD tasks parent under this.
- Consumes: `HUDCanvas` (fileID `410150244`), `HPBarBackground` (`1802765258`/`1802765259`), `HPLabel` (`1504382805`/`1504382808`) — existing objects, reparented and repositioned, not recreated.

- [ ] **Step 1: Create HUDFrame**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="HUDFrame",
  parent="HUDCanvas",
  component_properties={
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 20, "y": -20}, "sizeDelta": {"x": 480, "y": 93.4375}}
  }
)
```

Note the returned instance ID as `<hudFrameId>`.

- [ ] **Step 2: Reparent + reposition + fix scale on HPBarBackground**

```
mcp__UnityMCP__manage_gameobject(
  action="modify", target="HPBarBackground", parent="HUDFrame",
  component_properties={
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 103.125, "y": -31.875}, "sizeDelta": {"x": 323.4375, "y": 17.8125}, "localScale": {"x": 1, "y": 1, "z": 1}}
  }
)
```

- [ ] **Step 3: Reparent + reposition + fix scale on HPLabel**

```
mcp__UnityMCP__manage_gameobject(
  action="modify", target="HPLabel", parent="HUDFrame",
  component_properties={
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 103.125, "y": -31.875}, "sizeDelta": {"x": 323.4375, "y": 17.8125}, "localScale": {"x": 1, "y": 1, "z": 1}}
  }
)
```

- [ ] **Step 4: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Assets/Scenes/Game.unity`. Confirm: `HPBarBackground`'s RectTransform (`1802765259`) now has `m_Father` pointing at `<hudFrameId>`'s fileID, `m_LocalScale: {x: 1, y: 1, z: 1}` (not `2.123894`), and the new anchoredPosition/sizeDelta. Same checks for `HPLabel`'s RectTransform (`1504382808`). Confirm `HPBarFill` (`1621496834`) is untouched — still `anchorMin{0,0}`/`anchorMax{1,1}`/`sizeDelta{0,0}` — so it automatically tracks its parent's new size with no separate edit. Confirm `HUDCanvas`'s own RectTransform (`410150249`) still shows the harmless `{0,0,0}` scale — do not "fix" that one.

- [ ] **Step 5: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scenes/Game.unity"
git commit -m "Create HUDFrame container; reparent HP bar/label; fix localScale defect"
```

---

### Task 3: Circular-masked Ren portrait

**Files:**
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `PortraitMask` (`Image`+`Mask`) under `HUDFrame`; `PortraitImage` (`Image`, `ren_portrait.png`) as its stretched child.
- Consumes: `HUDFrame` (`<hudFrameId>` from Task 2), `ren_portrait.png` (guid `e6ba41c84430f85efcccefe28b1215a8`, fileID `21300000`, existing).

- [ ] **Step 1: Create PortraitMask**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="PortraitMask",
  parent="HUDFrame",
  components_to_add=["Image", "Mask"],
  component_properties={
    "Image": {"color": {"r": 1, "g": 1, "b": 1, "a": 1}},
    "Mask": {"showMaskGraphic": false},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 36.875, "y": -23.4375}, "sizeDelta": {"x": 56.25, "y": 49.375}}
  }
)
```

Note the instance ID as `<portraitMaskId>`.

- [ ] **Step 2: Assign a circular sprite to the mask**

`PortraitMask` needs a circular (here, oval, since its box is wider than tall) sprite shape for `Mask` to clip to a circle instead of its default rectangle. Unity ships a built-in filled-circle sprite (`UI/Skin/Knob.psd`, used by Slider/Scrollbar handles) with no project import needed, but it has no asset GUID reachable through normal property setters — assign it via `execute_code`:

```csharp
var maskGO = GameObject.Find("HUDFrame/PortraitMask");
var img = maskGO.GetComponent<UnityEngine.UI.Image>();
img.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
img.type = UnityEngine.UI.Image.Type.Simple;
UnityEditor.EditorUtility.SetDirty(maskGO);
```

- [ ] **Step 3: Create PortraitImage**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="PortraitImage",
  parent="PortraitMask",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 1, "g": 1, "b": 1, "a": 1}, "preserveAspect": false},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}
  }
)
```

- [ ] **Step 4: Assign ren_portrait.png**

```
mcp__UnityMCP__manage_components(action="set_property", target="PortraitImage", search_method="by_name", component_type="UnityEngine.UI.Image", property="sprite", value={"fileID": 21300000, "guid": "e6ba41c84430f85efcccefe28b1215a8", "type": 3})
```

If the friendly name `sprite` is rejected, retry with property name `m_Sprite` (this project's MCP bridge has needed the internal serialized name for some components before — [[project_unity_mcp_quirks]]).

- [ ] **Step 5: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read the scene. Confirm `PortraitMask` has a `Mask` component (`m_ShowMaskGraphic: 0`) and its `Image.m_Sprite` is non-`{fileID: 0}` (the built-in Knob sprite). Confirm `PortraitImage`'s `Image.m_Sprite` resolves to guid `e6ba41c84430f85efcccefe28b1215a8`, fileID `21300000`.

- [ ] **Step 6: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Scenes/Game.unity"
git commit -m "Add circular-masked Ren portrait to HUDFrame"
```

---

### Task 4: Mana backdrop + fill bar

**Files:**
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `ManaBarBackground` (`Image`, dark backdrop) under `HUDFrame`; `ManaBarFill` (`Image`, Filled/Horizontal, color `#3498DB`) as its stretched child — mirrors `HPBarBackground`/`HPBarFill` exactly. Task 7 wires `ManaBarFill`'s `Image` as `PlayerHUD.manaFillImage`.
- Consumes: `HUDFrame` (`<hudFrameId>`).

- [ ] **Step 1: Create ManaBarBackground**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="ManaBarBackground",
  parent="HUDFrame",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0.15, "g": 0.15, "b": 0.15, "a": 0.8}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 103.125, "y": -50.9375}, "sizeDelta": {"x": 323.4375, "y": 17.5}}
  }
)
```

- [ ] **Step 2: Create ManaBarFill**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="ManaBarFill",
  parent="ManaBarBackground",
  components_to_add=["Image"],
  component_properties={
    "Image": {"type": 3, "fillMethod": 0, "fillOrigin": 0, "fillAmount": 1, "color": {"r": 0.2039216, "g": 0.5960784, "b": 0.8588235, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}
  }
)
```

Note the instance ID as `<manaFillId>`.

- [ ] **Step 3: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read the scene. Confirm `ManaBarBackground`'s position/size match Step 1, `ManaBarFill` is stretch-anchored (`{0,0}`-`{1,1}`, `sizeDelta{0,0}`) with `m_Type:3`, `m_FillMethod:0`, `m_FillAmount:1`, color `{0.2039216,0.5960784,0.8588235,1}`.

- [ ] **Step 4: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scenes/Game.unity"
git commit -m "Add Mana backdrop and fill bar to HUDFrame"
```

---

### Task 5: ManaLabel

**Files:**
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `ManaLabel` (`TextMeshProUGUI`) under `HUDFrame`, styled identically to `HPLabel`. Task 7 wires this as `PlayerHUD.manaLabel`.
- Consumes: `HUDFrame` (`<hudFrameId>`), `HPLabel`'s existing style values (font asset guid `8f586378b4e144a9851e7b34d9b748ee`, fontSize 20, white) as the values to replicate.

- [ ] **Step 1: Create ManaLabel**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="ManaLabel",
  parent="HUDFrame",
  components_to_add=["TextMeshProUGUI"],
  component_properties={
    "TextMeshProUGUI": {"text": "100 / 100", "fontSize": 20, "color": {"r": 1, "g": 1, "b": 1, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 103.125, "y": -50.9375}, "sizeDelta": {"x": 323.4375, "y": 17.5}}
  }
)
```

- [ ] **Step 2: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read the scene. Confirm `ManaLabel`'s `m_text: "100 / 100"`, `m_fontSize: 20`, position/size match Step 1. Confirm `m_fontAsset` resolved to guid `8f586378b4e144a9851e7b34d9b748ee` (TMP's default for new text objects in this project) — if it differs from `HPLabel`'s, set it explicitly: `mcp__UnityMCP__manage_components(action="set_property", target="ManaLabel", search_method="by_name", component_type="TMPro.TextMeshProUGUI", property="m_fontAsset", value={"fileID": 11400000, "guid": "8f586378b4e144a9851e7b34d9b748ee", "type": 2})`.

- [ ] **Step 3: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scenes/Game.unity"
git commit -m "Add ManaLabel to HUDFrame"
```

---

### Task 6: Re-import frame art as a single sprite; add FrameImage on top

**Files:**
- Modify: `Assets/Art/UI/hud_player_frame.png.meta` (via reimport)
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `FrameImage` (`Image`, sprite = full-bounds `hud_player_frame.png`) as `HUDFrame`'s last/topmost sibling — renders over everything added in Tasks 2-5.
- Consumes: `HUDFrame` (`<hudFrameId>`), `hud_player_frame.png` (guid `a7254bc8b77778048b8dc43c3e9a4f76`) — currently Multiple-sprite-mode with 3 auto-sliced fragments; this task reconfigures it to Single mode.

- [ ] **Step 1: Reimport as a single, full-bounds, top-left-pivot sprite**

Check `mcp__UnityMCP__manage_texture`'s schema first for sprite-import-mode/pivot parameters; if it exposes them directly, use it. Otherwise use `execute_code`:

```csharp
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Art/UI/hud_player_frame.png");
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
importer.spriteAlignment = (int)UnityEngine.SpriteAlignment.Custom;
importer.spritePivot = new Vector2(0f, 1f);
importer.SaveAndReimport();
```

- [ ] **Step 2: Independently re-verify the reimport**

Re-read `Assets/Art/UI/hud_player_frame.png.meta`. Confirm `spriteMode: 1`, `spritePivot: {x: 0, y: 1}`, and `spriteSheet.sprites` now has exactly one entry covering the full `1536x299` rect (not the old 3-fragment list).

- [ ] **Step 3: Create FrameImage**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="FrameImage",
  parent="HUDFrame",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 1, "g": 1, "b": 1, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 0, "y": 0}, "sizeDelta": {"x": 480, "y": 93.4375}}
  }
)
```

- [ ] **Step 4: Assign the reimported sprite**

```
mcp__UnityMCP__manage_components(action="set_property", target="FrameImage", search_method="by_name", component_type="UnityEngine.UI.Image", property="sprite", value={"fileID": 21300000, "guid": "a7254bc8b77778048b8dc43c3e9a4f76", "type": 3})
```

(fileID `21300000` is the fixed ID Unity assigns to a Single-mode texture's one Sprite sub-asset — confirmed by analogy with `ren_portrait.png`, which uses the same fileID for the same reason.) If the friendly name `sprite` is rejected, retry with `m_Sprite`.

- [ ] **Step 5: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read the scene. Confirm `FrameImage`'s `Image.m_Sprite` resolves to guid `a7254bc8b77778048b8dc43c3e9a4f76`, fileID `21300000`. Confirm `FrameImage` is the **last** entry in `HUDFrame`'s `m_Children` list (topmost sibling = renders on top of the portrait/bars/labels added in Tasks 3-5).

- [ ] **Step 6: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Art/UI/hud_player_frame.png.meta" "Assets/Scenes/Game.unity"
git commit -m "Reimport frame art as single sprite; add FrameImage on top of HUDFrame"
```

---

### Task 7: Extend PlayerHUD.cs with Mana; wire references

**Files:**
- Modify: `Assets/Scripts/UI/PlayerHUD.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `PlayerHUD` gains `[SerializeField] PlayerMana playerMana`, `[SerializeField] Image manaFillImage`, `[SerializeField] TextMeshProUGUI manaLabel`. `Update()` drives the Mana fill/label using a fixed color `#3498DB` (`new Color(0.2039216f, 0.5960784f, 0.8588235f)`), no tier shifting (unlike HP's three-tier color shift).
- Consumes: `PlayerMana` (Task 1, `<playerManaId>`), `ManaBarFill`'s `Image` (Task 4, `<manaFillId>`), `ManaLabel`'s `TextMeshProUGUI` (Task 5).

- [ ] **Step 1: Replace PlayerHUD.cs's contents**

Call `mcp__UnityMCP__manage_script` (`action="update"` or `script_apply_edits`, whichever the live tool exposes) with the full new contents:

```csharp
using TheLastAethon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastAethon.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private TextMeshProUGUI hpLabel;
        [SerializeField] private PlayerMana playerMana;
        [SerializeField] private Image manaFillImage;
        [SerializeField] private TextMeshProUGUI manaLabel;

        private static readonly Color HighColor = new Color(0.7529412f, 0.2235294f, 0.1686275f);
        private static readonly Color MidColor = new Color(0.9019608f, 0.4941176f, 0.1333333f);
        private static readonly Color LowColor = new Color(0.9058824f, 0.2980392f, 0.2352941f);
        private static readonly Color ManaColor = new Color(0.2039216f, 0.5960784f, 0.8588235f);

        private void Update()
        {
            float hpPct = (float)playerHealth.Hp / playerHealth.MaxHp;
            hpFillImage.fillAmount = hpPct;
            hpFillImage.color = hpPct > 0.5f ? HighColor : hpPct > 0.25f ? MidColor : LowColor;
            hpLabel.text = $"{playerHealth.Hp} / {playerHealth.MaxHp}";

            float manaPct = (float)playerMana.Mana / playerMana.MaxMana;
            manaFillImage.fillAmount = manaPct;
            manaFillImage.color = ManaColor;
            manaLabel.text = $"{playerMana.Mana} / {playerMana.MaxMana}";
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `PlayerHUD.cs`.

- [ ] **Step 3: Wire the three new fields**

```
mcp__UnityMCP__manage_components(action="set_property", target="HUDCanvas", search_method="by_name", component_type="TheLastAethon.UI.PlayerHUD", property="playerMana", value="<playerManaId>")
mcp__UnityMCP__manage_components(action="set_property", target="HUDCanvas", search_method="by_name", component_type="TheLastAethon.UI.PlayerHUD", property="manaFillImage", value="<manaFillId>")
mcp__UnityMCP__manage_components(action="set_property", target="HUDCanvas", search_method="by_name", component_type="TheLastAethon.UI.PlayerHUD", property="manaLabel", value="<manaLabelId>")
```

- [ ] **Step 4: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read the scene. Confirm `PlayerHUD`'s `MonoBehaviour` block now lists all six fields (`playerHealth`, `hpFillImage`, `hpLabel`, `playerMana`, `manaFillImage`, `manaLabel`) resolved to non-zero fileIDs.

- [ ] **Step 5: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/UI/PlayerHUD.cs" "Assets/Scenes/Game.unity"
git commit -m "Extend PlayerHUD with Mana fill/label logic"
```

---

### Task 8: DebugManaTrigger.cs + ManaTest/J binding

**Files:**
- Create: `Assets/Scripts/Gameplay/DebugManaTrigger.cs`
- Modify: `Assets/Input/PlayerControls.inputactions`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `TheLastAethon.Gameplay.DebugManaTrigger` — mirrors `DebugDamageTrigger.cs` exactly; on the `Player` GameObject, calls `target.SpendMana(debugDrainAmount)` when the new `ManaTest` action (bound to J) fires.
- Consumes: `PlayerMana` (Task 1, `<playerManaId>`), the existing `Debug` action map and the `inputActions` asset reference already used by `DebugDamageTrigger` (fileID `-944628639613478452`, guid `295a9007186f8ac4581661722fc28cd0`).

- [ ] **Step 1: Add the ManaTest action + J binding**

Edit `Assets/Input/PlayerControls.inputactions`. In the `Debug` map, add a new action after `DamageTest`:

```json
                {
                    "name": "ManaTest",
                    "type": "Button",
                    "id": "8f1b6a2e-0003-4000-8000-000000000004",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
```

and a new binding after the existing `DamageTest`/H binding:

```json
                { "name": "", "id": "8f1b6a2e-0003-4000-8000-000000000005", "path": "<Keyboard>/j", "interactions": "", "processors": "", "groups": "", "action": "ManaTest", "isComposite": false, "isPartOfComposite": false }
```

- [ ] **Step 2: Reimport and check console**

```
mcp__UnityMCP__refresh_unity()
mcp__UnityMCP__read_console(action="get", types=["error"])
```

Expect Unity to reimport `PlayerControls.inputactions` with no errors.

- [ ] **Step 3: Create DebugManaTrigger.cs**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="DebugManaTrigger"`, `path="Assets/Scripts/Gameplay"`, `contents`:

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

- [ ] **Step 4: Verify it compiles**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `DebugManaTrigger.cs`.

- [ ] **Step 5: Attach to Player and wire its fields**

```
mcp__UnityMCP__manage_gameobject(action="modify", target="Player", components_to_add=["TheLastAethon.Gameplay.DebugManaTrigger"])
mcp__UnityMCP__manage_components(action="set_property", target="Player", search_method="by_name", component_type="TheLastAethon.Gameplay.DebugManaTrigger", property="inputActions", value={"fileID": -944628639613478452, "guid": "295a9007186f8ac4581661722fc28cd0", "type": 3})
mcp__UnityMCP__manage_components(action="set_property", target="Player", search_method="by_name", component_type="TheLastAethon.Gameplay.DebugManaTrigger", property="target", value="<playerManaId>")
mcp__UnityMCP__manage_components(action="set_property", target="Player", search_method="by_name", component_type="TheLastAethon.Gameplay.DebugManaTrigger", property="debugDrainAmount", value=10)
```

- [ ] **Step 6: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Player`'s raw YAML. Confirm the new `DebugManaTrigger` `MonoBehaviour` block has `inputActions`, `target`, and `debugDrainAmount: 10` all resolved (non-zero fileIDs for the references).

- [ ] **Step 7: Check console**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

- [ ] **Step 8: Commit**

```bash
git add "Assets/Scripts/Gameplay/DebugManaTrigger.cs" "Assets/Input/PlayerControls.inputactions" "Assets/Scenes/Game.unity"
git commit -m "Add DebugManaTrigger and ManaTest/J input binding"
```

---

### Task 9: End-to-end Play Mode verification

**Files:** none (verification only).

**Interfaces:**
- Consumes: every object/script from Tasks 1-8.

- [ ] **Step 1: Enter Play mode**

```
mcp__UnityMCP__manage_editor(action="play")
```

- [ ] **Step 2: Check console immediately**

`mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none on scene start.

- [ ] **Step 3: Confirm initial state**

Via `execute_code`, read and confirm: `PlayerHealth.Hp == 100`, `PlayerMana.Mana == 100`, `HPBarFill.fillAmount == 1`, `ManaBarFill.fillAmount == 1`, `HPLabel.text == "100 / 100"`, `ManaLabel.text == "100 / 100"`.

- [ ] **Step 4: Exercise DamageTest (H) and ManaTest (J)**

This project's Input System only registers simulated key presses while the Game view is treated as focused ([[project_unity_mcp_quirks]]). Via `execute_code`, for each key in turn:

```csharp
UnityEditorInternal.InternalEditorUtility.OnGameViewFocus(true);
InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.H)); // then Key.J for the second pass
InputSystem.Update();
```

- [ ] **Step 5: Confirm independent HP/Mana behavior**

Re-read `PlayerHealth.Hp`/`PlayerMana.Mana` and both fill/label objects after each keypress. Confirm: HP dropped by 10 (color shifts at the existing `#E67E22`/`#E74C3C` thresholds, unchanged from before this feature) and, independently, Mana dropped by 10 (`ManaBarFill.color` stays fixed at `#3498DB`, no shifting). Confirm draining one stat does not affect the other.

- [ ] **Step 6: Exit Play mode**

```
mcp__UnityMCP__manage_editor(action="stop")
```

- [ ] **Step 7: Flag visual alignment for manual review**

This task can confirm the fill/label logic works and that objects are positioned at the measured coordinates, but cannot judge pixel-perfect alignment against the frame art by eye. Tell the user the feature is wired and positioned from the measured geometry, and that any final lane/medallion alignment nudge is a quick manual `anchoredPosition`/`sizeDelta` tweak in the Editor — consistent with how camera-Y tuning was handled earlier in this project ([[feedback_manual_inspector_tweaks]]).

No commit for this task (verification only).

---

## Self-Review Notes

- **Spec coverage:** `hud_player_frame.png` imported as a single full-bounds sprite (Task 6) ✓; `PlayerMana.cs` minimal current/max stat (Task 1) ✓; `PlayerHUD.cs` extended with Mana fill/label, fixed color, no tiers (Task 7) ✓; `DebugManaTrigger.cs` + `ManaTest`/J binding (Task 8) ✓; HUD restructured under `HUDFrame` — frame art on top, HP fill (reused) + Mana fill (new) + masked Ren portrait underneath (Tasks 2-6) ✓; circular masking for the portrait (Task 3) ✓; end-to-end manual Play Mode verification, no automated tests (Task 9) ✓, matching the spec's stated testing approach.
- **Placeholder scan:** every numeric constant (positions, sizes, colors, scale, font size, fileIDs/guids) is an exact value derived from either the spec's measured geometry (scaled by the exact fraction `0.3125`) or a fresh read of the live scene/asset files this session — none are TBD.
- **Type/name consistency check:** `PlayerMana.SpendMana(int amount)` (Task 1) matches the call in `DebugManaTrigger.cs` (Task 8) exactly, mirroring `PlayerHealth.TakeDamage(int)`/`DebugDamageTrigger.cs`'s existing pattern. `PlayerHUD`'s new field names (`playerMana`, `manaFillImage`, `manaLabel`) match the `set_property` calls in Task 7 Step 3 exactly. `ManaColor`'s value (`0.2039216, 0.5960784, 0.8588235`) is identical between `PlayerHUD.cs` (Task 7) and `ManaBarFill`'s initial `Image.color` (Task 4) — the bar's edit-time color and its runtime-set color now match, so there's no flash-of-wrong-color before `Update()` first runs.
- **Deviation from the design spec, called out explicitly:** the spec's hierarchy diagram described reusing `HPBarBackground` itself as the fill image (`HPFillImage`). A fresh scene read this session (not available when the spec was written) showed this is wrong: `HPBarBackground` is a plain dark backdrop, and a separate child, `HPBarFill`, holds the `Image` that `PlayerHUD.hpFillImage` actually targets (confirmed by exact color match against `PlayerHUD.cs`'s `HighColor` constant). This plan reuses the existing backdrop+fill *pair* together (Task 2 repositions only the parent; the child's existing stretch-anchors carry it along automatically) and mirrors that same two-layer pattern for the new Mana bar (`ManaBarBackground`+`ManaBarFill`, Task 4) instead of the spec's flatter single `ManaFillImage`. This keeps Mana visually consistent with HP — a dark backdrop shows through the "empty" portion of either bar, rather than Mana revealing the transparent game world behind the frame art while HP does not. This fills in a level of detail the spec's architecture section didn't address (it covered the layering approach, not each bar's internal structure), not a contradiction of an explicit spec decision.
- **Tooling-risk note:** `manage_gameobject(action="create"/"modify")` with nested `component_properties` (including `"RectTransform": {...}` for anchors/pivot/anchoredPosition/sizeDelta/localScale, and stretch via `anchorMin`/`anchorMax`/`offsetMin`/`offsetMax`) is the exact pattern proven working in this project's own `docs/superpowers/plans/2026-06-19-unity-player-controller.md` (Tasks 6 and 9) — high confidence. `manage_texture`'s exact parameter names for sprite-import-mode/pivot were not confirmed this session (the texture was already imported, just in the wrong mode), so Task 6 Step 1 gives a verified-correct `execute_code`/`TextureImporter` fallback as the primary instruction. The built-in `UI/Skin/Knob.psd` circle sprite (Task 3) is a well-known Unity built-in resource path used by the engine's own Slider/Scrollbar prefabs, but assigning it was not live-tested in this session — if `GetBuiltinExtraResource` returns null, re-check the resource path against the installed Editor version, or substitute any other built-in/project circle sprite. Across all tasks, the fixed, non-negotiable part is the target end-state (exact component, property, and value) given in each step's verification — adjust call syntax as needed and re-run that step's verification before moving on, per this project's established trust-but-verify discipline.
