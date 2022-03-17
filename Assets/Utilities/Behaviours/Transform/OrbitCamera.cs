using UnityEngine;

namespace Rhinox.Utilities
{
    public class OrbitCamera : MonoBehaviour
    {
        public Transform Target;

        public float Distance = 20.0f;

        public float ZoomStep = 1.0f;

        public Vector2 PanSpeed = Vector2.one;

        private Vector3 _distanceVector;

        // The position of the cursor on the screen. Used to rotate the camera.
        private Vector2 _cursorPosition = Vector2.zero;

        // Move the camera to its initial position.
        private void Start()
        {
            _distanceVector = new Vector3(0.0f, 0.0f, -Distance);

            _cursorPosition = transform.localEulerAngles;

            Rotate(_cursorPosition);
        }

        //Rotate the camera or zoom depending on the input of the player.
        private void LateUpdate()
        {
            RotateControls();
            Zoom();
        }

        // Rotate the camera when the first button of the mouse is pressed.
        private void RotateControls()
        {
            if (Input.GetMouseButton(0))
            {
                _cursorPosition += GetMouseDelta() * PanSpeed;

                Rotate(_cursorPosition);
            }
        }

        // TODO create InputUtility and make this work with new input system
        private Vector2 GetMouseDelta()
        {
            return new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
        }

        // Transform the cursor mouvement in rotation and in a new position
        // for the camera.
        private void Rotate(Vector2 pos)
        {
            // Transform angle in degree in quaternion form used by Unity for rotation.
            Quaternion rotation = Quaternion.Euler(pos.y, pos.x, 0.0f);

            // The new position is the target position + the distance vector of the camera
            // rotated at the specified angle.
            Vector3 position = rotation * _distanceVector + TargetPosition;

            // Update the rotation and position of the camera.
            transform.rotation = rotation;
            transform.position = position;
        }

        private Vector3 TargetPosition => Target ? Target.position : Vector3.zero;

        // Zoom or dezoom depending on the input of the mouse wheel.
        private void Zoom()
        {
            float axis = Input.mouseScrollDelta.y;
            if (axis < 0.0f)
                ZoomOut();
            else if (axis > 0.0f)
                ZoomIn();
        }

        // Reduce the distance from the camera to the target and
        // position of the camera (with the Rotate function).
        private void ZoomIn()
        {
            Distance -= ZoomStep;
            _distanceVector = new Vector3(0.0f, 0.0f, -Distance);
            Rotate(_cursorPosition);
        }

        // Increase the distance from the camera to the target and
        // update the position of the camera (with the Rotate function).
        private void ZoomOut()
        {
            Distance += ZoomStep;
            _distanceVector = new Vector3(0.0f, 0.0f, -Distance);
            Rotate(_cursorPosition);
        }
    }
}