using UnityEngine;

namespace Rewinding {
    
    [RequireComponent(typeof(Rigidbody)),
     RequireComponent(typeof(RewindableTransform))]
    public class RewindableRigidbody : RewindableObject<(Vector3 vel, Vector3 angVel)> {
        
        private Rigidbody _rigidbody;
        private bool _wasKinematic;

        protected override void Awake() {
            base.Awake();
            Debug.Log($"Awake {name} in RewindableRigidbody");
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
            // TODO could also only get the frame from _nextFrames in StartRecording and do nothing here
            _rigidbody.AddForce(frame.vel - _rigidbody.velocity, ForceMode.VelocityChange); // TODO is the - rigidbody.velocity necessary?
            _rigidbody.AddTorque(frame.angVel, ForceMode.VelocityChange);
        }
        
        public override void Pause() {
            _rigidbody.isKinematic = true;
        }
        
        public override void Unpause() {
            _rigidbody.isKinematic = _wasKinematic;
        }
    }
}