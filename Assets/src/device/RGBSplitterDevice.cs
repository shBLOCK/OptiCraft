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
    public class RGBSplitterDevice : SimpleGridDevice {
        public AxisDirection inputDirection = AxisDirection.NegZ;
        public AxisDirection redDirection = AxisDirection.NegX;

        private ushort inputBeam = Beam.INVALID_ID;
        private readonly ushort[] outputBeams = CollectionUtils.newFilledArray(3, Beam.INVALID_ID);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (beam.direction.opposite() == inputDirection) {
                inputBeam = beam.id;
                var greenDirection = inputDirection.opposite();
                var blueDirection = redDirection.opposite();
                outputBeams[0] = space
                    .emitBeam(new Beam(
                        gridPos, redDirection,
                        beam.image.modulated(new float4(1, 0, 0, 0))
                            .withOrientation(beam.image.orientation.reflect(beam.direction.opposite(), redDirection))
                    )).id;
                outputBeams[1] = space
                    .emitBeam(new Beam(gridPos, greenDirection, beam.image.modulated(new float4(0, 1, 0, 0)))).id;
                outputBeams[2] = space
                    .emitBeam(new Beam(
                        gridPos, blueDirection,
                        beam.image.modulated(new float4(0, 0, 1, 0))
                            .withOrientation(beam.image.orientation.reflect(beam.direction.opposite(), blueDirection))
                    )).id;
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            if (beam.direction.opposite() == inputDirection) {
                for (int i = 0; i < 3; i++) {
                    space.stopEmitBeam(outputBeams[i]);
                }
            }
        }

        public override void onBeamIdChanged(ref Beam beam, Beam.End beamEnd, ushort newId) {
            if (beamEnd == Beam.End.Head) {
                inputBeam.replaceThis(beam.id, newId);
            } else {
                outputBeams.replaceAll(beam.id, newId);
            }
        }

        public override void reset() {
            inputBeam = Beam.INVALID_ID;
            Array.Fill(outputBeams, Beam.INVALID_ID);
        }

        public override void onRemoved() {
            if (inputBeam != Beam.INVALID_ID) space.stopConsumeBeam(inputBeam);
            for (int i = 0; i < 3; i++) {
                if (outputBeams[i] != Beam.INVALID_ID) space.stopEmitBeam(outputBeams[i]);
            }

            reset();

            base.onRemoved();
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["inputDirection"] = inputDirection.ToString();
            data["redDirection"] = redDirection.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            inputDirection = Enum.Parse<AxisDirection>(data["inputDirection"].GetValue<string>());
            redDirection = Enum.Parse<AxisDirection>(data["redDirection"].GetValue<string>());
        }

        public override void render() {
            base.render();
            DebugUtils.drawBoundsWireframe(
                new Bounds(new float3(gridPos) + redDirection.float3(0.75f), new float3(0.1f)), Color.red);
            DebugUtils.drawBoundsWireframe(
                new Bounds(new float3(gridPos) + inputDirection.opposite().float3(0.75f), new float3(0.1f)),
                Color.green);
            DebugUtils.drawBoundsWireframe(
                new Bounds(new float3(gridPos) + redDirection.opposite().float3(0.75f), new float3(0.1f)), Color.blue);
            DebugUtils.drawBoundsWireframe(
                new Bounds(new float3(gridPos) + inputDirection.float3(0.75f), new float3(0.1f)), Color.white);
        }

        public override void userActionRotate(AxisDirection axis) {
            inputDirection = inputDirection.rotate(axis);
            redDirection = redDirection.rotate(axis);
        }

        private static readonly OCDeviceType<RGBSplitterDevice> _TYPE = new("rgb_splitter");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}