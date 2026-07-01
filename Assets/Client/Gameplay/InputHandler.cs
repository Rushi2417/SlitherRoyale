using UnityEngine;

namespace SlitherRoyale.Client.Gameplay
{
    public static class InputHandler
    {
        private static Vector2 _steerDirection;
        private static bool _boostHeld;
        // BUG-06 FIX: renamed from _mobileBoostPressed (single-frame pulse) to
        // _mobileBoostHeld (persistent until PointerUp). Prevents boost feeling
        // unresponsive on mobile where only one frame was registered per tap.
        private static bool _mobileBoostHeld;
        private static Vector2 _lastDirection = Vector2.right;

        public static void Update(Vector2 wormPosition)
        {
            _steerDirection = Vector2.zero;

            Camera cam = Camera.main;
            if (cam == null) return;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    Vector3 touchWorld = cam.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, -cam.transform.position.z));
                    _steerDirection = ((Vector2)touchWorld - wormPosition).normalized;
                }
                // Two-finger = boost
                _boostHeld = Input.touchCount >= 2;
            }
            else
            {
                Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                _steerDirection = (mouseWorld - wormPosition).normalized;
                _boostHeld = Input.GetMouseButton(1);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    _steerDirection += Vector2.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  _steerDirection += Vector2.down;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  _steerDirection += Vector2.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) _steerDirection += Vector2.right;
            _boostHeld = _boostHeld || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);

            // BUG-06 FIX: _mobileBoostHeld persists until PointerUp — no single-frame consume
            _boostHeld = _boostHeld || _mobileBoostHeld;

            if (_steerDirection.magnitude > 0.01f)
            {
                _steerDirection.Normalize();
                _lastDirection = _steerDirection;
            }
            else
            {
                _steerDirection = _lastDirection;
            }
        }

        /// <summary>
        /// Called by the on-screen BOOST button via EventTrigger PointerDown/PointerUp.
        /// BUG-06 FIX: persistent hold state — true on PointerDown, false on PointerUp.
        /// </summary>
        public static void SetMobileBoostHeld(bool held) => _mobileBoostHeld = held;

        /// <summary>Called by the on-screen BOOST button in the HUD.</summary>
        [System.Obsolete("Use SetMobileBoostHeld instead (BUG-06 fix)")]
        public static void SetMobilBoostPressed(bool pressed) => _mobileBoostHeld = pressed;

        public static Vector2 GetSteerDirection() => _steerDirection;
        public static bool IsBoosting() => _boostHeld;
    }
}
