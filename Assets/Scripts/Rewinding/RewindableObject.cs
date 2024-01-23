using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rewinding {
    public abstract class RewindableObject : MonoBehaviour {
        /// <summary>
        /// Resumes recording the state of the object every frame.
        /// </summary>
        public abstract void StartRecording();

        /// <summary>
        /// Pauses the game object in time.
        /// </summary>
        public abstract void Pause();
        
        /// <summary>
        /// Unpauses the game object in time.
        /// </summary>
        public abstract void Unpause();

        /// <summary>
        /// Restores the state of the object to the state it was in <paramref name="skipFrames"/> + 1 frames ago.
        ///
        /// If <paramref name="skipFrames"/> is 0, the state of the object will be restored to the state it was in the previous frame.
        /// If <paramref name="skipFrames"/> is 1, the state of the object will be restored to the state it was in two frames ago.
        /// If <paramref name="skipFrames"/> is 2, the state of the object will be restored to the state it was in three frames ago.
        /// etc.
        ///
        /// If <paramref name="skipFrames"/> is greater than the number of frames that have been recorded, the state of the object will be restored to the state it was in the first frame. (TODO)
        /// </summary>
        /// <param name="skipFrames"> The number of frames to skip before restoring the state of the previous frame. </param>
        public abstract void RestorePreviousFrame(int skipFrames = 0);
        
        /// <summary>
        /// Restores the state of the object to the state it was in <paramref name="skipFrames"/> + 1 frames in the future.
        ///
        /// Only works there are frames in the future to restore to.
        /// </summary>
        /// <param name="skipFrames"> The number of frames to skip before restoring the state of the next frame. </param>
        public abstract void RestoreNextFrame(int skipFrames = 0);
            
        /// <summary>
        /// Clears all recorded frames.
        /// </summary>
        public abstract void ClearAllFrames();
        
    }
    
    public abstract class RewindableObject<T> : RewindableObject {
        
        private RewindController _rewindController;
        private RewindMode RewindMode => _rewindController.Mode;
        private bool IsRecording => _rewindController.IsRecording;
        private bool IsRewinding => _rewindController.IsRewinding;
        private bool IsForwarding => _rewindController.IsForwarding;
        private bool IsPaused => _rewindController.IsPaused;

        private readonly List<(int equalCount, T frame)> _previousFrames = new(); // TODO maybe use a class here to be able to increment equalCount without creating a new tuple
        private readonly List<(int equalCount, T frame)> _nextFrames = new();

        protected virtual void Awake() {
            Debug.Log($"Awake {name} in RewindableObject abstract class");
            Initialize();
            Debug.Assert(_rewindController != null, $"RewindController was not initialized in {name}");
        }

        private void Start() {
            Debug.Assert(_rewindController != null, $"RewindController was not initialized in Start of {name} {this.GetType().Name}");
        }

        protected virtual void Initialize() {
            _rewindController = RewindController.Register(this);
        }

        protected void Update() {
            if (IsRecording) {
                SaveFrame();
            }
        }

        private void SaveFrame() {
            Debug.Assert(IsRecording, "Can't save frame while not recording");
            if (_previousFrames.Count > 0 && !IsChangedFromPreviousFrame()) {
                _previousFrames[^1] = (_previousFrames[^1].equalCount + 1, _previousFrames[^1].frame);
                return;
            }
            _previousFrames.Add((1, GetFrameInfo()));
        }
        
        public override void StartRecording() {
            Debug.Assert(IsRecording);
            if (_previousFrames.Count == 0) {
                SaveFrame(); // TODO check if this is necessary
            }
            _nextFrames.Clear(); // we don't want to be able to redo frames after starting a new recording
            Unpause();
        }
        
        public override void ClearAllFrames() {
            _previousFrames.Clear();
        }

        public override void RestorePreviousFrame(int skipFrames = 0) {
            Debug.Log($"Restoring previous frame in {name}, skipFrames: {skipFrames}");
            Debug.Assert(!IsRecording, "Can't restore frame while recording");
            // Debug.Assert(_previousFrames.Aggregate(0, (acc, f) => acc + f.equalCount) == _rewindController.FramesToRewindCount, "Can't skip more frames than there are saved"); // TODO this is not very efficient, remove this check if it becomes a problem
            Debug.Assert(skipFrames >= 0, "Can't skip negative number of frames");
            
            var (equalCount, frame) = _previousFrames[^1];
            while (equalCount <= skipFrames) {
                Debug.Assert(equalCount > 0, "Should never have a frame with equalCount == 0");
                skipFrames -= equalCount;
                _nextFrames.Add((equalCount, frame));
                _previousFrames.RemoveAt(_previousFrames.Count - 1);
                (equalCount, frame) = _previousFrames[^1];
            }
            Debug.Assert(equalCount > skipFrames, "Sanity check failed, after the while loop, this should always be true");
            
            _nextFrames.Add((skipFrames + 1, frame));
            if (equalCount - skipFrames - 1 > 0) {
                _previousFrames[^1] = (equalCount - (skipFrames + 1), frame);
            }
            else {
                _previousFrames.RemoveAt(_previousFrames.Count - 1);
            }
            RestoreFrame(frame);
        }
        
        // TODO maybe combine this with RestorePreviousFrame
        public override void RestoreNextFrame(int skipFrames = 0) {
            Debug.Log($"Restoring next frame in {name}, skipFrames: {skipFrames}");
            Debug.Assert(!IsRecording, "Can't restore frame while recording");
            // Debug.Assert(_nextFrames.Aggregate(0, (acc, f) => acc + f.equalCount) == _rewindController.FramesToForwardCount, "Can't skip more frames than there are saved"); // TODO this is not very efficient, remove this check if it becomes a problem
            Debug.Assert(skipFrames >= 0, "Can't skip negative number of frames");
            
            var (equalCount, frame) = _nextFrames[^1];
            while (equalCount <= skipFrames) {
                Debug.Assert(equalCount > 0, "Should never have a frame with equalCount == 0");
                skipFrames -= equalCount;
                _previousFrames.Add((equalCount, frame));
                _nextFrames.RemoveAt(_nextFrames.Count - 1);
                (equalCount, frame) = _nextFrames[^1];
            }
            Debug.Assert(equalCount > skipFrames, "Sanity check failed, after the while loop, this should always be true");
            
            _previousFrames.Add((skipFrames + 1, frame));
            if (equalCount - skipFrames - 1 > 0) {
                _nextFrames[^1] = (equalCount - (skipFrames + 1), frame);
            }
            else {
                _nextFrames.RemoveAt(_nextFrames.Count - 1);
            }
            RestoreFrame(frame);
        }
        
        protected abstract void RestoreFrame(T frame);
        
        protected abstract T GetFrameInfo();

        protected abstract bool IsChangedFromFrame(T frame);
        
        protected virtual bool IsChangedFromPreviousFrame() {
            return IsChangedFromFrame(_previousFrames[^1].frame);
        }
        
    }
}