﻿// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Core/Audio Object")]
    public class AudioObject : MonoBehaviour
    {
        public Transform followTransform;
        public AudioObjectTag audioObjectTag;

        [Tooltip("If there is more than one event option, all options will be played before any of them is repeated " +
                 "and the same event will never play twice in a row.")]
        [FMODUnity.EventRef]
        public string[] eventOptions;
        private List<string> eventReferences = new List<string>();

        private List<FMOD.Studio.EventInstance> instantiations = new List<FMOD.Studio.EventInstance>();

        [Tooltip("All Audio Objects listen to the 'DestroyAllPersistentAudioObjects' -static event regardless of this setting.")]
        public bool listensToEvents = false;
        public bool singleton = false;
        public bool dontDestroyOnLoad = false;
        private bool isPersistent = false;
        public bool spatialAudioRoomAware = false;

        [HideInInspector]
        public SpatialAudioRoom initialRoom = null;

        private bool destructionInProgress = false;
        private bool initializationSuccesfull = false;

        // Event selection randomization.
        private List<int> availableIndexes = new List<int>();
        private int newRandomIndex = -2;
        private int lastUsedIndex = -1;

        // For attenuation visualization purposes in editor. 
        [HideInInspector]
        public int clickCounter = 0;
        [HideInInspector]
        public string currentAttenuationToDraw = null;

        void Awake()
        {
            for (int i = 0; i < eventOptions.Length; i++)
            {
                string eventOption = eventOptions[i];

                if (!string.IsNullOrEmpty(eventOption))
                {
                    eventReferences.Add(eventOption);
                }
            }

            if (eventReferences.Count < 1)
            {
                Debug.LogWarning("Audio Object '" + gameObject.name + "' does not contain any valid FMOD event references.");
                return;
            }

            if (dontDestroyOnLoad)
            {
                isPersistent = true;
                DontDestroyOnLoad(this.gameObject);
            }

            if (listensToEvents)
            {
                AudioObjectMessenger.OnStartAudioObjectEvent += OnStartSoundEvent;
                AudioObjectMessenger.OnStopAudioObjectEvent += OnStopSoundsEvent;

                if (isPersistent)
                {
                    AudioObjectMessenger.OnStopPersistentAudioObjectsEvent += OnStopPersistentSoundsEvent;
                    AudioObjectMessenger.OnStopAllPersistantAudioObjectEvents += OnStopAllPersistentSoundsEvent;
                }
            }
            else if (isPersistent)
            {
                AudioObjectMessenger.OnStopAllPersistantAudioObjectEvents += OnStopAllPersistentSoundsEvent;
            }

            CreateIndexList();

            initializationSuccesfull = true;
        }

        void Update()
        {
            if (!initializationSuccesfull) { return; }

            for (int i = instantiations.Count - 1; i > -1; i--)
            {
                FMOD.Studio.EventInstance eventInstance = instantiations[i];

                FMOD.Studio.PLAYBACK_STATE playbackState;
                eventInstance.getPlaybackState(out playbackState);

                if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    eventInstance.release(); 
                    instantiations.RemoveAt(i);
                }
            }

            if (destructionInProgress && instantiations.Count < 1)
            {
                Destroy(gameObject);
            }
        }

        void OnDisable()
        {
            StopSounds(true);
        }

        void OnDestroy()
        {
            StopSounds(true);
            EventSubscriptionCleanUp();
        }

        private void InstantiateAudioObject(Transform follow = null, List<AudioObjectParameter> parameters = null)
        {
            if (destructionInProgress || !initializationSuccesfull)
            {
                return;
            }

            if (singleton && instantiations.Count > 0)
            {
                return;
            }

            int index = 0;
            
            if (eventReferences.Count > 1)
            {
                index = SelectRandomIndex();
            }

            FMOD.Studio.EventInstance eventInstance;

            eventInstance = FMODUnity.RuntimeManager.CreateInstance(eventReferences[index]);

            if (!eventInstance.isValid())
            {
                Debug.LogError("Creating an event instance failed for Audio Object '" + gameObject.name + "'.");
                return;
            }

            if(parameters != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    AudioObjectParameter parameter = parameters[i];

                    FMOD.RESULT result = eventInstance.setParameterByName(parameter.name, parameter.value);

                    if (result != FMOD.RESULT.OK)
                    {
                        Debug.LogWarning("Setting a value for local parameter '" + parameter.name + 
                                         "' failed for '" + gameObject.name + "'. Fmod error: " + result);
                    }
                }
            }

            FMOD.Studio.EventDescription eventDescription = FMODUnity.RuntimeManager.GetEventDescription(eventReferences[index]);

            bool is3D;
            eventDescription.is3D(out is3D);

            Transform transformToFollow = null;

            if (is3D)
            {
                // Transform follow priority:
                // 1. Transform optionally provided with the event instantiation
                // 2. Transform optionally provided in the inspector
                // 3. The transform of this Audio Object.

                if (follow != null)
                {
                    transformToFollow = follow;
                }
                else if (followTransform)
                {
                    transformToFollow = followTransform;
                }
                else
                {
                    transformToFollow = gameObject.transform;
                }

                // If the followed game object has a rigibody, retrieve it and pass it to FMOD RuntimeManager for velocity updates. 
                Rigidbody rb = transformToFollow.gameObject.GetComponent<Rigidbody>();

                FMODUnity.RuntimeManager.AttachInstanceToGameObject(eventInstance, transformToFollow, rb);
            }

            if(spatialAudioRoomAware && SpatialAudioManager.instance != null)
            {
                if(is3D)
                {
                    float maxDistance;
                    eventDescription.getMaximumDistance(out maxDistance);

                    SpatialAudioManager.instance.RegisterRoomAwareInstance(eventInstance, transformToFollow, maxDistance, initialRoom);
                }
            }

            instantiations.Add(eventInstance);
            eventInstance.start();
        }

        private void StopSounds(bool release)
        {
            for (int i = 0; i < instantiations.Count; i++)
            {
                FMOD.Studio.EventInstance eventInstance = instantiations[i];

                if (eventInstance.isValid() == true)
                {
                    eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    
                    if(release == true)
                    {
                        eventInstance.release();
                    }
                }
            }
        }

        public void TriggerDirectly(TriggeringAction action, Transform followTransform = null, List<AudioObjectParameter> parameters = null)
        {
            switch (action)
            {
                case TriggeringAction.StartSound:
                    {
                        InstantiateAudioObject(followTransform, parameters);
                    }
                    break;
                case TriggeringAction.StopSound:
                    {
                        StopSounds(false);
                    }
                    break;
                case TriggeringAction.StopPersistentSound:
                    {
                        if(isPersistent)
                        {
                            destructionInProgress = true;
                            StopSounds(false);
                        }
                    }
                    break;
            }
        }

        private void OnStartSoundEvent(object sender, AudioObjectMessengerEventArgs eventArgs)
        {
            if (audioObjectTag != null && audioObjectTag == eventArgs.audioObjectTag) 
            {
                if (eventArgs.transformToFollow != null)
                {
                    InstantiateAudioObject(eventArgs.transformToFollow);
                }
                else
                {
                    InstantiateAudioObject();
                }
            }
        }

        private void OnStopSoundsEvent(object sender, AudioObjectMessengerEventArgs eventArgs) 
        {
            if (audioObjectTag != null && audioObjectTag == eventArgs.audioObjectTag)
            {
                StopSounds(false);
            }
        }

        private void OnStopPersistentSoundsEvent(object sender, AudioObjectMessengerEventArgs eventArgs)
        {
            if (isPersistent && audioObjectTag != null && audioObjectTag == eventArgs.audioObjectTag)
            {
                destructionInProgress = true;
                StopSounds(false);
            }
        }

        private void OnStopAllPersistentSoundsEvent(object sender, EventArgs e)
        {
            if (isPersistent)
            {
                destructionInProgress = true;
                StopSounds(false);
            }
        }

        private void EventSubscriptionCleanUp()
        {
            AudioObjectMessenger.OnStartAudioObjectEvent -= OnStartSoundEvent;
            AudioObjectMessenger.OnStopAudioObjectEvent -= OnStopSoundsEvent;
            AudioObjectMessenger.OnStopPersistentAudioObjectsEvent -= OnStopPersistentSoundsEvent;
            AudioObjectMessenger.OnStopAllPersistantAudioObjectEvents -= OnStopAllPersistentSoundsEvent;
        }

        public void SetLocalParameter(AudioObjectParameter parameter)
        {
            if (initializationSuccesfull == false) { return; }

            for (int i = 0; i < instantiations.Count; i++)
            {
                FMOD.Studio.EventInstance eventInstance = instantiations[i];

                FMOD.RESULT result = eventInstance.setParameterByName(parameter.name, parameter.value);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogWarning("Setting a value for local parameter '" + parameter.name + 
                                     "' failed for '" + gameObject.name + "'. Fmod error: " + result);
                }
            }
        }

        private void CreateIndexList()
        {
            if (eventReferences != null)
            {
                for (int i = 0; i < eventReferences.Count; i++)
                {
                    availableIndexes.Add(i);
                }
            }
        }

        private int SelectRandomIndex()
        {
            int min = 0;
            int max = availableIndexes.Count;
            int randomIndex = availableIndexes[UnityEngine.Random.Range(min, max)];
            newRandomIndex = randomIndex;

            // Enforce that an index is not repeated when the list is replenished.
            if (availableIndexes.Count == eventReferences.Count && eventReferences.Count > 1)
            {
                if (newRandomIndex == lastUsedIndex && newRandomIndex < eventReferences.Count - 1)
                {
                    newRandomIndex = newRandomIndex + 1;
                }
                else if (newRandomIndex == lastUsedIndex && newRandomIndex == eventReferences.Count - 1)
                {
                    newRandomIndex = newRandomIndex - 1;
                }
            }

            lastUsedIndex = newRandomIndex;


            // No indexes are repeated before the whole option list has been exhausted.
            availableIndexes.Remove(newRandomIndex);

            if (availableIndexes.Count == 0)
            {
                CreateIndexList();
            }

            return newRandomIndex;
        }

        void OnValidate()
        {
            if(eventOptions == null || eventOptions.Length < 1)
            {
                currentAttenuationToDraw = null;
                clickCounter = 0;
            }         
        }

#if UNITY_EDITOR
        public void IncrementClickCounter()
        {
            if(eventOptions != null && eventOptions.Length > 0)
            {
                float maxIndex = eventOptions.Length - 1;
                clickCounter++;

                if(clickCounter > maxIndex)
                {
                    clickCounter = 0;
                    currentAttenuationToDraw = eventOptions[clickCounter];
                }
                else
                {
                    currentAttenuationToDraw = eventOptions[clickCounter];
                }
            }
        }

        public void SetEventReferences(string[] eventReferences)
        {
            if(EditorApplication.isPlaying)
            {
                return;
            }

            eventOptions = eventReferences;
        }
    }
#endif
}