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
