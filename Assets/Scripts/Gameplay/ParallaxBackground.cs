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

        private float[] tileWidth;
        private Material[] materials;
        private float lastCameraX;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                Debug.LogError("ParallaxBackground: cameraTransform is not assigned.", this);
                enabled = false;
                return;
            }

            tileWidth = new float[layers.Length];
            materials = new Material[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                SpriteRenderer renderer = layers[i].renderer;
                if (renderer == null)
                {
                    continue;
                }

                tileWidth[i] = renderer.size.x;
                materials[i] = renderer.material;
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

                Transform layerTransform = renderer.transform;
                layerTransform.position += new Vector3(deltaX, 0f, 0f);

                float uvDelta = (deltaX * layers[i].scrollFactor) / tileWidth[i];
                Vector2 offset = materials[i].mainTextureOffset;
                offset.x += uvDelta;
                materials[i].mainTextureOffset = offset;
            }

            lastCameraX = currentCameraX;
        }
    }
}
