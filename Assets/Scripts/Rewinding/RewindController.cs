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
        internal int FramesToRewindCount { get; private set; } = 0;
        internal int FramesToForwardCount { get; private set; } = 0;

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
                    FramesToRewindCount++;
                    foreach (var rewindableObject in _rewindableObjects) {
                        rewindableObject.SaveFrame();
                        rewindableObject.DebugCheckFrameCount(FramesToRewindCount, FramesToForwardCount);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return;

            void Rewind() {
                Debug.Log($"RewindController::Rewind: FramesToRewindCount: {FramesToRewindCount}, FramesToForwardCount: {FramesToForwardCount}");
                FramesToForwardCount += _rewindSpeed;
                FramesToRewindCount -= _rewindSpeed;
                
                if (FramesToRewindCount <= 0) {
                    var framesTooMuch = -FramesToRewindCount;
                    var framesLeft = _rewindSpeed - framesTooMuch;
                    FramesToForwardCount -= framesTooMuch;
                    FramesToRewindCount = 0;
                    if (framesLeft > 0) {
                        foreach (var rewindableObject in _rewindableObjects) {
                            rewindableObject.RestorePreviousFrame(framesLeft - 1);
                            rewindableObject.DebugCheckFrameCount(FramesToRewindCount, FramesToForwardCount);
                        }
                    }

                    Mode = RewindMode.Pause;
                    Debug.Log($"RewindController::Rewind: Mode = RewindMode.Pause: FramesToRewindCount: {FramesToRewindCount}, FramesToForwardCount: {FramesToForwardCount}");
                    return;
                }
                
                foreach (var rewindableObject in _rewindableObjects) {
                    rewindableObject.RestorePreviousFrame(_rewindSpeed - 1);
                    rewindableObject.DebugCheckFrameCount(FramesToRewindCount, FramesToForwardCount);
                }
                
            }

            void Forward() {
                Debug.Log($"RewindController::Forward: FramesToRewindCount: {FramesToRewindCount}, FramesToForwardCount: {FramesToForwardCount}");
                FramesToForwardCount -= _rewindSpeed;
                FramesToRewindCount += _rewindSpeed;
                
                if (FramesToForwardCount <= 0) {
                    var framesTooMuch = -FramesToForwardCount;
                    var framesLeft = _rewindSpeed - framesTooMuch;
                    FramesToRewindCount -= framesTooMuch;
                    FramesToForwardCount = 0;
                    if (framesLeft > 0) {
                        foreach (var rewindableObject in _rewindableObjects) {
                            rewindableObject.RestoreNextFrame(framesLeft - 1);
                            rewindableObject.DebugCheckFrameCount(FramesToRewindCount, FramesToForwardCount);
                        }
                    }
                    
                    Mode = RewindMode.Pause;
                    Debug.Log($"RewindController::Forward: Mode = RewindMode.Pause: FramesToRewindCount: {FramesToRewindCount}, FramesToForwardCount: {FramesToForwardCount}");
                    return;
                }

                foreach (var rewindableObject in _rewindableObjects) {
                    rewindableObject.RestoreNextFrame(_rewindSpeed - 1);
                    rewindableObject.DebugCheckFrameCount(FramesToRewindCount, FramesToForwardCount);
                }
            }
        }

        public void RestartRecording() {
            Pause();
            Mode = RewindMode.Record;
            FramesToRewindCount = 0;
            FramesToForwardCount = 0;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.ClearAllFrames();
                rewindableObject.StartRecording();
            }
        }

        public void Pause() {
            if (IsPaused)
                return;
            Mode = RewindMode.Pause;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.Pause();
            }
        }

        public void StartRecording() {
            if (IsRecording)
                return;
            Pause();
            Mode = RewindMode.Record;
            FramesToForwardCount = 0;
            foreach (var rewindableObject in _rewindableObjects) {
                rewindableObject.StartRecording();
            }
        }

        public void StartRewinding(int rewindSpeed = 3) {
            if (IsRewinding)
                return;
            Pause();
            _rewindSpeed = rewindSpeed;
            Mode = RewindMode.Rewind;
        }
        
        public void StartForwarding() {
            if (IsForwarding)
                return;
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