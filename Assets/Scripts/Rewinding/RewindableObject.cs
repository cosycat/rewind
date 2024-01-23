using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rewinding {
    public abstract class RewindableObject : MonoBehaviour {
        
        /// <summary>
        /// Pauses recording the state of the object every frame.
        /// </summary>
        public abstract void PauseRecording();
        
        /// <summary>
        /// Resumes recording the state of the object every frame.
        /// </summary>
        public abstract void ResumeRecording();
        
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
        
        private bool _isRecording = true;
        private readonly List<(int equalCount, T frame)> _previousFrames = new(); // TODO maybe use a class here to be able to increment equalCount without creating a new tuple
        private readonly List<(int equalCount, T frame)> _nextFrames = new();

        private void Awake() {
            RewindController.Register(this);
        }

        protected void Update() {
            if (_isRecording) {
                SaveFrame();
            }
        }

        private void SaveFrame() {
            Debug.Assert(_isRecording, "Can't save frame while not recording");
            if (_previousFrames.Count > 0 && !IsChangedFromPreviousFrame()) {
                _previousFrames[^1] = (_previousFrames[^1].equalCount + 1, _previousFrames[^1].frame);
                return;
            }
            _previousFrames.Add((1, GetFrameInfo()));
        }
        
        protected virtual void StartRecording() {
            _isRecording = true;
            if (_previousFrames.Count == 0) {
                SaveFrame(); // TODO check if this is necessary
            }
            _nextFrames.Clear(); // we don't want to be able to redo frames after starting a new recording
        }

        public override void PauseRecording() {
            _isRecording = false;
        }

        public override void ResumeRecording() {
            StartRecording();
        }
        
        public override void ClearAllFrames() {
            _previousFrames.Clear();
        }

        public override void RestorePreviousFrame(int skipFrames = 0) {
            Debug.Assert(!_isRecording, "Can't restore frame while recording");
            Debug.Assert(_previousFrames.Aggregate(0, (acc, f) => acc + f.equalCount) >= skipFrames, "Can't skip more frames than there are saved"); // TODO this is not very efficient, remove this check if it becomes a problem
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
            _nextFrames.Add((skipFrames, frame));
            _previousFrames[^1] = (equalCount - skipFrames, frame); // could be behind an if statement, but this might be more efficient, because of cpu branch prediction
            RestoreFrame(frame);
        }
        
        // TODO maybe combine this with RestorePreviousFrame
        public override void RestoreNextFrame(int skipFrames = 0) {
            Debug.Assert(!_isRecording, "Can't restore frame while recording");
            Debug.Assert(_nextFrames.Aggregate(0, (acc, f) => acc + f.equalCount) >= skipFrames, "Can't skip more frames than there are saved"); // TODO this is not very efficient, remove this check if it becomes a problem
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
            _previousFrames.Add((skipFrames, frame));
            _nextFrames[^1] = (equalCount - skipFrames, frame); // could be behind an if statement, but this might be more efficient, because of cpu branch prediction
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