using System;
using System.Text.Json.Nodes;
using core;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace device {
    //TODO: cleanup
    public class BeamRegister : SimpleGridDevice {
        private AxisDirection direction;
        private ushort inputBeam = Beam.INVALID_ID;
        private ushort outputBeam = Beam.INVALID_ID;

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (beam.direction == direction) {
                inputBeam = beam.id;
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            if (beam.direction == direction) {
                inputBeam = Beam.INVALID_ID;
            }
        }

        public override void onBeamIdChanged(ref Beam beam, Beam.End beamEnd, ushort newId) {
            if (beamEnd == Beam.End.Head) {
                inputBeam.replaceThis(beam.id, newId);
            } else {
                outputBeam.replaceThis(beam.id, newId);
            }
        }

        private bool condition() => isInSpace && space.simulator.tickNumber % 100 == 0;
        
        public override void tick() {
            if (condition()) {
                if (outputBeam != Beam.INVALID_ID) {
                    space.stopEmitBeam(outputBeam);
                    outputBeam = Beam.INVALID_ID;
                }

                if (inputBeam != Beam.INVALID_ID) {
                    outputBeam = space.emitBeam(new Beam(gridPos, direction, space.getBeam(inputBeam).image)).id;
                }
            }
        }

        public override void onRemoved() {
            if (inputBeam != Beam.INVALID_ID) space.stopConsumeBeam(inputBeam);
            if (outputBeam != Beam.INVALID_ID) space.stopEmitBeam(outputBeam);

            base.onRemoved();
        }

        public override void reset() {
            base.reset();
            inputBeam = Beam.INVALID_ID;
            outputBeam = Beam.INVALID_ID;
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["direction"] = direction.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            direction = Enum.Parse<AxisDirection>(data["direction"].GetValue<string>());
        }

        public override void userActionRotate(AxisDirection axis) {
            direction = direction.rotate(axis);
        }

        public override void render() {
            base.render();
            DebugUtils.drawBoundsWireframe(new Bounds(new float3(gridPos) + direction.float3(0.75f), new float3(0.1f)), Color.red);
            DebugUtils.drawBoundsWireframe(new Bounds(new float3(gridPos) + direction.float3(-0.75f), new float3(0.1f)), Color.green);
            if (condition()) {
                DebugUtils.drawBoundsWireframe(new Bounds(new float3(gridPos), new float3(1.8f)), Color.green);
            }
        }

        private static readonly OCDeviceType<BeamRegister> _TYPE = new("beam_register");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}