// using System.Collections.Generic;
// using tmp;
// using UnityEngine;
//
// namespace core {
//     [RequireComponent(typeof(GridPosition))]
//     public class ModulatorDevice : Device {
//         [SerializeField] private ComputeShader cs;
//         
//         [SerializeField] private Axis modulatorAxis;
//         [SerializeField] private Axis modulatingAxis;
//         private Dictionary<AxisDirection, Beam> beams = new(); // TODO: optimize
//         private Dictionary<AxisDirection, Beam> emittedBeams = new();
//
//         private bool dirty = false;
//
//         public override void reset() {
//             dirty = false;
//             beams.Clear();
//             emittedBeams.Clear();
//         }
//
//         public override void onBeamHit(Beam beam) {
//             beam.consume();
//             if (beam.direction.axis() == modulatorAxis) {
//                 beams[beam.direction.opposite()] = beam;
//                 dirty = true;
//             } else if (beam.direction.axis() == modulatingAxis) {
//                 beams[beam.direction.opposite()] = beam;
//                 dirty = true;
//             } else {
//                 return;
//             }
//             //TODO: don't compute if didn't change
//         }
//
//         public override void postTick() {
//             if (dirty) {
//                 dirty = false;
//                 updateOutput(modulatingAxis.negDirection());
//                 updateOutput(modulatingAxis.posDirection());
//             }
//         }
//
//         private void updateOutput(AxisDirection outDirection) {
//             if (emittedBeams.TryGetValue(outDirection, out var old)) {
//                 old.stopEmit();
//             }
//
//             Beam modulatorA = null;
//             Beam modulatorB = null;
//             if (beams.TryGetValue(modulatorAxis.negDirection(), out modulatorA)) {
//                 modulatorB = beams.GetValueOrDefault(modulatorAxis.posDirection());
//             } else {
//                 modulatorA = beams.GetValueOrDefault(modulatorAxis.posDirection());
//             }
//             var modulatingBeam = beams.GetValueOrDefault(outDirection.opposite());
//             if (modulatingBeam == null) return;
//             BeamImage image;
//             if (modulatorA == null && modulatorB == null) {
//                 image = modulatingBeam.image;
//             } else {
//                 modulatingBeam.image._tmp_setToCS(cs, 0, "uModulating");
//                 modulatorA.image._tmp_setToCS(cs, 0, "uModulatorA");
//                 if (modulatorB != null) {
//                     cs.SetBool("uModulatorBActive", true);
//                     modulatorB.image._tmp_setToCS(cs, 0, "uModulatorB");
//                 } else {
//                     cs.SetBool("uModulatorBActive", false);
//                     cs.SetTexture(0, "uModulatorB", Texture2D.blackTexture);
//                 }
//                 image = new BeamImage(new BeamImageData(modulatingBeam.image.size), 1f);
//                 cs.SetTexture(0, "uOutput", image.data._tmp_getRT());
//             }
//             cs.Dispatch(0, image.size.x / 16, image.size.y / 16, 1);
//             emittedBeams[outDirection] = new Beam(space, modulatingBeam.direction, gridPos, image).emit();
//         }
//
//         public override void onBeamHitEdge(Beam beam) {
//             if (beam.direction.axis() != modulatorAxis && beam.direction.axis() != modulatingAxis) {
//                 beam.consume();
//             }
//         }
//
//         private void OnValidate() {
//             //TODO: rotate to align modulator axis
//             transform.localRotation = Quaternion.FromToRotation(Vector3.forward, modulatingAxis.float3());
//         }
//     }
// }