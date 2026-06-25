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
