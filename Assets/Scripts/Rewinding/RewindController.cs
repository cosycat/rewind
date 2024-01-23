using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

namespace Rewinding {
    
    public class RewindController : MonoBehaviour {

        #region Static Controller Management

        public static RewindController MainRewindController { get; } = new GameObject("RewindController").AddComponent<RewindController>();
        
        public static RewindController Register([NotNull] RewindableObject rewindableObject) {
            MainRewindController.RegisterObject(rewindableObject);
            return MainRewindController;
        }
        
        // private static List<RewindController> RewindControllers { get; } = new();
        //
        // public static RewindController MainRewindController {
        //     get {
        //         if (RewindControllers.Count == 0) {
        //             Debug.Log("Creating RewindController in RewindController.MainRewindController");
        //             RewindControllers.Add(new GameObject("RewindController").AddComponent<RewindController>());
        //         }
        //         return RewindControllers[0];
        //     }
        // }
        //
        // public static RewindController Register([NotNull] RewindableObject rewindableObject, int id = 0) {
        //     Debug.Assert(id >= 0, "id must be greater than or equal to 0");
        //     if (id >= RewindControllers.Count) {
        //         RewindControllers.AddRange(new RewindController[id - RewindControllers.Count + 1]);
        //     }
        //     
        //     if (RewindControllers[id] == null) {
        //         Debug.Log($"Creating RewindController {id} in RewindController.Register");
        //         RewindControllers[id] = (id == 0 ? new GameObject("RewindController") : RewindControllers[0].gameObject).AddComponent<RewindController>();
        //     }
        //     RewindControllers[id].RegisterObject(rewindableObject);
        //     return RewindControllers[id];
        // }
        
        #endregion


        private readonly List<RewindableObject> _rewindableObjects = new();
        private int _rewindSpeed = 1;
        private int _framesToRewindCount = 0;
        private int _framesToForwardCount = 0;

        public bool IsRecording => Mode == RewindMode.Record;
        public bool IsRewinding => Mode == RewindMode.Rewind;
        public bool IsForwarding => Mode == RewindMode.Forward;
        public bool IsPaused => Mode == RewindMode.Pause;

        public RewindMode Mode { get; private set; } = RewindMode.Record;

        private void RegisterObject([NotNull] RewindableObject rewindableObject) {
            Debug.Assert(!_rewindableObjects.Contains(rewindableObject), "rewindableObject is already registered");
            _rewindableObjects.Add(rewindableObject);
        }

        private void Update() {
            switch (Mode) {
                case RewindMode.Rewind:
                    Rewind();
                    break;
                case RewindMode.Forward:
                    Forward();
                    break;
                case RewindMode.Pause:
                    break;
                case RewindMode.Record:
                    _framesToRewindCount++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return;

            void Rewind() {
                _framesToForwardCount += _rewindSpeed;
                _framesToRewindCount -= _rewindSpeed;
                if (_framesToRewindCount <= 0) {
                    _framesToForwardCount -= _framesToRewindCount;
                    _framesToRewindCount = 0;
                    Mode = RewindMode.Pause;
                    // TODO the last frame is not restored
                    return;
                }
                
                foreach (var rewindableObject in _rewindableObjects) {
                    rewindableObject.RestorePreviousFrame(_rewindSpeed - 1);
                }
                
            }

            void Forward() {
                _framesToForwardCount -= _rewindSpeed;
                _framesToRewindCount += _rewindSpeed;
                if (_framesToForwardCount <= 0) {
                    _framesToRewindCount -= _framesToForwardCount;
                    _framesToForwardCount = 0;
                    Mode = RewindMode.Pause;
                    // TODO the last frame is not restored
                    return;
                }

                foreach (var rewindableObject in _rewindableObjects) {
                    rewindableObject.RestoreNextFrame(_rewindSpeed - 1);
                }
            }
        }

        public void RestartRecording() {
            Mode = RewindMode.Record;
            _framesToRewindCount = 0;
            _framesToForwardCount = 0;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.Pause();
                rewindableObject.ClearAllFrames();
                rewindableObject.StartRecording();
            }
        }

        public void Pause() {
            Mode = RewindMode.Pause;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.Pause();
            }
        }

        public void StartRecording() {
            Mode = RewindMode.Record;
            _framesToRewindCount = 0;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.StartRecording();
            }
        }

        public void StartRewinding(int rewindSpeed = 1) {
            if (IsRecording)
                Pause();
            _rewindSpeed = rewindSpeed;
            Mode = RewindMode.Rewind;
        }
        
        public void StartForwarding() {
            if (IsRecording)
                Pause();
            Mode = RewindMode.Forward;
        }
        
    }
    
    public enum RewindMode {
        Rewind,
        Forward,
        Pause,
        Record
    }
    
}