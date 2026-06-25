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
