using UnityEngine;

namespace TheLastAethon.Gameplay
{
    public class ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public struct Layer
        {
            public SpriteRenderer renderer;
            public float scrollFactor;
        }

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Layer[] layers;

        private float lastCameraX;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                Debug.LogError("ParallaxBackground: cameraTransform is not assigned.", this);
                enabled = false;
                return;
            }

            lastCameraX = cameraTransform.position.x;
        }

        private void LateUpdate()
        {
            float currentCameraX = cameraTransform.position.x;
            float deltaX = currentCameraX - lastCameraX;

            for (int i = 0; i < layers.Length; i++)
            {
                SpriteRenderer renderer = layers[i].renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.transform.position += new Vector3(deltaX * layers[i].scrollFactor, 0f, 0f);
            }

            lastCameraX = currentCameraX;
        }
    }
}
