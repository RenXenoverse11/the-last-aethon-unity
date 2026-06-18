using TheLastAethon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastAethon.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Start()
        {
            startButton.onClick.AddListener(HandleStart);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(HandleStart);
        }

        private void HandleStart()
        {
            GameManager.Instance.LoadGame();
        }
    }
}
