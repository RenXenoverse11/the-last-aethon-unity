# Player Controller / Gameplay Systems Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable, physics-driven Player (move/run/jump/attack, HP/regen/hurt/knockback) on a placeholder-art level (ground + 5 platforms), with a Cinemachine-driven camera and an HP HUD — sub-project #2 of the Unity redesign of "The Last Aethon".

**Architecture:** Two new namespaced script groups: `TheLastAethon.Gameplay` (`PlayerHealth`, `PlayerController`, `DebugDamageTrigger`) and an addition to `TheLastAethon.UI` (`PlayerHUD`). A `Player` GameObject in the `Game` scene splits physics (root: `Rigidbody2D` + `BoxCollider2D`) from visuals (child `Visual`: `SpriteRenderer` + `Animator`) so Animator-driven squash/tint never fights collider geometry. Two one-off Editor-only build scripts (`Assets/Editor/PlaceholderAssetBuilder.cs`) generate a shared placeholder sprite and the Player's Animator Controller deterministically via Unity's `AnimatorController`/`AnimationClip` APIs, sidestepping any uncertainty about MCP animation-authoring tool support. Cinemachine is added by hand-editing `Packages/manifest.json` (same pattern as `.inputactions`), the same way Task 1 of sub-project #1 wrote `PlayerControls.inputactions` directly.

**Tech Stack:** Unity 6000.5.0f1, URP 17.5.0, Input System 1.19.0, Cinemachine 3.1.4 (added this plan), 2D Physics, TextMeshPro, C#.

**Source spec:** `docs/superpowers/specs/2026-06-19-unity-player-controller-design.md`

**Source files referenced (sibling Phaser repo, read-only):** `the-last-aethon/src/objects/Player.ts`, `the-last-aethon/src/scenes/GameScene.ts`, `the-last-aethon/src/ui/HUD.ts`, `the-last-aethon/src/main.ts` (canvas 1920×1080, confirms `groundY = height - 170 = 910`, `worldWidth = width * 4 = 7680`).

## Global Constraints

- Unity Editor version: 6000.5.0f1 — do not introduce APIs requiring a newer/older Editor.
- C# namespaces: `TheLastAethon.Gameplay` (new, this sub-project) and `TheLastAethon.UI` (existing, from sub-project #1).
- No third-party tween/animation libraries (Cinemachine is first-party, does not violate this).
- No audio system.
- Default windowed resolution 1920×1080, resizable, no enforced fullscreen.
- Velocities are proportional pixel→meter conversions, not pixel-exact, per spec's Units note — exact constants (`walkSpeed`, `runSpeed`, `jumpSpeed`) are fixed values chosen below, not subject to further "accuracy" debate during implementation.
- Tooling: this project is wired to the UnityMCP server (`mcp__UnityMCP__*` tools) — use those for all Editor-side scene/GameObject/component work. Hand-write a file directly only where a step explicitly says to (`.inputactions` JSON, `Packages/manifest.json`, `ProjectSettings/TagManager.asset`, and the two Editor build scripts) — Unity auto-imports/auto-reloads all of these.
- After every script create/edit, call `read_console` (action `get`, filter `error`) before moving on.
- Independently re-read raw scene/asset YAML after MCP tool calls rather than trusting "success" alone (see `feedback_unity_mcp_trust_but_verify` / `project_unity_mcp_quirks` memory notes) — every task below that touches `Game.unity` ends with a hierarchy/property re-check before commit.
- Commit after every task with `git add` of the exact files touched (never `git add -A`).
- World/level constants fixed by this plan (1 Unity unit = 100 original px, ground top at world y = 0):
  - Ground: center `(38.4, -0.1, 0)`, size `(76.8, 0.2)`, color `#1A2E1A` (opaque — original was alpha-0 because parallax art handled visuals; we have no parallax art yet, so it must be visible).
  - Platforms (center x, center y, width; height `0.14` and color `#2A3A2A` for all five): `(5.0, 1.2, 2.0)`, `(9.0, 1.8, 1.6)`, `(13.0, 1.0, 2.2)`, `(17.0, 1.6, 1.8)`, `(21.0, 1.3, 2.0)`.
  - Player spawn: `(1.2, 0.8, 0)`.
  - Boundary walls: left center `(-0.25, 5, 0)` size `(0.5, 20)`; right center `(77.05, 5, 0)` size `(0.5, 20)`.

---

### Task 1: Input Actions — Gameplay bindings + Debug map

**Files:**
- Modify: `Assets/Input/PlayerControls.inputactions` (currently has the `Gameplay` map's 4 actions stubbed with empty bindings, from sub-project #1)

**Interfaces:**
- Produces: `Gameplay` map's `Move`/`Run`/`Jump`/`Attack` actions now have real bindings. A new `Debug` map with one action `DamageTest` (Button, H key). Task 4 (`PlayerController`) reads `Gameplay` actions by name via `FindActionMap("Gameplay").FindAction(...)`. Task 3 (`DebugDamageTrigger`) reads `Debug`/`DamageTest` the same way.

- [ ] **Step 1: Overwrite the file with this exact content**

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

- [ ] **Step 2: Re-import and verify**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect no errors mentioning `PlayerControls`. Then `mcp__UnityMCP__manage_asset(action="get_info", path="Assets/Input/PlayerControls.inputactions")` and confirm it still resolves as an Input Actions asset.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Input/PlayerControls.inputactions"
git commit -m "Bind Gameplay actions and add Debug action map for damage testing"
```

---

### Task 2: PlayerHealth.cs

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerHealth.cs`

**Interfaces:**
- Produces: `TheLastAethon.Gameplay.PlayerHealth`, a `MonoBehaviour` requiring a `Rigidbody2D` on the same GameObject. Public surface: `int Hp { get; }`, `int MaxHp { get; }` (backed by serialized `maxHp = 100`), `bool IsHurt { get; }`, `void TakeDamage(int amount)`. Task 3's `DebugDamageTrigger` calls `TakeDamage`. Task 4's `PlayerController` reads `IsHurt` every frame to gate input. Task 9's `PlayerHUD` reads `Hp`/`MaxHp`.

- [ ] **Step 1: Create the script**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="PlayerHealth"`, `path="Assets/Scripts/Gameplay"`, `contents`:

```csharp
using UnityEngine;

namespace TheLastAethon.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHp = 100;
        [SerializeField] private float regenDelay = 5f;
        [SerializeField] private int regenAmount = 5;
        [SerializeField] private float hurtDuration = 0.4f;
        [SerializeField] private float knockbackSpeedX = 3f;
        [SerializeField] private float knockbackSpeedY = 4f;

        private Rigidbody2D body;
        private float regenTimer;
        private float hurtTimer;

        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool IsHurt { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            Hp = maxHp;
        }

        private void Update()
        {
            if (IsHurt)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f)
                {
                    IsHurt = false;
                }
                return;
            }

            if (Hp < maxHp)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= regenDelay)
                {
                    Hp = Mathf.Min(maxHp, Hp + regenAmount);
                    regenTimer = 0f;
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsHurt) return;

            Hp = Mathf.Max(0, Hp - amount);
            IsHurt = true;
            hurtTimer = hurtDuration;
            regenTimer = 0f;

            float currentX = body.linearVelocity.x;
            float knockbackX = currentX >= 0f ? -knockbackSpeedX : knockbackSpeedX;
            body.linearVelocity = new Vector2(knockbackX, knockbackSpeedY);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `PlayerHealth.cs`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Gameplay/PlayerHealth.cs"
git commit -m "Add PlayerHealth with regen, hurt timer, and knockback"
```

---

### Task 3: DebugDamageTrigger.cs

**Files:**
- Create: `Assets/Scripts/Gameplay/DebugDamageTrigger.cs`

**Interfaces:**
- Produces: `TheLastAethon.Gameplay.DebugDamageTrigger`, a `MonoBehaviour` with a serialized `InputActionAsset inputActions` and `PlayerHealth target` field. On the `Debug`/`DamageTest` action firing, calls `target.TakeDamage(debugDamageAmount)` (`debugDamageAmount` default `10`).
- Consumes: `TheLastAethon.Gameplay.PlayerHealth.TakeDamage(int)` (Task 2), the `Debug` action map's `DamageTest` action (Task 1).

- [ ] **Step 1: Create the script**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="DebugDamageTrigger"`, `path="Assets/Scripts/Gameplay"`, `contents`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastAethon.Gameplay
{
    public class DebugDamageTrigger : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerHealth target;
        [SerializeField] private int debugDamageAmount = 10;

        private InputAction damageTestAction;

        private void Awake()
        {
            damageTestAction = inputActions.FindActionMap("Debug").FindAction("DamageTest");
        }

        private void OnEnable()
        {
            damageTestAction.Enable();
        }

        private void OnDisable()
        {
            damageTestAction.Disable();
        }

        private void Update()
        {
            if (damageTestAction.WasPressedThisFrame())
            {
                target.TakeDamage(debugDamageAmount);
            }
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `DebugDamageTrigger.cs`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Gameplay/DebugDamageTrigger.cs"
git commit -m "Add debug-only damage trigger for manual hurt/knockback/regen testing"
```

---

### Task 4: PlayerController.cs

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerController.cs`

**Interfaces:**
- Produces: `TheLastAethon.Gameplay.PlayerController`, a `MonoBehaviour` with serialized fields `inputActions` (`InputActionAsset`), `visualSprite` (`SpriteRenderer`), `animator` (`Animator`), `groundCheck` (`Transform`), `groundLayer` (`LayerMask`), and tunables `walkSpeed = 3.2f`, `runSpeed = 5.6f`, `jumpSpeed = 9f`, `attackDuration = 0.5f`, `groundCheckRadius = 0.15f`. Requires `Rigidbody2D` and `PlayerHealth` on the same GameObject (read via `GetComponent` in `Awake`). Drives Animator parameters `Speed` (float), `IsRunning` (bool), `IsGrounded` (bool), `Attack` (trigger), `Hurt` (trigger) — Task 5's Animator Controller must define exactly these names and types.
- Consumes: `TheLastAethon.Gameplay.PlayerHealth.IsHurt` (Task 2), `Gameplay` action map's `Move`/`Run`/`Jump`/`Attack` (Task 1).

- [ ] **Step 1: Create the script**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="PlayerController"`, `path="Assets/Scripts/Gameplay"`, `contents`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastAethon.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private SpriteRenderer visualSprite;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.6f;
        [SerializeField] private float jumpSpeed = 9f;
        [SerializeField] private float attackDuration = 0.5f;
        [SerializeField] private float groundCheckRadius = 0.15f;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsRunningParam = Animator.StringToHash("IsRunning");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int HurtParam = Animator.StringToHash("Hurt");

        private Rigidbody2D body;
        private PlayerHealth health;

        private InputAction moveAction;
        private InputAction runAction;
        private InputAction jumpAction;
        private InputAction attackAction;

        private bool isAttacking;
        private float attackTimer;
        private bool wasHurt;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<PlayerHealth>();

            InputActionMap gameplayMap = inputActions.FindActionMap("Gameplay");
            moveAction = gameplayMap.FindAction("Move");
            runAction = gameplayMap.FindAction("Run");
            jumpAction = gameplayMap.FindAction("Jump");
            attackAction = gameplayMap.FindAction("Attack");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            runAction.Enable();
            jumpAction.Enable();
            attackAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            runAction.Disable();
            jumpAction.Disable();
            attackAction.Disable();
        }

        private void Update()
        {
            if (health.IsHurt)
            {
                isAttacking = false;
                if (!wasHurt)
                {
                    animator.SetTrigger(HurtParam);
                }
                wasHurt = true;
                return;
            }
            wasHurt = false;

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            animator.SetBool(IsGroundedParam, isGrounded);

            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    isAttacking = false;
                }
                else
                {
                    body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                    return;
                }
            }

            if (attackAction.WasPressedThisFrame() && isGrounded)
            {
                isAttacking = true;
                attackTimer = attackDuration;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                animator.SetTrigger(AttackParam);
                return;
            }

            float moveX = moveAction.ReadValue<Vector2>().x;
            bool isRunning = runAction.IsPressed();
            float speed = isRunning ? runSpeed : walkSpeed;

            if (Mathf.Abs(moveX) > 0.01f)
            {
                body.linearVelocity = new Vector2(moveX > 0f ? speed : -speed, body.linearVelocity.y);
                visualSprite.flipX = moveX < 0f;
            }
            else
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }

            animator.SetFloat(SpeedParam, Mathf.Abs(moveX));
            animator.SetBool(IsRunningParam, isRunning);

            if (jumpAction.WasPressedThisFrame() && isGrounded)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            }
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `PlayerController.cs`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Gameplay/PlayerController.cs"
git commit -m "Add PlayerController: movement, run, jump, attack-trigger, hurt gating"
```

---

### Task 5: Placeholder sprite + Player Animator Controller

**Files:**
- Create: `Assets/Editor/PlaceholderAssetBuilder.cs` (one-off Editor build script — `Assets/Editor/` is excluded from builds by Unity automatically, no further cleanup needed)
- Create (generated by running the menu items below, not hand-written): `Assets/Art/Placeholder/WhiteSquare.png`, `Assets/Animations/Player/PlayerAnimatorController.controller`

**Interfaces:**
- Produces: a shared 100×100px white square sprite at 100 PPU (so `localScale` in world units maps 1:1 onto sprite size) for every placeholder `SpriteRenderer` in this plan (Player visual, ground, platforms). Produces `PlayerAnimatorController.controller` with parameters `Speed` (float), `IsRunning` (bool), `IsGrounded` (bool), `Attack` (trigger), `Hurt` (trigger) and states `Idle`/`Walk`/`Run`/`Jump`/`Attack`/`Hurt` — exact names Task 4's `PlayerController` already references via `Animator.StringToHash`. Task 6 attaches this controller to the Player's `Visual` child and uses the sprite on every placeholder `SpriteRenderer`.

- [ ] **Step 1: Create the build script**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="PlaceholderAssetBuilder"`, `path="Assets/Editor"`, `contents`:

```csharp
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlaceholderAssetBuilder
{
    private const float VisualWidth = 0.6f;
    private const float VisualHeight = 1.6f;

    [MenuItem("Tools/The Last Aethon/Build Placeholder Sprite")]
    public static void BuildSprite()
    {
        const string dir = "Assets/Art/Placeholder";
        const string pngPath = dir + "/WhiteSquare.png";
        Directory.CreateDirectory(dir);

        Texture2D tex = new Texture2D(100, 100, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[100 * 100];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(pngPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        Debug.Log("Placeholder sprite built at " + pngPath);
    }

    [MenuItem("Tools/The Last Aethon/Build Player Animator Controller")]
    public static void BuildAnimatorController()
    {
        const string controllerPath = "Assets/Animations/Player/PlayerAnimatorController.controller";
        Directory.CreateDirectory("Assets/Animations/Player");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimationClip idleClip = CreateClip(controllerPath, "Idle", new Color(0.3f, 0.6f, 1f), 1f, true, 1f);
        AnimationClip walkClip = CreateClip(controllerPath, "Walk", new Color(0.3f, 1f, 0.4f), 0.5f, true, 1f);
        AnimationClip runClip = CreateClip(controllerPath, "Run", new Color(1f, 0.9f, 0.2f), 0.3f, true, 1f);
        AnimationClip jumpClip = CreateClip(controllerPath, "Jump", new Color(0.6f, 0.3f, 1f), 0.6f, true, 1.15f);
        AnimationClip attackClip = CreateClip(controllerPath, "Attack", new Color(1f, 0.3f, 0.3f), 0.5f, false, 0.85f);
        AnimationClip hurtClip = CreateClip(controllerPath, "Hurt", new Color(1f, 1f, 1f), 0.4f, false, 1f);

        AnimatorState idle = sm.AddState("Idle", new Vector3(240, 0, 0));
        idle.motion = idleClip;
        AnimatorState walk = sm.AddState("Walk", new Vector3(240, 80, 0));
        walk.motion = walkClip;
        AnimatorState run = sm.AddState("Run", new Vector3(240, 160, 0));
        run.motion = runClip;
        AnimatorState jump = sm.AddState("Jump", new Vector3(420, 80, 0));
        jump.motion = jumpClip;
        AnimatorState attack = sm.AddState("Attack", new Vector3(420, 160, 0));
        attack.motion = attackClip;
        AnimatorState hurt = sm.AddState("Hurt", new Vector3(420, 240, 0));
        hurt.motion = hurtClip;

        sm.defaultState = idle;

        AnimatorStateTransition t;

        t = idle.AddTransition(walk);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

        t = idle.AddTransition(run);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
        t.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        t = walk.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        t = walk.AddTransition(run);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        t = run.AddTransition(walk);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

        t = run.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        AnimatorState[] groundedSources = { idle, walk, run };
        foreach (AnimatorState source in groundedSources)
        {
            AnimatorStateTransition jt = source.AddTransition(jump);
            jt.hasExitTime = false; jt.duration = 0.05f;
            jt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        }

        AnimatorStateTransition jumpToIdle = jump.AddTransition(idle);
        jumpToIdle.hasExitTime = false; jumpToIdle.duration = 0.1f;
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

        AnimatorState[] attackSources = { idle, walk, run };
        foreach (AnimatorState source in attackSources)
        {
            AnimatorStateTransition at = source.AddTransition(attack);
            at.hasExitTime = false; at.duration = 0f;
            at.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        }

        AnimatorStateTransition attackToIdle = attack.AddTransition(idle);
        attackToIdle.hasExitTime = true; attackToIdle.exitTime = 1f; attackToIdle.duration = 0.1f;

        AnimatorState[] hurtSources = { idle, walk, run, jump, attack };
        foreach (AnimatorState source in hurtSources)
        {
            AnimatorStateTransition ht = source.AddTransition(hurt);
            ht.hasExitTime = false; ht.duration = 0f;
            ht.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        }

        AnimatorStateTransition hurtToIdle = hurt.AddTransition(idle);
        hurtToIdle.hasExitTime = true; hurtToIdle.exitTime = 1f; hurtToIdle.duration = 0.1f;

        AssetDatabase.SaveAssets();
        Debug.Log("PlayerAnimatorController built at " + controllerPath);
    }

    private static AnimationClip CreateClip(string controllerPath, string name, Color color, float duration, bool loop, float squash)
    {
        AnimationClip clip = new AnimationClip { name = name, frameRate = 30 };
        AnimationCurve r = AnimationCurve.Constant(0, duration, color.r);
        AnimationCurve g = AnimationCurve.Constant(0, duration, color.g);
        AnimationCurve b = AnimationCurve.Constant(0, duration, color.b);
        AnimationCurve a = AnimationCurve.Constant(0, duration, 1f);
        AnimationCurve sx = AnimationCurve.Constant(0, duration, VisualWidth * squash);
        AnimationCurve sy = AnimationCurve.Constant(0, duration, VisualHeight * (2f - squash));
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.r", r);
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.g", g);
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.b", b);
        clip.SetCurve("", typeof(SpriteRenderer), "m_Color.a", a);
        clip.SetCurve("", typeof(Transform), "localScale.x", sx);
        clip.SetCurve("", typeof(Transform), "localScale.y", sy);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.AddObjectToAsset(clip, controllerPath);
        return clip;
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `PlaceholderAssetBuilder.cs`.

- [ ] **Step 3: Run both build menu items**

```
mcp__UnityMCP__execute_menu_item(menu_path="Tools/The Last Aethon/Build Placeholder Sprite")
mcp__UnityMCP__execute_menu_item(menu_path="Tools/The Last Aethon/Build Player Animator Controller")
```

- [ ] **Step 4: Verify the generated assets**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none. Call `mcp__UnityMCP__manage_asset(action="get_info", path="Assets/Art/Placeholder/WhiteSquare.png")` and confirm it resolves as a `Sprite`-typed texture. Call `mcp__UnityMCP__manage_asset(action="get_info", path="Assets/Animations/Player/PlayerAnimatorController.controller")` and confirm it resolves as an `AnimatorController`.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Editor/PlaceholderAssetBuilder.cs" "Assets/Art/Placeholder/WhiteSquare.png" "Assets/Art/Placeholder/WhiteSquare.png.meta" "Assets/Animations/Player/PlayerAnimatorController.controller" "Assets/Animations/Player/PlayerAnimatorController.controller.meta"
git commit -m "Generate placeholder sprite and Player Animator Controller via Editor build script"
```

---

### Task 6: Player GameObject assembly

**Files:**
- Modify: `ProjectSettings/TagManager.asset` (add a `Ground` user layer at index 6 — confirmed empty slot)
- Modify: `Assets/Scenes/Game.unity` (adds the `Player` hierarchy)

**Interfaces:**
- Produces: a `Player` root GameObject (`Rigidbody2D`, `BoxCollider2D`, `PlayerHealth`, `PlayerController`, `DebugDamageTrigger`) with children `GroundCheck` (empty Transform) and `Visual` (`SpriteRenderer` + `Animator`). Produces the `Ground` layer (bit `64`, i.e. `1 << 6`) that Task 7's level geometry assigns its colliders to, matched by `PlayerController.groundLayer` set here.
- Consumes: `TheLastAethon.Gameplay.PlayerHealth`/`PlayerController`/`DebugDamageTrigger` (Tasks 2–4), `Assets/Input/PlayerControls.inputactions` (Task 1), `Assets/Art/Placeholder/WhiteSquare.png` + `Assets/Animations/Player/PlayerAnimatorController.controller` (Task 5).

- [ ] **Step 1: Add the Ground layer**

Read `ProjectSettings/TagManager.asset`. Find the `layers:` list — index 6 (the empty entry immediately after `UI`) is unused. Change that one line from `  - ` to `  - Ground`. Leave every other line unchanged.

- [ ] **Step 2: Refresh and confirm the layer registered**

```
mcp__UnityMCP__refresh_unity
```

Re-read `ProjectSettings/TagManager.asset` and confirm line still reads `  - Ground` at the same position (Unity does not rewrite this file on refresh, but confirm no merge/format surprises).

- [ ] **Step 3: Confirm Game is the active scene**

Call `mcp__UnityMCP__manage_scene(action="get_active")`; if not `Game`, call `mcp__UnityMCP__manage_scene(action="load", scene_name="Game", scene_path="Assets/Scenes/Game.unity")`.

- [ ] **Step 4: Create the Player root GameObject**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="Player",
  components_to_add=["Rigidbody2D", "BoxCollider2D", "TheLastAethon.Gameplay.PlayerHealth", "TheLastAethon.Gameplay.PlayerController", "TheLastAethon.Gameplay.DebugDamageTrigger"],
  position=[1.2, 0.8, 0],
  component_properties={
    "Rigidbody2D": {"gravityScale": 1, "freezeRotation": true},
    "BoxCollider2D": {"size": {"x": 0.6, "y": 1.6}, "offset": {"x": 0, "y": 0}}
  }
)
```

Note the returned instance ID as `<playerId>`.

- [ ] **Step 5: Create the GroundCheck child**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="GroundCheck",
  parent="Player",
  position=[1.2, 0.0, 0]
)
```

(World position `[1.2, 0.0, 0]` = local `(0, -0.8, 0)` relative to `Player` at `(1.2, 0.8, 0)` — feet of the 1.6-tall collider.) Note the returned instance ID as `<groundCheckId>`.

- [ ] **Step 6: Create the Visual child**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="Visual",
  parent="Player",
  components_to_add=["SpriteRenderer", "Animator"],
  position=[1.2, 0.8, 0],
  scale=[0.6, 1.6, 1],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png"},
    "Animator": {"runtimeAnimatorController": "Assets/Animations/Player/PlayerAnimatorController.controller"}
  }
)
```

Note the returned instance ID as `<visualId>`.

- [ ] **Step 7: Wire PlayerController's fields**

```
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.PlayerController", property="inputActions", value="Assets/Input/PlayerControls.inputactions")
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.PlayerController", property="visualSprite", value="<visualId>")
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.PlayerController", property="animator", value="<visualId>")
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.PlayerController", property="groundCheck", value="<groundCheckId>")
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.PlayerController", property="groundLayer", value=64)
```

(`64` = `1 << 6`, the `Ground` layer added in Step 1.)

- [ ] **Step 8: Wire DebugDamageTrigger's fields**

```
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.DebugDamageTrigger", property="inputActions", value="Assets/Input/PlayerControls.inputactions")
mcp__UnityMCP__manage_components(action="set_property", target="<playerId>", component_type="TheLastAethon.Gameplay.DebugDamageTrigger", property="target", value="<playerId>")
```

(`target` is typed `PlayerHealth` — passing the `Player` GameObject's ID resolves to its `PlayerHealth` component, same convention as Task 5/Task 8 of sub-project #1's plan.)

- [ ] **Step 9: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Assets/Scenes/Game.unity` raw YAML directly (do not rely on tool "success" responses — see `feedback_unity_mcp_trust_but_verify`). Confirm: `Player` has a `Rigidbody2D`, a `BoxCollider2D` with `m_Size: {x: 0.6, y: 1.6}`, and the three Gameplay script components; `Visual`'s `m_LocalScale` is `{x: 0.6, y: 1.6, z: 1}` (not `{1,1,1}` — this is exactly the kind of silent property-apply failure the sub-project #1 retro caught); `PlayerController`'s serialized `groundLayer` bitmask is `64`.

- [ ] **Step 10: Verify no console errors, then commit**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

```bash
git add "ProjectSettings/TagManager.asset" "Assets/Scenes/Game.unity"
git commit -m "Assemble Player GameObject with physics, visual child, and wired scripts"
```

---

### Task 7: Level geometry — ground, platforms, boundary walls

**Files:**
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: a `Ground` GameObject and five `Platform1`–`Platform5` GameObjects (each `SpriteRenderer` + `BoxCollider2D`, layer `Ground`), plus `BoundaryLeft`/`BoundaryRight` (`BoxCollider2D` only, layer `Default`). No script depends on these by name — `PlayerController`'s `Physics2D.OverlapCircle` ground check (Task 4/6) matches them purely by layer.
- Consumes: `Assets/Art/Placeholder/WhiteSquare.png` (Task 5), the `Ground` layer (Task 6 Step 1).

- [ ] **Step 1: Confirm Game is the active scene**

Call `mcp__UnityMCP__manage_scene(action="get_active")`; load it if not active.

- [ ] **Step 2: Create the Ground GameObject**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="Ground",
  layer="Ground",
  position=[38.4, -0.1, 0],
  scale=[76.8, 0.2, 1],
  components_to_add=["SpriteRenderer", "BoxCollider2D"],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png", "color": {"r": 0.1019608, "g": 0.1803922, "b": 0.1019608, "a": 1}},
    "BoxCollider2D": {"size": {"x": 1, "y": 1}}
  }
)
```

(`BoxCollider2D.size` stays `{1,1}` — the collider is in the GameObject's own local space, which is already scaled `76.8 × 0.2` by the transform, exactly matching the stretched sprite.)

- [ ] **Step 3: Create the five platforms**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="Platform1",
  layer="Ground",
  position=[5.0, 1.2, 0],
  scale=[2.0, 0.14, 1],
  components_to_add=["SpriteRenderer", "BoxCollider2D"],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png", "color": {"r": 0.1647059, "g": 0.2274510, "b": 0.1647059, "a": 1}},
    "BoxCollider2D": {"size": {"x": 1, "y": 1}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="Platform2",
  layer="Ground",
  position=[9.0, 1.8, 0],
  scale=[1.6, 0.14, 1],
  components_to_add=["SpriteRenderer", "BoxCollider2D"],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png", "color": {"r": 0.1647059, "g": 0.2274510, "b": 0.1647059, "a": 1}},
    "BoxCollider2D": {"size": {"x": 1, "y": 1}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="Platform3",
  layer="Ground",
  position=[13.0, 1.0, 0],
  scale=[2.2, 0.14, 1],
  components_to_add=["SpriteRenderer", "BoxCollider2D"],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png", "color": {"r": 0.1647059, "g": 0.2274510, "b": 0.1647059, "a": 1}},
    "BoxCollider2D": {"size": {"x": 1, "y": 1}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="Platform4",
  layer="Ground",
  position=[17.0, 1.6, 0],
  scale=[1.8, 0.14, 1],
  components_to_add=["SpriteRenderer", "BoxCollider2D"],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png", "color": {"r": 0.1647059, "g": 0.2274510, "b": 0.1647059, "a": 1}},
    "BoxCollider2D": {"size": {"x": 1, "y": 1}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="Platform5",
  layer="Ground",
  position=[21.0, 1.3, 0],
  scale=[2.0, 0.14, 1],
  components_to_add=["SpriteRenderer", "BoxCollider2D"],
  component_properties={
    "SpriteRenderer": {"sprite": "Assets/Art/Placeholder/WhiteSquare.png", "color": {"r": 0.1647059, "g": 0.2274510, "b": 0.1647059, "a": 1}},
    "BoxCollider2D": {"size": {"x": 1, "y": 1}}
  }
)
```

- [ ] **Step 4: Create the boundary walls**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="BoundaryLeft",
  position=[-0.25, 5, 0],
  scale=[0.5, 20, 1],
  components_to_add=["BoxCollider2D"],
  component_properties={"BoxCollider2D": {"size": {"x": 1, "y": 1}}}
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="BoundaryRight",
  position=[77.05, 5, 0],
  scale=[0.5, 20, 1],
  components_to_add=["BoxCollider2D"],
  component_properties={"BoxCollider2D": {"size": {"x": 1, "y": 1}}}
)
```

- [ ] **Step 5: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Assets/Scenes/Game.unity` raw YAML. Confirm `Ground` and all five `Platform*` GameObjects carry `m_Layer: 6` (the `Ground` layer's index) — not `m_Layer: 0`, a silent-default-application failure this project has seen before. Confirm `BoundaryLeft`/`BoundaryRight` exist with the exact scale/position values above.

- [ ] **Step 6: Verify no console errors, then commit**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

```bash
git add "Assets/Scenes/Game.unity"
git commit -m "Add ground, five platforms, and boundary walls to Game scene"
```

---

### Task 8: Cinemachine camera follow

**Files:**
- Modify: `Packages/manifest.json` (add `com.unity.cinemachine`)
- Modify: `Assets/Scenes/Game.unity` (Main Camera → orthographic + `CinemachineBrain`; new `PlayerFollowCamera` GameObject)

**Interfaces:**
- Produces: Main Camera (`orthographic: 1`) with a `CinemachineBrain`, blended by a new `PlayerFollowCamera` GameObject (`CinemachineCamera` + `CinemachineFollow`) whose `Follow` target is the `Player` GameObject (Task 6). No script in this plan references Cinemachine types — this task is scene/package wiring only.

- [ ] **Step 1: Add the Cinemachine package dependency**

Edit `Packages/manifest.json`: inside `"dependencies"`, add `"com.unity.cinemachine": "3.1.4",` as a new line (alphabetically, right after `"com.unity.collab-proxy"` and before `"com.unity.ide.rider"`, matching this file's existing alphabetical ordering).

- [ ] **Step 2: Resolve the package**

```
mcp__UnityMCP__refresh_unity
```

Wait for the Editor to finish compiling, then call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect no errors. Call `mcp__UnityMCP__manage_asset(action="get_info", path="Packages/manifest.json")` or re-read `Packages/packages-lock.json` to confirm `com.unity.cinemachine` resolved to a concrete version (if Package Manager resolved a different compatible version than `3.1.4`, that's fine — note the actual resolved version for the commit message in Step 7).

- [ ] **Step 3: Confirm Game is the active scene, find Main Camera**

Call `mcp__UnityMCP__manage_scene(action="get_active")`; load `Game` if needed. Call `mcp__UnityMCP__manage_scene(action="get_hierarchy")` and locate `Main Camera`'s instance ID (call it `<mainCameraId>`) — if the tool exposes a direct find-by-name action (e.g. `manage_gameobject(action="find", name="Main Camera")`), use that instead.

- [ ] **Step 4: Convert Main Camera to orthographic and add CinemachineBrain**

```
mcp__UnityMCP__manage_components(action="set_property", target="<mainCameraId>", component_type="Camera", property="orthographic", value=true)
mcp__UnityMCP__manage_gameobject(action="modify", target="<mainCameraId>", components_to_add=["Unity.Cinemachine.CinemachineBrain"])
```

(If `manage_gameobject` doesn't support an `action="modify"` for adding a component to an existing object, use `mcp__UnityMCP__manage_components(action="add", target="<mainCameraId>", component_type="Unity.Cinemachine.CinemachineBrain")` instead.)

- [ ] **Step 5: Create the CinemachineCamera following the Player**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="PlayerFollowCamera",
  components_to_add=["Unity.Cinemachine.CinemachineCamera", "Unity.Cinemachine.CinemachineFollow"]
)
```

Note the returned instance ID as `<followCamId>`. Then wire it (using `<playerId>` from Task 6 Step 4):

```
mcp__UnityMCP__manage_components(action="set_property", target="<followCamId>", component_type="Unity.Cinemachine.CinemachineCamera", property="Follow", value="<playerId>")
mcp__UnityMCP__manage_components(action="set_property", target="<followCamId>", component_type="Unity.Cinemachine.CinemachineCamera", property="Lens.OrthographicSize", value=5)
mcp__UnityMCP__manage_components(action="set_property", target="<followCamId>", component_type="Unity.Cinemachine.CinemachineFollow", property="FollowOffset", value={"x": 0, "y": 1, "z": -10})
mcp__UnityMCP__manage_components(action="set_property", target="<followCamId>", component_type="Unity.Cinemachine.CinemachineFollow", property="Damping", value={"x": 0.5, "y": 0.5, "z": 0})
```

If any property name above doesn't match the live component's actual serialized field name, inspect the component via the MCP resource/inspection action available at execution time, adjust the property path, and re-apply — the target end-state (Follow = Player, orthographic lens size 5, offset `(0, 1, -10)`, damping `(0.5, 0.5, 0)`) is fixed regardless of exact property path.

- [ ] **Step 6: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Assets/Scenes/Game.unity` raw YAML. Confirm Main Camera's `Camera` component shows `orthographic: 1` and now has a `CinemachineBrain` `MonoBehaviour` entry among its components. Confirm `PlayerFollowCamera` exists with `CinemachineCamera` and `CinemachineFollow` components and that the `CinemachineCamera`'s Follow reference resolves to the `Player` GameObject's fileID (not `{fileID: 0}`, which would mean the wiring silently failed).

- [ ] **Step 7: Verify no console errors, then commit**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none.

```bash
git add "Packages/manifest.json" "Packages/packages-lock.json" "Assets/Scenes/Game.unity"
git commit -m "Add Cinemachine package and player-follow camera"
```

---

### Task 9: PlayerHUD + end-to-end Play Mode verification

**Files:**
- Create: `Assets/Scripts/UI/PlayerHUD.cs`
- Modify: `Assets/Scenes/Game.unity`

**Interfaces:**
- Produces: `TheLastAethon.UI.PlayerHUD`, a `MonoBehaviour` with serialized `playerHealth` (`PlayerHealth`), `hpFillImage` (`Image`), `hpLabel` (`TextMeshProUGUI`). Reads `PlayerHealth.Hp`/`MaxHp` every frame, sets `hpFillImage.fillAmount` and color per the spec's exact thresholds (`pct > 0.5` → `#C0392B`, `pct > 0.25` → `#E67E22`, else `#E74C3C`), and updates the label text.
- Consumes: `TheLastAethon.Gameplay.PlayerHealth.Hp`/`MaxHp` (Task 2), the `Player` GameObject (Task 6).

- [ ] **Step 1: Create PlayerHUD.cs**

Call `mcp__UnityMCP__manage_script` with `action="create"`, `name="PlayerHUD"`, `path="Assets/Scripts/UI"`, `contents`:

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

        private static readonly Color HighColor = new Color(0.7529412f, 0.2235294f, 0.1686275f);
        private static readonly Color MidColor = new Color(0.9019608f, 0.4941176f, 0.1333333f);
        private static readonly Color LowColor = new Color(0.9058824f, 0.2980392f, 0.2352941f);

        private void Update()
        {
            float pct = (float)playerHealth.Hp / playerHealth.MaxHp;
            hpFillImage.fillAmount = pct;
            hpFillImage.color = pct > 0.5f ? HighColor : pct > 0.25f ? MidColor : LowColor;
            hpLabel.text = $"{playerHealth.Hp} / {playerHealth.MaxHp}";
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

Call `mcp__UnityMCP__read_console(action="get", types=["error"])` — expect none referencing `PlayerHUD.cs`.

- [ ] **Step 3: Confirm Game is active, build the HUD canvas**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="HUDCanvas",
  components_to_add=["Canvas", "CanvasScaler", "GraphicRaycaster", "TheLastAethon.UI.PlayerHUD"],
  component_properties={
    "Canvas": {"renderMode": 0, "sortingOrder": 10},
    "CanvasScaler": {"uiScaleMode": 1, "referenceResolution": {"x": 1920, "y": 1080}, "screenMatchMode": 0}
  }
)
```

Note the returned instance ID as `<hudCanvasId>`.

- [ ] **Step 4: Create the HP bar background and fill**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="HPBarBackground",
  parent="HUDCanvas",
  components_to_add=["Image"],
  component_properties={
    "Image": {"color": {"r": 0.15, "g": 0.15, "b": 0.15, "a": 0.8}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 20, "y": -20}, "sizeDelta": {"x": 300, "y": 30}}
  }
)

mcp__UnityMCP__manage_gameobject(
  action="create", name="HPBarFill",
  parent="HPBarBackground",
  components_to_add=["Image"],
  component_properties={
    "Image": {"type": 3, "fillMethod": 0, "fillOrigin": 0, "fillAmount": 1, "color": {"r": 0.7529412, "g": 0.2235294, "b": 0.1686275, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 0}, "anchorMax": {"x": 1, "y": 1}, "offsetMin": {"x": 0, "y": 0}, "offsetMax": {"x": 0, "y": 0}}
  }
)
```

(`Image.type: 3` = Filled, `fillMethod: 0` = Horizontal, `fillOrigin: 0` = Left.) Note `HPBarFill`'s instance ID as `<hpFillId>`.

- [ ] **Step 5: Create the HP label**

```
mcp__UnityMCP__manage_gameobject(
  action="create", name="HPLabel",
  parent="HUDCanvas",
  components_to_add=["TextMeshProUGUI"],
  component_properties={
    "TextMeshProUGUI": {"text": "100 / 100", "fontSize": 20, "color": {"r": 1, "g": 1, "b": 1, "a": 1}},
    "RectTransform": {"anchorMin": {"x": 0, "y": 1}, "anchorMax": {"x": 0, "y": 1}, "pivot": {"x": 0, "y": 1}, "anchoredPosition": {"x": 20, "y": -55}, "sizeDelta": {"x": 300, "y": 30}}
  }
)
```

Note the returned instance ID as `<hpLabelId>`.

- [ ] **Step 6: Wire PlayerHUD's fields**

Using `<playerId>` from Task 6 Step 4:

```
mcp__UnityMCP__manage_components(action="set_property", target="<hudCanvasId>", component_type="TheLastAethon.UI.PlayerHUD", property="playerHealth", value="<playerId>")
mcp__UnityMCP__manage_components(action="set_property", target="<hudCanvasId>", component_type="TheLastAethon.UI.PlayerHUD", property="hpFillImage", value="<hpFillId>")
mcp__UnityMCP__manage_components(action="set_property", target="<hudCanvasId>", component_type="TheLastAethon.UI.PlayerHUD", property="hpLabel", value="<hpLabelId>")
```

- [ ] **Step 7: Save and independently re-verify**

```
mcp__UnityMCP__manage_scene(action="save")
```

Re-read `Assets/Scenes/Game.unity` raw YAML. Confirm `PlayerHUD`'s three fields resolve to non-zero fileIDs (not `{fileID: 0}`), and `HPBarFill`'s `RectTransform` anchors are `{0,0}`/`{1,1}` (stretched to fill its parent) and not the mistranscribed-localScale pattern this project saw in sub-project #1.

- [ ] **Step 8: End-to-end Play Mode verification**

```
mcp__UnityMCP__manage_editor(action="play")
```

Confirm via the Game view / a screenshot (`manage_camera` screenshot action): the Player (tinted square) stands on the ground; the HP bar reads `100 / 100` and is full. Then exercise each behavior in turn, checking `read_console(action="get", types=["error"])` after each:
- Move left/right (A/D or arrows) — Player's visual tints/squashes per Idle→Walk transition, moves at the walk speed, and the camera smoothly follows.
- Hold Left Shift while moving — Player moves faster (run speed) and the Run tint shows.
- Press Space — Player jumps onto/over a platform; the Jump tint/stretch shows mid-air and clears on landing.
- Press Left Mouse Button or Z while grounded — Attack tint/squash plays for ~0.5s, then returns to Idle/Walk/Run; movement is locked during it.
- Press H — Hurt tint flashes, Player gets knocked back (opposite its last move direction) and a small upward pop, HP bar drops by 10 and shifts color once at the `#E67E22` (HP ≤ 50) and `#E74C3C` (HP ≤ 25) thresholds across repeated presses, and the HP number updates.
- Stop pressing H and wait — after ~5 seconds HP regenerates by 5 (confirm visually or by re-reading the displayed number across two samples ~5s apart).
- Walk into `BoundaryLeft`/`BoundaryRight` — Player cannot pass beyond the world edges.

Then `mcp__UnityMCP__manage_editor(action="stop")`. If any step fails, check in order: (a) the failing component's wiring from Tasks 6/7/8/9 (re-read the relevant GameObject's raw YAML), (b) `read_console` for a silent runtime exception, (c) the Animator Controller's parameter names (Task 5) against `PlayerController`'s `Animator.StringToHash` calls (Task 4).

- [ ] **Step 9: Commit**

```bash
git add "Assets/Scripts/UI/PlayerHUD.cs" "Assets/Scenes/Game.unity"
git commit -m "Add PlayerHUD and complete end-to-end gameplay verification"
```

---

## Self-Review Notes

- **Spec coverage:** `PlayerController` movement/run/jump/attack-trigger (Task 4) ✓; `PlayerHealth` HP/regen/hurt/knockback ported 1:1 from original values (Task 2) ✓; 6-state Animator Controller with placeholder tint/squash (Task 5) ✓; HP HUD with corrected (non-green) color thresholds (Task 9) ✓; ground + 5 platforms at original-proportional layout (Task 7) ✓; Cinemachine camera follow + Main Camera orthographic conversion (Task 8) ✓; `Gameplay` map bindings + new `Debug` map with `DamageTest` (Task 1) ✓; manual Play Mode testing approach, no UTF (Task 9 Step 8, no automated-test task added) ✓; fixed `BoxCollider2D` size across all states, no per-animation hitbox resize (Task 6 — one collider size, never modified by Tasks 1–9) ✓, matching the spec's explicitly-called-out deviation.
- **Placeholder scan:** every numeric constant (speeds, durations, colors, positions, sizes, layer index, package version) is an exact value, not a TBD — the one explicitly-flagged exception is Task 8 Step 2's package version, which is pinned to `3.1.4` but allowed to resolve differently by Package Manager since that's normal package-resolution behavior, not an unspecified requirement.
- **Type/name consistency check:** `Animator.StringToHash` parameter names in `PlayerController.cs` (Task 4: `Speed`, `IsRunning`, `IsGrounded`, `Attack`, `Hurt`) match `AnimatorController.AddParameter` calls in `PlaceholderAssetBuilder.cs` (Task 5) exactly, including type (float/bool/bool/trigger/trigger). `PlayerHealth.TakeDamage(int amount)` (Task 2) matches the call in `DebugDamageTrigger.cs` (Task 3) and is never given a second, differently-shaped overload. `PlayerController`'s serialized field names (`visualSprite`, `animator`, `groundCheck`, `groundLayer`, `inputActions`) match the `set_property` calls in Task 6 Step 7 exactly. The `Ground` layer index (`6`, bitmask `64`) is consistent between Task 6 Step 1 (TagManager edit) and Steps 7 (PlayerController wiring) and Task 7 (platform/ground layer assignment).
- **Deviation from spec, called out explicitly:** the spec describes returning from Attack/Hurt to Idle "via animation-complete" without specifying the mechanism. This plan uses a timer (`attackDuration`/`hurtDuration` fields, matched 1:1 by each clip's exact length in Task 5) rather than Animator Events, because Animator Event wiring has no confirmed MCP tool support and a timer is simpler, fully deterministic, and produces the same observable behavior (player regains control when the clip's worth of time has elapsed).
- **Tooling-risk note (not a spec deviation):** the UnityMCP server is disconnected in the authoring session that wrote this plan, so several tool calls (`manage_gameobject(action="modify"...)`, Cinemachine property paths in Task 8, a possible `find`-by-name action in Task 8 Step 3) are best-effort based on the confirmed patterns from sub-project #1's plan, with explicit fallback instructions where the exact call shape is uncertain. The implementer subagent, with a live Editor connection, should adjust call syntax as needed and re-run this task's verification step before moving on — the fixed, non-negotiable part of each task is the target end-state (exact component, property, and value), not the literal tool invocation.
