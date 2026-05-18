using Unity.Mathematics;
using UnityEngine;

namespace level {
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour {
        public float moveSpeed = 0.05f;
        public float sprintSpeed = 0.15f;
        public float rotationSpeed = 0.2f;

        private InputSystem_Actions inputActions;

        private void Awake() {
            inputActions = new InputSystem_Actions();
            inputActions.Enable();
        }

        private void Update() {
            float2 lookVec = inputActions.Player.CameraLook.ReadValue<Vector2>() * rotationSpeed;
            var newEulerAngles = new float3(
                new float3(transform.localEulerAngles).xy + lookVec.yx * new float2(-1f, 1f),
                0f
            );
            newEulerAngles.x = math.clamp(newEulerAngles.x, -89.9f, 89.9f);
            transform.localEulerAngles = newEulerAngles;

            var moveVec = inputActions.Player.CameraMove.ReadValue<Vector3>();
            var spring = inputActions.Player.CameraSprint.IsPressed();
            moveVec *= spring ? sprintSpeed : moveSpeed;
            moveVec = transform.localRotation * moveVec;
            transform.localPosition += moveVec;
        }
    }
}