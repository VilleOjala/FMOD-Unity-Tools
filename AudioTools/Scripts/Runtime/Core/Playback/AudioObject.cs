// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Runtime.InteropServices;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Core/Audio Object")]
    public class AudioObject : MonoBehaviour
    {
        public Transform followTransform;
        public AudioObjectTag audioObjectTag;

        [Tooltip("If there is more than one event option, all options will be played before any of them is repeated " +
                 "and the same event will never play twice in a row.")]
        [EventRef]
        public string[] eventOptions;
        private List<string> eventReferences = new List<string>();

        private List<EventInstance> instantiations = new List<EventInstance>();

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

        EVENT_CALLBACK instanceCreatedCallback;

        private List<DelayedSpatialAudioRegistree> delayedSpatialAudioRegistrees = new List<DelayedSpatialAudioRegistree>();

        private class DelayedSpatialAudioRegistree
        {
            public EventInstance eventInstance;
            public Transform transformToFollow;
            public bool isInvalid = false;
            public bool instanceCreationComplete = false;
            public float maxDistance;
        }
    

        public bool usesResonanceAudioSource = false;
        private bool _usesResonanceAudioSource = false;

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
                AudioObjectMessenger.StartAudioObjectEvent += OnStartSoundEvent;
                AudioObjectMessenger.StopAudioObjectEvent += OnStopSoundsEvent;

                if (isPersistent)
                {
                    AudioObjectMessenger.StopPersistentAudioObjectsEvent += OnStopPersistentSoundsEvent;
                    AudioObjectMessenger.StopAllPersistantAudioObjectsEvent += OnStopAllPersistentSoundsEvent;
                }
            }
            else if (isPersistent)
            {
                AudioObjectMessenger.StopAllPersistantAudioObjectsEvent += OnStopAllPersistentSoundsEvent;
            }

            CreateIndexList();

            _usesResonanceAudioSource = usesResonanceAudioSource;

            if (_usesResonanceAudioSource && spatialAudioRoomAware)
            {
                instanceCreatedCallback = new EVENT_CALLBACK(EventInstanceCreatedCallback);
            }

            initializationSuccesfull = true;
        }

        void Update()
        {
            if (!initializationSuccesfull) { return; }

            for (int i = instantiations.Count - 1; i > -1; i--)
            {
                EventInstance eventInstance = instantiations[i];

                PLAYBACK_STATE playbackState;
                eventInstance.getPlaybackState(out playbackState);

                if (playbackState == PLAYBACK_STATE.STOPPED)
                {
                    eventInstance.release(); 
                    instantiations.RemoveAt(i);
                }
            }

            
            for (int i = delayedSpatialAudioRegistrees.Count - 1; i > -1; i--)
            {
                var delayedRegistree = delayedSpatialAudioRegistrees[i];

                if (delayedRegistree.isInvalid)
                {
                    delayedSpatialAudioRegistrees.RemoveAt(i);
                }
                else if (delayedRegistree.instanceCreationComplete)
                {
                    if (SpatialAudioManager.Instance != null)
                    {
                        SpatialAudioManager.Instance.RegisterRoomAwareInstance(delayedRegistree.eventInstance, delayedRegistree.transformToFollow,
                                                       delayedRegistree.maxDistance, initialRoom, true);
                    }

                    instantiations.Add(delayedRegistree.eventInstance);
                    delayedRegistree.eventInstance.start();
                    delayedSpatialAudioRegistrees.RemoveAt(i);
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

            EventInstance eventInstance;

            eventInstance = RuntimeManager.CreateInstance(eventReferences[index]);

            if (!eventInstance.isValid())
            {
                Debug.LogError("Creating an event instance failed for Audio Object '" + gameObject.name + "'.");
                return;
            }

            if (parameters != null)
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

            EventDescription eventDescription = RuntimeManager.GetEventDescription(eventReferences[index]);

            bool is3D;
            eventDescription.is3D(out is3D);

            Transform transformToFollow = null;

            if (is3D)
            {
                // Transform follow priority:
                // 1. Transform optionally provided with the event instantiation
                // 2. Transform optionally provided in the inspector
                // 3. The Transform of this Audio Object.

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

                RuntimeManager.AttachInstanceToGameObject(eventInstance, transformToFollow, rb);
            }

            if (spatialAudioRoomAware && SpatialAudioManager.Instance != null)
            {
                if (is3D)
                {
                    if (_usesResonanceAudioSource)
                    {
                        var delayedRegistree = new DelayedSpatialAudioRegistree();
                        delayedRegistree.transformToFollow = transformToFollow;
                        delayedRegistree.eventInstance = eventInstance;
                        delayedSpatialAudioRegistrees.Add(delayedRegistree);

                        GCHandle registreeHandle = GCHandle.Alloc(delayedRegistree, GCHandleType.Pinned);
                        eventInstance.setUserData(GCHandle.ToIntPtr(registreeHandle));
                        eventInstance.setCallback(instanceCreatedCallback, EVENT_CALLBACK_TYPE.CREATED | EVENT_CALLBACK_TYPE.DESTROYED);
                        return;
                    }
                    else
                    {
                        float maxDistance;
                        eventDescription.getMaximumDistance(out maxDistance);
                        SpatialAudioManager.Instance.RegisterRoomAwareInstance(eventInstance, transformToFollow, maxDistance, initialRoom, false);
                    }
                }
            }

            instantiations.Add(eventInstance);
            eventInstance.start();
        }

        private void StopSounds(bool release)
        {
            for (int i = 0; i < instantiations.Count; i++)
            {
                EventInstance eventInstance = instantiations[i];

                if (eventInstance.isValid())
                {
                    eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    
                    if (release)
                    {
                        eventInstance.release();
                    }
                }
            }

            for (int i = 0; i < delayedSpatialAudioRegistrees.Count; i++)
            {
                var delayedRegistree = delayedSpatialAudioRegistrees[i];
                delayedRegistree.eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                delayedRegistree.eventInstance.release();
            }

            delayedSpatialAudioRegistrees.Clear();
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
                        if (isPersistent)
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
            AudioObjectMessenger.StartAudioObjectEvent -= OnStartSoundEvent;
            AudioObjectMessenger.StopAudioObjectEvent -= OnStopSoundsEvent;
            AudioObjectMessenger.StopPersistentAudioObjectsEvent -= OnStopPersistentSoundsEvent;
            AudioObjectMessenger.StopAllPersistantAudioObjectsEvent -= OnStopAllPersistentSoundsEvent;
        }

        public void SetLocalParameter(AudioObjectParameter parameter)
        {
            if (initializationSuccesfull == false) { return; }

            for (int i = 0; i < instantiations.Count; i++)
            {
                EventInstance eventInstance = instantiations[i];

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
                    newRandomIndex += 1;
                }
                else if (newRandomIndex == lastUsedIndex && newRandomIndex == eventReferences.Count - 1)
                {
                    newRandomIndex -= 1;
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
            if (eventOptions == null || eventOptions.Length < 1)
            {
                currentAttenuationToDraw = null;
                clickCounter = 0;
            }         
        }

#if UNITY_EDITOR

        public void IncrementClickCounter()
        {
            if (eventOptions != null && eventOptions.Length > 0)
            {
                float maxIndex = eventOptions.Length - 1;
                clickCounter++;

                if (clickCounter > maxIndex)
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
            if (EditorApplication.isPlaying)
            {
                return;
            }

            eventOptions = eventReferences;
        }
#endif

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        FMOD.RESULT EventInstanceCreatedCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            EventInstance instance = new EventInstance(instancePtr);

            IntPtr registreePointer;
            instance.getUserData(out registreePointer);
            GCHandle registreeHandle = GCHandle.FromIntPtr(registreePointer);
            DelayedSpatialAudioRegistree delayedRegistree = registreeHandle.Target as DelayedSpatialAudioRegistree;

            switch (type)
            {
                case EVENT_CALLBACK_TYPE.CREATED:
                    {
                        float maxDistance = ResonanceAudioSourceUtility.GetResonanceAudioSourceMaxDistance(instance);

                        if (maxDistance == -1)
                        {
                            if (delayedRegistree != null)
                                delayedRegistree.isInvalid = true;
                        }
                        else 
                        {
                            if (instance.isValid() && delayedRegistree != null)
                            {
                                delayedRegistree.maxDistance = maxDistance;
                                delayedRegistree.instanceCreationComplete = true;
                            }
                            else if (delayedRegistree != null)
                            {
                                delayedRegistree.isInvalid = true;
                            }
                        }
                    }
                    break;
                case EVENT_CALLBACK_TYPE.DESTROYED:
                    {
                        registreeHandle.Free();
                    }
                    break;
                default:
                    break;
            }

            return FMOD.RESULT.OK;
        }     
    }
}