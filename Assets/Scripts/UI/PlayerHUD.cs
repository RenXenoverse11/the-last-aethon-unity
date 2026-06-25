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
