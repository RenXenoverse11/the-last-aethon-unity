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
