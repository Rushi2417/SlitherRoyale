using UnityEngine;

namespace SlitherRoyale.Client.Gameplay
{
    public static class InputHandler
    {
        private static Vector2 _steerDirection;
        private static bool _boostHeld;

        public static void Update()
        {
            _steerDirection = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    Vector3 touchWorld = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10f));
                    _steerDirection = (touchWorld - Camera.main.transform.position).normalized;
                }

                _boostHeld = Input.touchCount >= 2;
            }
            else
            {
                Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 camPos = Camera.main.transform.position;
                _steerDirection = ((Vector3)mouseWorld - camPos).normalized;

                _boostHeld = Input.GetMouseButton(1);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) _steerDirection += Vector2.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) _steerDirection += Vector2.down;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) _steerDirection += Vector2.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) _steerDirection += Vector2.right;
            _boostHeld = _boostHeld || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);

            if (_steerDirection.magnitude > 0.01f)
                _steerDirection.Normalize();
            else
                _steerDirection = Vector2.right;
        }

        public static Vector2 GetSteerDirection() => _steerDirection;
        public static bool IsBoosting() => _boostHeld;
    }
}
