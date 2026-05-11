namespace core {
    public class DiodeMirrorDevice : MirrorLikeDevice {
        protected override void onBeamHitMirror(Beam beam, AxisDirection reflectDir, bool isFrontSide) {
            beam.consume();
            if (isFrontSide) {
                beam.userData_Beam = new Beam(space, reflectDir, gridPos, beam.image).emit();
            }
        }

        public override void onBeamEnd(Beam beam) {
            if (beam.userData_Beam != null) {
                beam.userData_Beam.stopEmit();
            }
        }
    }
}