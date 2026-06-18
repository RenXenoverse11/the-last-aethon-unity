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
