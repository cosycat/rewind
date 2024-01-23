using UnityEngine;

namespace Rewinding {
    
    [RequireComponent(typeof(Rigidbody)),
     RequireComponent(typeof(RewindableTransform)),
     DisallowMultipleComponent]
    public class RewindableRigidbody : RewindableObject<(Vector3 vel, Vector3 angVel)> {
        
        private Rigidbody _rigidbody;
        private bool _wasKinematic;
        
        private void Awake() {
            _rigidbody = GetComponent<Rigidbody>();
            _wasKinematic = _rigidbody.isKinematic;
        }

        protected override (Vector3 vel, Vector3 angVel) GetFrameInfo() {
            return (_rigidbody.velocity, _rigidbody.angularVelocity);
        }

        protected override bool IsChangedFromFrame((Vector3 vel, Vector3 angVel) frame) {
            return _rigidbody.velocity != frame.vel || _rigidbody.angularVelocity != frame.angVel;
        }

        protected override void RestoreFrame((Vector3 vel, Vector3 angVel) frame) {
            _rigidbody.velocity = frame.vel; // TODO could also only get the frame from _nextFrames in StartRecording and do nothing here
            _rigidbody.angularVelocity = frame.angVel;
        }

        protected override void StartRecording() {
            base.StartRecording();
            _rigidbody.isKinematic = _wasKinematic;
        }
        
        public override void PauseRecording() {
            base.PauseRecording();
            _wasKinematic = _rigidbody.isKinematic;
            _rigidbody.isKinematic = true;
        }
    }
}