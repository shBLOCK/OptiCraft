using Unity.Mathematics;
using UnityEngine;

namespace level {
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour {
        public float moveSpeed = 5f;
        public float sprintSpeed = 15f;
        public float rotationSpeed = 0.2f;

        private InputSystem_Actions inputActions;

        private float2 rotation;

        private void Awake() {
            inputActions = new InputSystem_Actions();
            inputActions.Enable();
            rotation = ((float3)transform.localEulerAngles).xy;
        }

        private void Update() {
            float2 lookVec = inputActions.Player.CameraLook.ReadValue<Vector2>() * rotationSpeed;

            rotation += lookVec.yx * new float2(-1f, 1f);
            rotation.x = math.clamp(rotation.x, -89.9f, 89.9f);
            transform.localEulerAngles = new float3(rotation, 0f);

            var moveVec = inputActions.Player.CameraMove.ReadValue<Vector3>();
            var spring = inputActions.Player.CameraSprint.IsPressed();
            moveVec *= (spring ? sprintSpeed : moveSpeed) * Time.deltaTime;
            moveVec = transform.localRotation * moveVec;
            transform.localPosition += moveVec;
        }
    }
}