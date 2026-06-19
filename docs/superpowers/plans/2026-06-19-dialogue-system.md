# Dialogue System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the Phaser `DialogueScene` typewriter/portrait dialogue overlay and its only real content (the 7-line intro sequence from `triggerIntroDialogue()`) into Unity as a reusable `DialogueUI` engine plus a `GameplayIntroTrigger` that auto-fires the intro on scene load.

**Architecture:** `TheLastAethon.UI.DialogueUI` is a generic, content-agnostic engine (`Play(DialogueLine[] lines, Action onComplete)`) driving a placeholder Canvas hierarchy (dim overlay, two portrait rectangles, a repositioning dialogue box, name/body/continue-hint text). `TheLastAethon.Gameplay.GameplayIntroTrigger` is a thin content provider that calls it once on `Start()`. Gameplay pause is implemented by swapping the Input Actions asset's active map (`Gameplay` ↔ `Dialogue`), not `Time.timeScale` or new flags on existing scripts.

**Tech Stack:** Unity 6000.5.0f1, Unity UI (`Canvas`/`Image`), TextMeshPro, Unity Input System (`InputActionAsset`), `mcp__UnityMCP__*` tooling.

## Global Constraints

- Unity Editor 6000.5.0f1
- Namespaces: `TheLastAethon.Core`, `TheLastAethon.UI`, `TheLastAethon.Gameplay` (this plan adds to `UI` and `Gameplay`)
- No third-party tween/animation libraries — the typewriter effect uses a plain coroutine with `WaitForSeconds`, no tweening needed
- No audio system
- Resizable window, no enforced fullscreen, default 1920×1080 (matches `CanvasScaler.referenceResolution` used here and in `HUDCanvas`)
- Placeholder visuals only (flat-colored UI rectangles) — confirmed deliberately, consistent with sub-project #2's placeholder convention; real art (`ren_portrait.png`, `vesper_portrait.png`, `dialogue_box.png`) is deferred to a future art pass
- `mcp__UnityMCP__*` tooling for all Editor-side work; `read_console` error-check after every script edit
- Independently re-verify raw scene/asset YAML after every MCP tool call rather than trusting "success" responses alone — the `component_properties`/`set_property` silent-apply-failure quirk has hit 100% of `Canvas`/`Image`/`TextMeshProUGUI` creations in this project so far (see [[project_unity_mcp_quirks]])
- No key-press-simulation tool exists among `mcp__UnityMCP__*` tools — edge-detected actions (`WasPressedThisFrame()`) must be verified via direct invocation of the same downstream call the real input edge-detect would trigger, confirmed independently via raw-YAML/state re-reads, exactly as established in sub-project #2 Task 9 (see [[project_unity_mcp_quirks]])

---

### Task 1: Input Actions — Dialogue map

**Files:**
- Modify: `Assets/Input/PlayerControls.inputactions`

**Interfaces:**
- Produces: a `Dialogue` action map with one `Advance` (Button) action, bound to Z / Enter / Space / Left Click. Consumed by Task 2's `DialogueUI` via `inputActions.FindActionMap("Dialogue").FindAction("Advance")`.

- [ ] **Step 1: Overwrite the file with the Dialogue map added**

Replace the full contents of `Assets/Input/PlayerControls.inputactions` with:

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
            "bindings": [
                { "name": "WASD", "id": "8f1b6a2e-0002-4000-8000-000000000010", "path": "2DVector", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": true, "isPartOfComposite": false },
                { "name": "up", "id": "8f1b6a2e-0002-4000-8000-000000000011", "path": "<Keyboard>/w", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "down", "id": "8f1b6a2e-0002-4000-8000-000000000012", "path": "<Keyboard>/s", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "left", "id": "8f1b6a2e-0002-4000-8000-000000000013", "path": "<Keyboard>/a", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "right", "id": "8f1b6a2e-0002-4000-8000-000000000014", "path": "<Keyboard>/d", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "Arrows", "id": "8f1b6a2e-0002-4000-8000-000000000015", "path": "2DVector", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": true, "isPartOfComposite": false },
                { "name": "up", "id": "8f1b6a2e-0002-4000-8000-000000000016", "path": "<Keyboard>/upArrow", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "down", "id": "8f1b6a2e-0002-4000-8000-000000000017", "path": "<Keyboard>/downArrow", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "left", "id": "8f1b6a2e-0002-4000-8000-000000000018", "path": "<Keyboard>/leftArrow", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "right", "id": "8f1b6a2e-0002-4000-8000-000000000019", "path": "<Keyboard>/rightArrow", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Move", "isComposite": false, "isPartOfComposite": true },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-00000000001a", "path": "<Gamepad>/leftStick", "interactions": "", "processors": "", "groups": "Gamepad", "action": "Move", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-00000000001b", "path": "<Keyboard>/leftShift", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Run", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-00000000001c", "path": "<Gamepad>/leftShoulder", "interactions": "", "processors": "", "groups": "Gamepad", "action": "Run", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-00000000001d", "path": "<Keyboard>/space", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Jump", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-00000000001e", "path": "<Keyboard>/upArrow", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Jump", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-00000000001f", "path": "<Gamepad>/buttonSouth", "interactions": "", "processors": "", "groups": "Gamepad", "action": "Jump", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-000000000020", "path": "<Mouse>/leftButton", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Attack", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-000000000021", "path": "<Keyboard>/z", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Attack", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0002-4000-8000-000000000022", "path": "<Gamepad>/buttonWest", "interactions": "", "processors": "", "groups": "Gamepad", "action": "Attack", "isComposite": false, "isPartOfComposite": false }
            ]
        },
        {
            "name": "Debug",
            "id": "8f1b6a2e-0003-4000-8000-000000000001",
            "actions": [
                {
                    "name": "DamageTest",
                    "type": "Button",
                    "id": "8f1b6a2e-0003-4000-8000-000000000002",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": [
                { "name": "", "id": "8f1b6a2e-0003-4000-8000-000000000003", "path": "<Keyboard>/h", "interactions": "", "processors": "", "groups": "", "action": "DamageTest", "isComposite": false, "isPartOfComposite": false }
            ]
        },
        {
            "name": "Dialogue",
            "id": "8f1b6a2e-0004-4000-8000-000000000001",
            "actions": [
                {
                    "name": "Advance",
                    "type": "Button",
                    "id": "8f1b6a2e-0004-4000-8000-000000000002",
                    "expectedControlType": "Button",
                    "processors": "",
                    "interactions": "",
                    "initialStateCheck": false
                }
            ],
            "bindings": [
                { "name": "", "id": "8f1b6a2e-0004-4000-8000-000000000003", "path": "<Keyboard>/z", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Advance", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0004-4000-8000-000000000004", "path": "<Keyboard>/enter", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Advance", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0004-4000-8000-000000000005", "path": "<Keyboard>/space", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Advance", "isComposite": false, "isPartOfComposite": false },
                { "name": "", "id": "8f1b6a2e-0004-4000-8000-000000000006", "path": "<Mouse>/leftButton", "interactions": "", "processors": "", "groups": "Keyboard&Mouse", "action": "Advance", "isComposite": false, "isPartOfComposite": false }
            ]
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

(Only the new `Dialogue` map block and its 4 bindings are new; `Gameplay`/`Debug`/`controlSchemes` are byte-for-byte unchanged from the current file — confirm this with a diff after writing.)

- [ ] **Step 2: Verify Unity reimports it without errors**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console(action="get", types=["error"], count=50)`. Expect 0 errors referencing `PlayerControls.inputactions`. Then call `mcp__UnityMCP__manage_asset(action="get_info", path="Assets/Input/PlayerControls.inputactions")` and confirm it still resolves as `UnityEngine.InputSystem.InputActionAsset` with GUID `295a9007186f8ac4581661722fc28cd0` (unchanged — same asset file, new content).

- [ ] **Step 3: Commit**

```bash
git add "Assets/Input/PlayerControls.inputactions"
git commit -m "Add Dialogue action map with Advance action"
```

---

### Task 2: DialogueUI engine

**Files:**
- Create: `Assets/Scripts/UI/DialogueUI.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Consumes: `Dialogue`/`Advance` and `Gameplay` action maps on `Assets/Input/PlayerControls.inputactions` (Task 1).
- Produces: `TheLastAethon.UI.DialogueSpeaker` (enum: `Narrator`, `Ren`, `Vesper`), `TheLastAethon.UI.DialogueLine` (struct: `Speaker`, `Text`), `TheLastAethon.UI.DialogueUI.Play(DialogueLine[] lines, System.Action onComplete)`. Consumed by Task 3's `GameplayIntroTrigger`.

- [ ] **Step 1: Create DialogueUI.cs**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="DialogueUI"`, `path="Assets/Scripts/UI"`, `contents`:

```csharp
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastAethon.UI
{
    public enum DialogueSpeaker
    {
        Narrator,
        Ren,
        Vesper
    }

    public struct DialogueLine
    {
        public DialogueSpeaker Speaker;
        public string Text;
    }

    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private GameObject portraitLeft;
        [SerializeField] private GameObject portraitRight;
        [SerializeField] private RectTransform dialogueBox;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI continueHint;
        [SerializeField] private float charDelay = 0.028f;

        private const float BoxCenterX = 0f;
        private const float BoxLeftX = -300f;
        private const float BoxRightX = 300f;

        private InputActionMap gameplayMap;
        private InputActionMap dialogueMap;
        private InputAction advanceAction;

        private DialogueLine[] lines;
        private int lineIndex;
        private bool isTyping;
        private Coroutine typeCoroutine;
        private Action onComplete;

        private void Awake()
        {
            gameplayMap = inputActions.FindActionMap("Gameplay");
            dialogueMap = inputActions.FindActionMap("Dialogue");
            advanceAction = dialogueMap.FindAction("Advance");
            gameObject.SetActive(false);
        }

        public void Play(DialogueLine[] sequence, Action onCompleteCallback)
        {
            lines = sequence;
            onComplete = onCompleteCallback;
            lineIndex = -1;
            gameObject.SetActive(true);
            gameplayMap.Disable();
            dialogueMap.Enable();
            ShowNextLine();
        }

        private void Update()
        {
            if (advanceAction.WasPressedThisFrame())
            {
                if (isTyping)
                {
                    SkipTyping();
                }
                else
                {
                    ShowNextLine();
                }
            }

            if (continueHint.gameObject.activeSelf)
            {
                float alpha = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
                Color c = continueHint.color;
                c.a = alpha;
                continueHint.color = c;
            }
        }

        private void ShowNextLine()
        {
            lineIndex++;
            if (lineIndex >= lines.Length)
            {
                EndDialogue();
                return;
            }

            DialogueLine line = lines[lineIndex];
            ApplySpeakerVisuals(line.Speaker);
            dialogueText.text = string.Empty;
            continueHint.gameObject.SetActive(false);

            if (typeCoroutine != null)
            {
                StopCoroutine(typeCoroutine);
            }
            typeCoroutine = StartCoroutine(TypeLine(line.Text));
        }

        private void ApplySpeakerVisuals(DialogueSpeaker speaker)
        {
            portraitLeft.SetActive(speaker == DialogueSpeaker.Ren);
            portraitRight.SetActive(speaker == DialogueSpeaker.Vesper);
            nameLabel.gameObject.SetActive(speaker != DialogueSpeaker.Narrator);
            nameLabel.text = speaker == DialogueSpeaker.Ren ? "Ren" : speaker == DialogueSpeaker.Vesper ? "Vesper" : string.Empty;

            float boxX = speaker == DialogueSpeaker.Ren ? BoxRightX : speaker == DialogueSpeaker.Vesper ? BoxLeftX : BoxCenterX;
            dialogueBox.anchoredPosition = new Vector2(boxX, dialogueBox.anchoredPosition.y);
        }

        private IEnumerator TypeLine(string text)
        {
            isTyping = true;
            for (int i = 0; i <= text.Length; i++)
            {
                dialogueText.text = text.Substring(0, i);
                yield return new WaitForSeconds(charDelay);
            }
            isTyping = false;
            continueHint.gameObject.SetActive(true);
        }

        private void SkipTyping()
        {
            StopCoroutine(typeCoroutine);
            dialogueText.text = lines[lineIndex].Text;
            isTyping = false;
            continueHint.gameObject.SetActive(true);
        }

        private void EndDialogue()
        {
            gameObject.SetActive(false);
            dialogueMap.Disable();
            gameplayMap.Enable();
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `DialogueUI.cs`.

- [ ] **Step 3: Create the DialogueCanvas**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="DialogueCanvas",
  components_to_add=["Canvas", "CanvasScaler", "GraphicRaycaster", "TheLastAethon.UI.DialogueUI"],
  component_properties={
    "Canvas": {"renderMode": 0, "sortingOrder": 20},
    "CanvasScaler": {"uiScaleMode": 1, "referenceResolution": {"x": 1920, "y": 1080}, "screenMatchMode": 0}
  }
)
```

Note the returned instance ID as `<dialogueCanvasId>`. (`sortingOrder: 20` puts this above `HUDCanvas`'s `10`, so dialogue renders on top of the HP bar.)

- [ ] **Step 4: Create the dim overlay**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="DimOverlay",
  parent="DialogueCanvas",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0, "g": 0, "b": 0, "a": 0.3}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}
  }
)
```

- [ ] **Step 5: Create the two portrait placeholders**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="PortraitLeft",
  parent="DialogueCanvas",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0.290196, "g": 0.435294, "b": 0.647059, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 0, "y": 0}, "pivot": {"x": 0.5, "y": 0}, "anchoredPosition": {"x": 270, "y": 0}, "sizeDelta": {"x": 540, "y": 1080}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="PortraitRight",
  parent="DialogueCanvas",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0.556863, "g": 0.356863, "b": 0.619608, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 1, "y": 0}, "anchorMax": {"x": 1, "y": 0}, "pivot": {"x": 0.5, "y": 0}, "anchoredPosition": {"x": -270, "y": 0}, "sizeDelta": {"x": 540, "y": 1080}}
  }
)
```

(Left = Ren tint, steel blue; Right = Vesper tint, muted purple — both placeholders, both initially active in the Editor; `DialogueUI.Awake()` and `ApplySpeakerVisuals()` control their actual visibility at runtime.)

- [ ] **Step 6: Create the dialogue box and its child text elements**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="DialogueBox",
  parent="DialogueCanvas",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0.168627, "g": 0.129412, "b": 0.094118, "a": 0.92}},
    "RectTransform": {"anchorMin": {"x": 0.5, "y": 0}, "anchorMax": {"x": 0.5, "y": 0}, "pivot": {"x": 0.5, "y": 0}, "anchoredPosition": {"x": 0, "y": 40}, "sizeDelta": {"x": 1000, "y": 260}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="NameLabel",
  parent="DialogueBox",
  components_to_add=["TextMeshProUGUI"],
  component_properties={
    "TextMeshProUGUI": {"text": "Ren", "fontSize": 22, "color": {"r": 0.788235, "g": 0.658824, "b": 0.298039, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 30, "y": -15}, "sizeDelta": {"x": 300, "y": 36}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="DialogueText",
  parent="DialogueBox",
  components_to_add=["TextMeshProUGUI"],
  component_properties={
    "TextMeshProUGUI": {"text": "", "fontSize": 26, "color": {"r": 0.831373, "g": 0.788235, "b": 0.658824, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 30, "y": 60}, "offsetMax": {"x": -30, "y": -55}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="ContinueHint",
  parent="DialogueBox",
  components_to_add=["TextMeshProUGUI"],
  component_properties={
    "TextMeshProUGUI": {"text": "▶ Z / Enter", "fontSize": 16, "color": {"r": 1, "g": 1, "b": 1, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 1, "y": 0}, "anchorMax": {"x": 1, "y": 0}, "pivot": {"x": 1, "y": 0}, "anchoredPosition": {"x": -30, "y": 15}, "sizeDelta": {"x": 200, "y": 30}}
  }
)
```

Note the returned instance IDs as `<portraitLeftId>`, `<portraitRightId>`, `<dialogueBoxId>`, `<nameLabelId>`, `<dialogueTextId>`, `<continueHintId>`.

- [ ] **Step 7: Wire DialogueUI's fields**

```
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="inputActions", value="Assets/Input/PlayerControls.inputactions")
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="portraitLeft", value="<portraitLeftId>")
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="portraitRight", value="<portraitRightId>")
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="dialogueBox", value="<dialogueBoxId>")
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="nameLabel", value="<nameLabelId>")
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="dialogueText", value="<dialogueTextId>")
mcp__UnityMCP__manage_components(action="set_property", target="<dialogueCanvasId>", component_type="TheLastAethon.UI.DialogueUI", property="continueHint", value="<continueHintId>")
```

(`inputActions` takes the asset path directly, the same pattern used for `PlayerController.inputActions` in sub-project #2 Task 6 — it resolves to `{fileID: -944628639613478452, guid: 295a9007186f8ac4581661722fc28cd0, type: 3}`.)

- [ ] **Step 8: Save and independently re-verify**

Call `mcp__UnityMCP__manage_scene(action="save")`, then re-read the raw `Assets/Scenes/Game.unity` YAML. Confirm, per the established silent-apply-failure quirk:
- `DialogueUI`'s 7 fields all resolve to non-zero `{fileID: ...}` (or the correct asset guid for `inputActions`), not `{fileID: 0}`.
- `PortraitLeft`/`PortraitRight` `RectTransform.m_SizeDelta` is `{x: 540, y: 1080}` (not a dropped/default value).
- `DialogueBox` `RectTransform.m_AnchoredPosition` is `{x: 0, y: 40}` and `m_SizeDelta` is `{x: 1000, y: 260}`.
- `DialogueCanvas`'s own `Canvas.m_SortingOrder` is `20` and `m_RenderMode` is `0`.
- Fix any silently-dropped property via a follow-up `set_property` call and re-read until all match.

- [ ] **Step 9: Standalone Play Mode verification of the engine**

Call `mcp__UnityMCP__manage_editor(action="play")`. Using `mcp__UnityMCP__execute_code`, get the `DialogueUI` component on `DialogueCanvas` and call `Play()` directly with a short ad-hoc 3-line test sequence (this does not require `GameplayIntroTrigger` from Task 3 — `DialogueUI` is self-contained):

```csharp
var lines = new TheLastAethon.UI.DialogueLine[] {
    new TheLastAethon.UI.DialogueLine { Speaker = TheLastAethon.UI.DialogueSpeaker.Narrator, Text = "Test line one." },
    new TheLastAethon.UI.DialogueLine { Speaker = TheLastAethon.UI.DialogueSpeaker.Ren, Text = "Test line two." },
    new TheLastAethon.UI.DialogueLine { Speaker = TheLastAethon.UI.DialogueSpeaker.Vesper, Text = "Test line three." },
};
dialogueUIComponent.Play(lines, null);
```

Confirm via state reads after the call:
- `DialogueCanvas.activeSelf` is now `true` (it starts `false` per `Awake()`).
- The `Gameplay` action map's `enabled` is now `false` and `Dialogue`'s is `true`.
- `dialogueText.text` is non-empty and growing — since the typewriter uses `WaitForSeconds` (real engine time, not something `Physics2D.Simulate`/`Animator.Update` can step deterministically), confirm this via natural-elapsed-time sampling across a few tool calls (same technique as Task 9's Regen check), not single-frame stepping. If real-time sampling proves too slow/unreliable across tool-call round trips, temporarily set `Time.timeScale` to a large value (e.g. `20`) for this verification window only, then restore it to `1` afterward — `WaitForSeconds` respects scaled time, so this compresses the wait without changing the coroutine logic.
- Once `dialogueText.text` equals the full line text, `continueHint.gameObject.activeSelf` is `true`.
- `Advance` is edge-detected and no key-press-simulation tool exists among `mcp__UnityMCP__*` tools (same accepted limitation as Jump/Attack in sub-project #2 Task 9), so invoke `DialogueUI`'s private advance-handling methods directly via reflection — this is the exact same call `Update()` makes on a real `Advance` press:

```csharp
var t = typeof(TheLastAethon.UI.DialogueUI);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var showNextLine = t.GetMethod("ShowNextLine", flags);
var skipTyping = t.GetMethod("SkipTyping", flags);

// While a line is still typing, this is what a real Advance press does:
skipTyping.Invoke(dialogueUIComponent, null);
// Once a line has finished typing (continueHint active), this is what a real Advance press does:
showNextLine.Invoke(dialogueUIComponent, null);
```

Step through all 3 lines using `showNextLine.Invoke(...)` (each line will already be in its idle/finished-typing state from the natural-elapsed-time sampling above, so `skipTyping` isn't required for this pass — call it once on line 1 first to confirm it also works, then use `showNextLine` for the rest). After each invocation, confirm `PortraitLeft`/`PortraitRight`/`nameLabel` active-state and `dialogueBox.anchoredPosition.x` match the expected speaker for that line (Narrator → both portraits inactive, box x=0; Ren → `PortraitLeft` active, box x=300; Vesper → `PortraitRight` active, box x=-300).
- After the 3rd line's final `showNextLine.Invoke(...)` (which internally calls `EndDialogue()`): `DialogueCanvas.activeSelf` is `false` again, `Gameplay` map `enabled` is `true`, `Dialogue` map `enabled` is `false`.

Call `mcp__UnityMCP__manage_editor(action="stop")` afterward. Re-read the raw scene YAML once more and confirm it is unchanged from the Step 8 save (Play Mode edits are runtime-only).

`read_console(action="get", types=["error"])` after the whole sequence — expect 0 errors.

- [ ] **Step 10: Commit**

```bash
git add "Assets/Scripts/UI/DialogueUI.cs" "Assets/Scenes/Game.unity"
git commit -m "Add DialogueUI engine: typewriter, portrait swap, box repositioning, input-gated advance"
```

---

### Task 3: GameplayIntroTrigger

**Files:**
- Create: `Assets/Scripts/Gameplay/GameplayIntroTrigger.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Consumes: `TheLastAethon.UI.DialogueUI.Play(DialogueLine[] lines, Action onComplete)`, `TheLastAethon.UI.DialogueLine`, `TheLastAethon.UI.DialogueSpeaker` (Task 2).

- [ ] **Step 1: Create GameplayIntroTrigger.cs**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="GameplayIntroTrigger"`, `path="Assets/Scripts/Gameplay"`, `contents`:

```csharp
using UnityEngine;
using TheLastAethon.UI;

namespace TheLastAethon.Gameplay
{
    public class GameplayIntroTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;

        private void Start()
        {
            DialogueLine[] lines =
            {
                new DialogueLine { Speaker = DialogueSpeaker.Narrator, Text = "Ashenveil Forest. Ten years after the fall of the Aethon Clan." },
                new DialogueLine { Speaker = DialogueSpeaker.Ren, Text = "Still here. Still breathing. That's enough for today." },
                new DialogueLine { Speaker = DialogueSpeaker.Narrator, Text = "He had been running for as long as he could remember. But lately... something felt different." },
                new DialogueLine { Speaker = DialogueSpeaker.Ren, Text = "The patrols are getting closer. They're not just searching anymore." },
                new DialogueLine { Speaker = DialogueSpeaker.Vesper, Text = "You're not as hard to find as they say." },
                new DialogueLine { Speaker = DialogueSpeaker.Ren, Text = "..." },
                new DialogueLine { Speaker = DialogueSpeaker.Vesper, Text = "The last person you'll ever meet. Now stop talking." },
            };

            dialogueUI.Play(lines, null);
        }
    }
}
```

(Content ported verbatim from the original's `triggerIntroDialogue()` — see `../the-last-aethon/src/scenes/GameScene.ts:151-189`.)

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `GameplayIntroTrigger.cs`.

- [ ] **Step 3: Create the trigger GameObject and wire it**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="IntroDialogueTrigger",
  components_to_add=["TheLastAethon.Gameplay.GameplayIntroTrigger"]
)
```

Note the returned instance ID as `<introTriggerId>`. Then wire its `dialogueUI` field to the `DialogueCanvas`'s `DialogueUI` component from Task 2 (`<dialogueCanvasId>`):

```
mcp__UnityMCP__manage_components(action="set_property", target="<introTriggerId>", component_type="TheLastAethon.Gameplay.GameplayIntroTrigger", property="dialogueUI", value="<dialogueCanvasId>")
```

- [ ] **Step 4: Save and independently re-verify**

Call `mcp__UnityMCP__manage_scene(action="save")`, then re-read the raw `Assets/Scenes/Game.unity` YAML. Confirm `GameplayIntroTrigger.dialogueUI` resolves to a non-zero `{fileID: ...}` matching `DialogueCanvas`'s `DialogueUI` component block (cross-reference by `fileID`, the same fileID-chasing discipline used throughout sub-project #2).

- [ ] **Step 5: End-to-end Play Mode verification**

Call `mcp__UnityMCP__manage_editor(action="play")`. Confirm via state reads / a screenshot:
- Immediately on entering Play Mode, `DialogueCanvas.activeSelf` becomes `true` (the intro auto-fires from `GameplayIntroTrigger.Start()`) and the first line ("Ashenveil Forest...", Narrator) begins typing — both portraits inactive, `dialogueBox.anchoredPosition.x == 0`.
- While the intro is active, confirm `Gameplay` input is inert. Inject a real key-hold via `execute_code` (the same real-injection technique sub-project #2 Task 9 used for Move):

```csharp
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
InputSystem.Update();
```

  Confirm `moveAction.ReadValue<Vector2>()` reads `Vector2.zero` (the disabled `Gameplay` map reports no input regardless of device state) and the Player's `Rigidbody2D.linearVelocity.x` stays `0` despite the simulated key-hold — proving `PlayerController` is not consuming movement input while the `Gameplay` map is disabled. Release the key afterward: `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); InputSystem.Update();`.
- Step through all 7 lines using the same reflection-based `showNextLine`/`skipTyping` invocation as Task 2 Step 9 (no key-press-simulation tool exists for the edge-detected `Advance` action). For each line, confirm: correct speaker text, correct portrait active-state (Ren → `PortraitLeft`; Vesper → `PortraitRight`; Narrator → neither), correct `nameLabel.gameObject.activeSelf` (`false` only for Narrator lines), correct `dialogueBox.anchoredPosition.x` (300 for Ren, -300 for Vesper, 0 for Narrator).
- After advancing past the 7th and final line ("The last person you'll ever meet. Now stop talking."): `DialogueCanvas.activeSelf` is `false`, `Gameplay` map `enabled` is `true`, `Dialogue` map `enabled` is `false`.
- Re-test `Gameplay` input now responds normally: repeat the `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D)); InputSystem.Update();` injection and confirm it now does move the Player (`Rigidbody2D.linearVelocity.x` matches `walkSpeed`), confirming input control was correctly handed back. Release the key afterward.

Call `mcp__UnityMCP__manage_editor(action="stop")` afterward. Re-read the raw scene YAML once more and confirm it is unchanged from the Step 4 save.

`read_console(action="get", types=["error"])` after the whole sequence — expect 0 errors.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/Gameplay/GameplayIntroTrigger.cs" "Assets/Scenes/Game.unity"
git commit -m "Add GameplayIntroTrigger and wire intro dialogue to auto-fire on scene start"
```
