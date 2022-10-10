// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using FMODUnity;
using FMOD.Studio;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Ambience Systems/Multi Direction Output")]
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class MultiDirectionOutput : MonoBehaviour
    {
        public Collider outputArea;
        public AudioActorTag playerAudioActorTag;
        public MultiDirectionFeeder feeder;

        [EventRef]
        public string outputEvent;

        private EventDescription outputEventDescription;
        private EventInstance outputEventInstance;
        public float MaxDistance { get; private set; }
        public float SpatializerAttenuation { get; private set; }

        private bool listenerIsInside = false;
        private bool systemIsActive = true;

        private Collider playerCollider;
        private bool playerColliderStored = false;

        private bool initializationSuccesfull = false;

        [Space(10)]

        // visualize the calculated closest point on output area 
        public bool debugDraw = false;
        // for weighing debugging in editor
        public string debugVolume = "";

        private void Awake()
        {
            if (outputArea == null)
                return;

            if (string.IsNullOrEmpty(outputEvent))
                return;

            outputEventDescription = FMODUnity.RuntimeManager.GetEventDescription(outputEvent);

            if (!outputEventDescription.isValid())
                return;

            bool is3D;
            outputEventDescription.is3D(out is3D);

            if (!is3D)
                return;

            if (playerAudioActorTag == null)
            {
                return;
            }

            outputEventDescription.getMinMaxDistance(out float minDistance, out float maxDist);
            MaxDistance = maxDist;

            if (MaxDistance == 0)
                return;

            initializationSuccesfull = true;
        }

        void Update()
        {
            if (systemIsActive)
            {
                UpdateProtocol();
            }
        }

        private void UpdateProtocol()
        {
            if (!initializationSuccesfull)
                return;

            if (listenerIsInside)
            {
                SpatializerAttenuation = 1;

                if (!outputEventInstance.isValid())
                {
                    StartSound();
                }
                else
                {
                    UpdateEventPosition(playerAudioActorTag.transform.position);
                }
            }
            else
            {
                Vector3 closestPoint = FindClosestPoint();
                float distanceToClosestPoint = Vector3.Distance(playerAudioActorTag.transform.position, closestPoint);

                #if UNITY_EDITOR
                if (debugDraw)
                {
                    DebugDraw(playerAudioActorTag.transform.position, closestPoint);
                }
                #endif

                if (distanceToClosestPoint < MaxDistance)
                {
                    SpatializerAttenuation = 1- (distanceToClosestPoint / MaxDistance);

                    if (!outputEventInstance.isValid())
                    {
                        StartSound(closestPoint);
                    }
                    else
                    {
                        UpdateEventPosition(closestPoint);
                    }
                }
                else
                {
                    StopSound(FMOD.Studio.STOP_MODE.IMMEDIATE);
                }
            }
        }

        private Vector3 FindClosestPoint()
        {
            Vector3 closestPoint = outputArea.ClosestPoint(playerAudioActorTag.transform.position);

            return closestPoint;
        }

        private void UpdateEventPosition(Vector3 position)
        {
            if (outputEventInstance.isValid())
            {
                outputEventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            }
        }

        private void StartSound(Vector3 closestPoint = default)
        {
            outputEventDescription.createInstance(out outputEventInstance);

            if (outputEventInstance.isValid())
            {
                if (listenerIsInside)
                {
                    UpdateEventPosition(playerAudioActorTag.transform.position);
                }
                else
                {
                    UpdateEventPosition(closestPoint);
                }

                outputEventInstance.start();
                
                if (feeder != null)
                {
                    feeder.ReportOutputStart(this);
                }
            }
        }

        private void StopSound(FMOD.Studio.STOP_MODE stopMode)
        {
            if (outputEventInstance.isValid())
            {
                outputEventInstance.stop(stopMode);
                outputEventInstance.release();
                
                if (feeder != null)
                {
                    feeder.ReportOutputStop(this);
                }
            }     
        }

        public void SetVolume (float volume)
        {
            if (outputEventInstance.isValid())
            {
                outputEventInstance.setVolume(volume);

                #if UNITY_EDITOR
                debugVolume = volume.ToString();
                #endif
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!initializationSuccesfull)
                return;

            if (!playerColliderStored)
            {
                var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

                if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
                {
                    playerCollider = other;
                    playerColliderStored = true;
                    listenerIsInside = true;
                }
            }
            else
            {
                if (other == playerCollider)
                {
                    listenerIsInside = true;
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!initializationSuccesfull)
                return;

            if (!playerColliderStored)
            {
                var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

                if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
                {
                    playerCollider = other;
                    playerColliderStored = true;
                    listenerIsInside = false;
                }
            }
            else
            {
                if (other == playerCollider)
                {
                    listenerIsInside = false;
                }
            }
        }

        private void DebugDraw(Vector3 a, Vector3 b)
        {
            Debug.DrawLine(a, b, Color.magenta);
        }

        public void SetSystemActive()
        {
            systemIsActive = true;
        }

        public void SetSystemInactive()
        {
            if (feeder != null)
            {
                feeder.ReportOutputStop(this);
            }

            StopSound(FMOD.Studio.STOP_MODE.IMMEDIATE);

            systemIsActive = false;
        }

        public void SetParameter(string parameterName, float parameterValue)
        {
            if (outputEventInstance.isValid())
            {
                FMOD.RESULT result = outputEventInstance.setParameterByName(parameterName, parameterValue);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError("FMOD error: " + result);
                }
            }
        }

        void Reset()
        {
            outputArea = gameObject.GetComponent<Collider>();

            if (outputArea != null)
            {
                outputArea.isTrigger = true;
            }

            var rb = gameObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnDestroy()
        {
            SetSystemInactive();
        }
    }
}