using Rewinding;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input {
    public class RewindInputController : MonoBehaviour
    {
        
        public void OnRewind(InputAction.CallbackContext context) {
            if (context.started) {
                RewindController.MainRewindController.StartRewinding();
            }
        }
        
        public void OnPause(InputAction.CallbackContext context) {
            if (!context.started)
                return;
            if (RewindController.MainRewindController.IsPaused) {
                RewindController.MainRewindController.StartRecording();
            } else {
                RewindController.MainRewindController.Pause();
            }
        }
        
        public void OnForward(InputAction.CallbackContext context) {
            if (context.started) {
                RewindController.MainRewindController.StartForwarding();
            }
        }
    
    }
}
