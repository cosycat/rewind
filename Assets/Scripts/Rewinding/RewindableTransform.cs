using System;
using UnityEngine;

namespace Rewinding {
    
    public class RewindableTransform : RewindableObject<(Vector3 pos, Quaternion rot)> {
        
        protected override (Vector3 pos, Quaternion rot) GetFrameInfo() {
            var t = transform;
            return (t.position, t.rotation);
        }

        protected override void RestoreFrame((Vector3 pos, Quaternion rot) frame) {
            var t = transform;
            t.position = frame.pos;
            t.rotation = frame.rot;
        }

        protected override bool IsChangedFromFrame((Vector3 pos, Quaternion rot) frame) {
            var t = transform;
            return t.position != frame.pos || t.rotation != frame.rot;
        }

        protected override bool IsChangedFromPreviousFrame() {
            return transform.hasChanged;
        }

        protected virtual void LateUpdate() {
            transform.hasChanged = false;
        }

        public override void Pause() {
            if (TryGetComponent<Rigidbody>(out var rigidbody)) {
                rigidbody.isKinematic = true;
            }
        }
        
        public override void Unpause() {
            if (TryGetComponent<Rigidbody>(out var rigidbody)) {
                rigidbody.isKinematic = false;
            }
        }
    }
}