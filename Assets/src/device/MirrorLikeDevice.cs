using System;
using System.Text.Json.Nodes;
using core;
using utils;

namespace device {
    public abstract class MirrorLikeDevice : SimpleGridDevice {
        protected MirrorDirection mirrorDir;

        public override void onBeamHitEdge(ref Beam beam) {
            if (beam.direction.axis() != mirrorDir.dirA().axis() && beam.direction.axis() != mirrorDir.dirB().axis()) {
                space.consumeBeam(beam.id);
            }
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["mirrorDir"] = mirrorDir.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            mirrorDir = Enum.Parse<MirrorDirection>(data["mirrorDir"].GetValue<string>());
        }
    }
}