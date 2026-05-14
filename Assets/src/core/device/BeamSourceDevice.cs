// using System;
// using Unity.Mathematics;
// using UnityEngine;
//
// namespace core {
//     [RequireComponent(typeof(GridPosition))]
//     public sealed class BeamSourceDevice : Device {
//         [SerializeField] private Texture2D image = null;
//         [SerializeField] private float4 color = 1f;
//         [SerializeField] private AxisDirection direction = AxisDirection.PosZ;
//
//         private Beam beam = null;
//
//         public override void reset() {
//             beam = null;
//         }
//
//         public override void postTick() {
//             if (beam == null) {
//                 BeamImage beamImage;
//                 if (!image) {
//                     beamImage = BeamImage.singlePixel(color);
//                 } else {
//                     beamImage = new(BeamImageData.fromTexture2D(image), color);
//                 }
//                 beam = new Beam(space, direction, gridPos.offset(direction), beamImage).emit();
//             }
//         }
//
//         private void Update() {
//             //TODO: tmp
//             updateRendering();
//         }
//         
//         private void OnValidate() {
//             updateRendering();
//         }
//
//         private void updateRendering() {
//             transform.localRotation = Quaternion.FromToRotation(Vector3.forward, direction.float3());
//         }
//     }
// }