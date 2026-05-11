using System;
using System.Collections.Generic;
using UnityEngine;

namespace core {
    [RequireComponent(typeof(GridPosition))]
    public class MirrorDevice : MirrorLikeDevice {
        [SerializeField] private bool doubleSided = true;

        protected override void onBeamHitMirror(Beam beam, AxisDirection reflectDir, bool isFrontSide) {
            if (isFrontSide || doubleSided) {
                beam.consume();
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