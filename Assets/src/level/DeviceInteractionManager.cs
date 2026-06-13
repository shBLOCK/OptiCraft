using System;
using core;
using device;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace level {
    [RequireComponent(typeof(Simulator))]
    public class DeviceInteractionManager : MonoBehaviour {
        private Simulator simulator;
        private InputSystem_Actions inputActions;

        private void Awake() {
            simulator = GetComponent<Simulator>();
            inputActions = new InputSystem_Actions();
            inputActions.Enable();
        }

        private OCDevice hoveredDevice;
        private AxisDirection hoveredDeviceNormal;
        private OCDevice grabbedDevice = null;

        private string[] _tmp_beamSourceColor = CollectionUtils.newFilledArray<string>(6, "0");

        public void _OnGUI() {
            if (grabbedDevice == null) {
                foreach (var deviceType in OCDevice.TYPES) {
                    if (GUILayout.Button(deviceType.id)) {
                        grabbedDevice = deviceType.construct();
                    }
                }

                if (GUILayout.Button("BeamSourceXG")) {
                    grabbedDevice = new BeamSourceDevice() {
                        imagePath = "x_gradient"
                    };
                }

                if (GUILayout.Button("BeamSourceYG")) {
                    grabbedDevice = new BeamSourceDevice() {
                        imagePath = "y_gradient"
                    };
                }

                GUILayout.BeginHorizontal();
                for (int i = 0; i < 4; i++) {
                    _tmp_beamSourceColor[i] = GUILayout.TextField(_tmp_beamSourceColor[i]);
                }

                if (GUILayout.Button("BeamSrc")) {
                    float4 color = 0f;
                    for (int i = 0; i < 4; i++) {
                        color[i] = float.Parse(_tmp_beamSourceColor[i]);
                    }

                    grabbedDevice = new BeamSourceDevice() {
                        color = color
                    };
                }

                GUILayout.EndHorizontal();
            }
        }

        private const float DEVICE_BOUNDS_EXPANSION = 0.1f;

        private void Update() {
            var mouseOnGui = GUIUtility.hotControl != 0;

            var mouseRay = Camera.main.ScreenPointToRay(inputActions.UI.Point.ReadValue<Vector2>());
            hoveredDevice = null;
            if (grabbedDevice == null) {
                foreach (var device in simulator.rootSpace.enumerateDevices()) {
                    var bounds = device.getVisualBox();
                    bounds.Expand(DEVICE_BOUNDS_EXPANSION);
                    if (bounds.intersectRay(mouseRay, out var normal)) {
                        hoveredDevice = device;
                        hoveredDeviceNormal = normal;
                        break;
                    }
                }
            }

            if (grabbedDevice != null) {
                grabbedDevice.render();

                if (inputActions.Player.DeviceRotateCCW.triggered) grabbedDevice.userActionRotate(AxisDirection.NegY, inplace: false);
                if (inputActions.Player.DeviceRotateCW.triggered) grabbedDevice.userActionRotate(AxisDirection.PosY, inplace: false);

                if (new Plane(new float3(0f, 1f, 0f), new float3(0f, -1f, 0f)).Raycast(mouseRay, out var dist)) {
                    var pos = new float3(mouseRay.GetPoint(dist));
                    pos.y = 0f;
                    if (grabbedDevice is SimpleGridDevice simpleGridDevice) {
                        simpleGridDevice._tmp_setGridPos(simpleGridDevice.findGridPosForPlacement(pos));
                    }
                }

                if (inputActions.Player.DeviceGrab.triggered && !mouseOnGui) {
                    if (grabbedDevice is SimpleGridDevice simpleGridDevice) {
                        if (!simpleGridDevice.isCurrentGridPosValidInSpace(simulator.rootSpace)) goto skip;
                    }

                    simulator.rootSpace.addDevice(grabbedDevice);
                    grabbedDevice = null;
                    skip: ;
                }

                if (inputActions.Player.DeviceDelete.triggered) {
                    grabbedDevice = null;
                }
            }

            if (hoveredDevice != null) {
                var bounds = hoveredDevice.getVisualBox();
                bounds.Expand(DEVICE_BOUNDS_EXPANSION);
                DebugUtils.drawBoundsWireframe(bounds, Color.HSVToRGB(math.frac(Time.time), 1f, 0.8f));

                D.raw(new Shape.Text(bounds.center, hoveredDevice.TYPE.id, Camera.main), Color.gray);

                AxisDirection? rotationAxis = null;
                if (inputActions.Player.DeviceRotateCCW.triggered) rotationAxis = hoveredDeviceNormal.opposite();
                if (inputActions.Player.DeviceRotateCW.triggered) rotationAxis = hoveredDeviceNormal;

                if (rotationAxis != null || inputActions.Player.DeviceGrab.triggered && !mouseOnGui) {
                    simulator.rootSpace.removeDevice(hoveredDevice);

                    if (rotationAxis != null) {
                        hoveredDevice.userActionRotate(rotationAxis.Value, inplace: true);
                    }

                    if (inputActions.Player.DeviceGrab.triggered && !mouseOnGui) {
                        grabbedDevice = hoveredDevice;
                    }

                    if (grabbedDevice == null) {
                        simulator.rootSpace.addDevice(hoveredDevice);
                    }
                }
            }
        }
    }
}