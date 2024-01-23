using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

namespace Rewinding {
    
    public class RewindController : MonoBehaviour {

        #region Static Controller Management

        private static List<RewindController> RewindControllers { get; } = new();

        public static void Register([NotNull] RewindableObject rewindableObject, int id = 0) {
            Debug.Assert(id >= 0, "id must be greater than or equal to 0");
            if (id >= RewindControllers.Count) {
                RewindControllers.AddRange(new RewindController[id - RewindControllers.Count + 1]);
            }
            
            RewindControllers[id] ??= (id == 0 ? new GameObject("RewindController") : RewindControllers[0].gameObject).AddComponent<RewindController>();
            RewindControllers[id].RegisterObject(rewindableObject);
        }
        
        #endregion


        private readonly List<RewindableObject> _rewindableObjects = new();
        private int _rewindSpeed = 1;
        private int _framesToRewindCount = 0;
        private int _framesToForwardCount = 0;
        private RewindMode _rewindMode = RewindMode.Pause;

        public bool IsRecording => _rewindMode == RewindMode.Record;
        public bool IsRewinding => _rewindMode == RewindMode.Rewind;
        public bool IsForwarding => _rewindMode == RewindMode.Forward;
        public bool IsPaused => _rewindMode == RewindMode.Pause;

        private void RegisterObject([NotNull] RewindableObject rewindableObject) {
            Debug.Assert(!_rewindableObjects.Contains(rewindableObject), "rewindableObject is already registered");
            _rewindableObjects.Add(rewindableObject);
        }

        private void Update() {
            switch (_rewindMode) {
                case RewindMode.Rewind:
                    _framesToForwardCount += _rewindSpeed;
                    _framesToRewindCount -= _rewindSpeed;
                    foreach (var rewindableObject in _rewindableObjects) {
                        rewindableObject.RestorePreviousFrame(_rewindSpeed - 1);
                    }
                    if (_framesToRewindCount <= 0) {
                        _framesToForwardCount -= _framesToRewindCount;
                        _framesToRewindCount = 0;
                        _rewindMode = RewindMode.Pause;
                    }
                    break;
                case RewindMode.Forward:
                    _framesToForwardCount -= _rewindSpeed;
                    _framesToRewindCount += _rewindSpeed;
                    foreach (var rewindableObject in _rewindableObjects) {
                        rewindableObject.RestoreNextFrame(_rewindSpeed - 1);
                    }
                    if (_framesToForwardCount <= 0) {
                        _framesToRewindCount -= _framesToForwardCount;
                        _framesToForwardCount = 0;
                        _rewindMode = RewindMode.Pause;
                    }
                    break;
                case RewindMode.Pause:
                    break;
                case RewindMode.Record:
                    _framesToRewindCount++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RestartRecording() {
            _rewindMode = RewindMode.Record;
            _framesToRewindCount = 0;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.PauseRecording();
                rewindableObject.ClearAllFrames();
                rewindableObject.ResumeRecording();
            }
        }

        public void PauseRecording() {
            _rewindMode = RewindMode.Pause;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.PauseRecording();
            }
        }

        public void ResumeRecording() {
            _rewindMode = RewindMode.Record;
            _framesToRewindCount = 0;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.ResumeRecording();
            }
        }

        public void StartRewinding(int rewindSpeed = 1) {
            if (IsRecording)
                PauseRecording();
            _rewindSpeed = rewindSpeed;
            _rewindMode = RewindMode.Rewind;
        }
        
        public void StartForwarding() {
            if (IsRecording)
                PauseRecording();
            _rewindMode = RewindMode.Forward;
        }

        public enum RewindMode {
            Rewind,
            Forward,
            Pause,
            Record
        }
        
    }
    
}