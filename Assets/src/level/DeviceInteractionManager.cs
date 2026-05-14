using System;
using core;
using device;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

namespace level {
    public class DeviceInteractionManager : MonoBehaviour {
        private Simulator simulator;
        private InputSystem_Actions inputActions;
        private CommandBuffer cmds;
        
        private void Awake() {
            simulator = GetComponent<Simulator>();
            inputActions = new InputSystem_Actions();
            inputActions.Enable();
            cmds = new CommandBuffer();
        }
        
        private OCDevice hoveredDevice;
        private AxisDirection hoveredDeviceNormal;
        private OCDevice grabbedDevice = null;

        public void _OnGUI() {
            if (grabbedDevice == null) {
                if (GUILayout.Button("Mirror")) {
                    grabbedDevice = new MirrorDevice();
                }

                if (GUILayout.Button("BeamSourceXG")) {
                    grabbedDevice = new BeamSourceDevice() {
                        image = Resources.Load<Texture2D>("x_gradient")
                    };
                }
                if (GUILayout.Button("BeamSourceYG")) {
                    grabbedDevice = new BeamSourceDevice() {
                        image = Resources.Load<Texture2D>("y_gradient")
                    };
                }
            }
        }

        private void Update() {
            var mouseRay = Camera.main.ScreenPointToRay(inputActions.UI.Point.ReadValue<Vector2>());
            hoveredDevice = null;
            if (grabbedDevice == null) {
                foreach (var device in simulator.rootSpace.enumerateDevices()) {
                    if (device.getVisualBox().intersectRay(mouseRay, out var normal)) {
                        hoveredDevice = device;
                        hoveredDeviceNormal = normal;
                        break;
                    }
                }
            }

            if (grabbedDevice != null) {
                grabbedDevice.render(cmds);
                
                if (inputActions.Player.DeviceRotateCCW.triggered) grabbedDevice.userActionRotate(AxisDirection.PosY);
                if (inputActions.Player.DeviceRotateCW.triggered) grabbedDevice.userActionRotate(AxisDirection.NegY);

                if (new Plane(new float3(0f, 1f, 0f), 0f).Raycast(mouseRay, out var dist)) {
                    var pos = mouseRay.GetPoint(dist);
                    if (grabbedDevice is SimpleGridDevice simpleGridDevice) {
                        simpleGridDevice._tmp_setGridPos(new int3(pos));
                    }
                }
                
                if (inputActions.Player.DeviceGrab.triggered) {
                    simulator.rootSpace.addDevice(grabbedDevice);
                    grabbedDevice = null;
                }

                if (inputActions.Player.DeviceDelete.triggered) {
                    grabbedDevice = null;
                }
            }

            if (hoveredDevice != null) {
                var bounds = hoveredDevice.getVisualBox();
                bounds.debugDraw(Color.HSVToRGB(Time.time, 1f, 0.5f));

                AxisDirection? rotationAxis = null;
                if (inputActions.Player.DeviceRotateCCW.triggered) rotationAxis = hoveredDeviceNormal;
                if (inputActions.Player.DeviceRotateCW.triggered) rotationAxis = hoveredDeviceNormal.opposite();

                if (rotationAxis != null || inputActions.Player.DeviceGrab.triggered) {
                    simulator.rootSpace.removeDevice(hoveredDevice);

                    if (rotationAxis != null) {
                        hoveredDevice.userActionRotate(rotationAxis.Value);
                    }

                    if (inputActions.Player.DeviceGrab.triggered) {
                        grabbedDevice = hoveredDevice;
                    }

                    if (grabbedDevice == null) {
                        simulator.rootSpace.addDevice(hoveredDevice);
                    }
                }
            }
            
            Graphics.ExecuteCommandBuffer(cmds);
            cmds.Clear();
        }
    }
}