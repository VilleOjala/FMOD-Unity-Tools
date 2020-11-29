// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Ambience System/Spot Ambience")]
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public class SpotAmbience : MonoBehaviour
    {
        [FMODUnity.EventRef]
        public string spotAmbience;

        public float adjustMaxDistance = 20.0f;

        private SphereCollider sphereCollider = null;
        private Rigidbody rb = null;

        public bool spatialAudioRoomAware = false;

        [HideInInspector]
        public SpatialAudioRoom initialRoom = null; 

        private BaseAmbienceArea parentAmbienceArea = null;
        private FMOD.Studio.EventInstance spotAmbienceInstance;

        FMOD.Studio.EventDescription eventDescription;

        private bool isInitialized = false;
        private bool instanceAlreadyPlaying = false;

        void Awake()
        {
            if (sphereCollider != null)
            {
                sphereCollider.radius = adjustMaxDistance;
            }         
        }

        public bool InitializeSpotAmbience(BaseAmbienceArea parentAmbienceArea)
        {
            if (isInitialized)
            {
                Debug.LogWarning("Failed to initialize spot ambience for parent area "
                                 + parentAmbienceArea.gameObject.name + ". It has already been initialized by parent area "
                                 + parentAmbienceArea.gameObject.name + ".");
                return false;
            }

            if (string.IsNullOrEmpty(spotAmbience))
            {
                Debug.LogError("FMOD event reference is null or empty for Spot Ambience " + gameObject.name + ".");
                return false;
            }

            FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.getEvent(spotAmbience, out eventDescription);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError("FMOD event reference is not valid for spot ambience " + gameObject.name + ".");
                return false;
            }

            eventDescription.is3D(out bool is3D);

            if (!is3D)
            {
                Debug.LogError("FMOD event reference is not 3D for spot ambience " + gameObject.name + ".");
                return false;
            }

            this.parentAmbienceArea = parentAmbienceArea;

            isInitialized = true;
            return true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isInitialized)
                return;

            var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

            if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
            {
                if (parentAmbienceArea != null && parentAmbienceArea.BaseAmbienceIsValid() && !instanceAlreadyPlaying)
                {
                    spotAmbienceInstance = FMODUnity.RuntimeManager.CreateInstance(spotAmbience);

                    if (spotAmbienceInstance.isValid())
                    {
                        FMODUnity.RuntimeManager.AttachInstanceToGameObject(spotAmbienceInstance, gameObject.transform, rb);

                        int playbackPosition = parentAmbienceArea.GetBaseAmbiencePlaybackPosition();
                        spotAmbienceInstance.setTimelinePosition(playbackPosition);
                        spotAmbienceInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, adjustMaxDistance);

                        if (spatialAudioRoomAware && SpatialAudioManager.instance != null)
                        {
                            SpatialAudioManager.instance.RegisterRoomAwareInstance(spotAmbienceInstance, gameObject.transform,
                                                                                   adjustMaxDistance, initialRoom);
                        }

                        FMOD.RESULT result = spotAmbienceInstance.start();

                        if (result == FMOD.RESULT.OK)
                        {
                            instanceAlreadyPlaying = true;
                        }    
                    }                    
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!isInitialized)
                return;

            var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

            if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
            {
                if (instanceAlreadyPlaying && spotAmbienceInstance.isValid())
                {
                    spotAmbienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    spotAmbienceInstance.release();
                    instanceAlreadyPlaying = false;
                }
                else
                {
                    instanceAlreadyPlaying = false;
                }
            }
        }

        void Update()
        {
            if (spotAmbienceInstance.isValid() && parentAmbienceArea != null)
            {
                spotAmbienceInstance.setVolume(1 - parentAmbienceArea.InsideStatus);
            }         
        }

        void OnValidate()
        {
            if (adjustMaxDistance < 0)
                adjustMaxDistance = 0;

            if (sphereCollider != null)
            {
                sphereCollider.radius = adjustMaxDistance;
            }         
        }

        void Reset()
        {
            sphereCollider = gameObject.GetComponent<SphereCollider>();
            
            if (sphereCollider != null)
            {
                sphereCollider.isTrigger = true;
                sphereCollider.radius = adjustMaxDistance;
                sphereCollider.hideFlags = HideFlags.NotEditable;
            }

            rb = gameObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.hideFlags = HideFlags.NotEditable;
            }
        }

        void OnDestroy()
        {
            if (spotAmbienceInstance.isValid())
            {
                spotAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                spotAmbienceInstance.release();
            }
        }
    }
}