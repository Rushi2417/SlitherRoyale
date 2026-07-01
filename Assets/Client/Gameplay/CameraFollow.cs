using UnityEngine;

namespace SlitherRoyale.Client.Gameplay
{
    public class CameraFollow : MonoBehaviour
    {
        private Camera _cam;
        [SerializeField] private float baseOrthoSize = 60f;
        [SerializeField] private float sizePerMass = 0.08f;
        [SerializeField] private float maxOrthoSize = 150f;
        [SerializeField] private float smoothTime = 0.3f;

        private Vector3 _velocity;
        private float _sizeVelocity;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        public void SetTarget(float x, float y, float mass)
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            Vector3 targetPos = new Vector3(x, y, _cam.transform.position.z);
            _cam.transform.position = Vector3.SmoothDamp(
                _cam.transform.position, targetPos, ref _velocity, smoothTime);

            float targetSize = Mathf.Min(baseOrthoSize + mass * sizePerMass, maxOrthoSize);
            _cam.orthographicSize = Mathf.SmoothDamp(
                _cam.orthographicSize, targetSize, ref _sizeVelocity, 0.5f);
        }
    }
}
