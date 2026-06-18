using UnityEngine;
using UnityEngine.UI;

namespace TheLastAethon.UI
{
    [RequireComponent(typeof(Graphic))]
    public class BlinkingText : MonoBehaviour
    {
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float cycleDuration = 0.8f;

        private Graphic targetGraphic;

        private void Awake()
        {
            targetGraphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * (2f * Mathf.PI / cycleDuration)) + 1f) * 0.5f;
            Color color = targetGraphic.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            targetGraphic.color = color;
        }
    }
}
