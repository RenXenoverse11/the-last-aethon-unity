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