using UnityEngine;

namespace TheLastAethon.Gameplay
{
    public class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float fixedY = 8f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = target.position.x;
            position.y = fixedY;
            transform.position = position;
        }
    }
}
