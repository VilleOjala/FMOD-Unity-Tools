// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

namespace FMODUnityTools
{
    public enum ControlAction
    {
        Start,
        Stop,
        UpdateParameters
    }

    [AddComponentMenu("FMOD Unity Tools/Core/Audio Object")]
    public class AudioObject : MonoBehaviour, IEventListener
    {
        public EventTag eventTag;

        [SerializeField, Tooltip("For 3D sounds. If left empty, sounds follow the position of this Game Object.")]
        private Transform followTarget;

        [SerializeField, Tooltip("For 3D sounds requiring velocity updates")]
        private Rigidbody rb;

        /// <summary>
        /// Changing the followed target does not affect already playing instances.
        /// </summary>
        public Transform FollowTarget
        {
            get
            {
                if (followTarget == null)
                {
                    return transform;
                } 

                return followTarget;
            }
            set
            {
                if (value == followTarget)
                    return;

                followTarget = value;           
            }
        }

        /// <summary>
        /// Changing the target rigidbody does not affect already playing instances.
        /// </summary>
        public Rigidbody TargetRigidbody
        {
            get
            {
                return rb;
            }
            set 
            { 
                rb = value;              
            }
        }
             
        public List<EventReference> eventReferences = new List<EventReference>();
        private List<EventDescription> eventDescriptions = new List<EventDescription>();
        private List<EventInstance> eventInstances = new List<EventInstance>();

        [Min(0)]
        public int excludePrevious;
        private int _excludePrevious;
        private List<int> excludedIndexes = new List<int>();

        public bool singleton = false;
        public bool spatialAudioRoomAware = false;

        [HideInInspector]
        public SpatialAudioRoom fixedRoom = null;
        private List<int> availableIndexes = new List<int>();

        // For visualization purposes in the editor. 
        [HideInInspector]
        public int clickCounter = 0;
        [HideInInspector]
        public EventReference currentAttenuationToDraw = default;
        //
    
        void Awake()
        {
            foreach (var eventReference in eventReferences)
            {
                if (eventReference.IsNull)
                    continue;

                var eventDescription = RuntimeManager.GetEventDescription(eventReference);

                if (eventDescription.isValid())
                {
                    eventDescriptions.Add(eventDescription);
                }
            }

            ExcludePreviousSanityCheck();
            _excludePrevious = excludePrevious;
            availableIndexes = new List<int>(eventDescriptions.Count);
        }

        void Update()
        {
            for (int i = eventInstances.Count - 1; i > -1; i--)
            {
                EventInstance eventInstance = eventInstances[i];
                eventInstance.getPlaybackState(out PLAYBACK_STATE playbackState);

                if (playbackState == PLAYBACK_STATE.STOPPED)
                {
                    eventInstance.release(); 
                    eventInstances.RemoveAt(i);
                }
            }
        }

        private void OnEnable()
        {
            EventManager.RegisterListener(this);
        }

        private void OnDisable()
        {
            EventManager.UnregisterListener(this);
            StopAllInstances();
        }

        private void OnDestroy()
        {
            StopAllInstances();
        }

        private void InstantiateAudioObject(params ParamRef[] parameters)
        {
            if ((singleton && eventInstances.Count > 0) || eventDescriptions.Count == 0)
                return;

            var eventDescription = GetRandomEventDescription();

            if (!eventDescription.isValid())
            {
                Debug.LogError("Creating an event instance failed for Audio Object '" + gameObject.name + "'.");
                return;
            }

            eventDescription.createInstance(out EventInstance eventInstance);
            SetLocalParameters(eventInstance, parameters);
            eventDescription.is3D(out bool is3D);

            if (is3D)
            {
                RuntimeManager.AttachInstanceToGameObject(eventInstance, FollowTarget, rb);

                if (spatialAudioRoomAware)
                {
                    if (SpatialAudioManager.Instance != null)
                    {
                        SpatialAudioManager.Instance.RegisterRoomAwareInstance(eventInstance, fixedRoom);
                    }
                }
            }

            eventInstance.start();
            eventDescription.isOneshot(out bool isOneshot);
            
            if (isOneshot)
            {
                eventInstance.release();
            }

            eventInstances.Add(eventInstance);
        }

        private void StopAllInstances()
        {
            foreach (var eventInstance in eventInstances)
            {
                if (eventInstance.isValid())
                {
                    eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    eventInstance.release();
                }
            }
        }

        public void Control(ControlAction action, params ParamRef[] parameters)
        {
            switch (action)
            {
                case ControlAction.Start:
                        InstantiateAudioObject(parameters);
                    break;
                case ControlAction.Stop:
                        SetLocalParametersForAll(parameters);
                        StopAllInstances();
                    break;
                case ControlAction.UpdateParameters:
                        SetLocalParametersForAll(parameters);
                    break;
            }
        }

        public void EventReceived(EventArguments eventArgs)
        {
            if (eventArgs == null || eventArgs.eventTag == null || eventArgs.eventTag != this.eventTag)
                return;

            if (eventArgs is ControlActionEventArguments)
            {
                var args = (ControlActionEventArguments)eventArgs;
                Control(args.controlAction, args.parameters);
            }
            else if (eventArgs is AnimatorStateEventArguments)
            {
                var args = (AnimatorStateEventArguments)eventArgs;
                Control(args.controlAction, args.parameters);
            }
        }

        public void SetLocalParametersForAll(params ParamRef[] parameters)
        {
            foreach (var eventInstance in eventInstances)
            {
                SetLocalParameters(eventInstance, parameters);
            }
        }

        private void SetLocalParameters(EventInstance eventInstance, params ParamRef[] parameters)
        {
            if (parameters == null || !eventInstance.isValid())
                return;

            foreach (var parameter in parameters)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.Name))
                    continue;

                var result = eventInstance.setParameterByName(parameter.Name, parameter.Value);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogWarning("Setting a value for local parameter '" + parameter.Name +
                                     "' failed for '" + gameObject.name + "'. Fmod error: " + result);
                }
            }
        }

        private EventDescription GetRandomEventDescription()
        {
            if (eventDescriptions.Count == 0)
            {
                return default;
            }
            else if (eventDescriptions.Count == 1)
            {
                return eventDescriptions[0];
            }

            if (_excludePrevious == 0)
            {
                int randomIndex = Random.Range(0, eventDescriptions.Count);
                return eventDescriptions[randomIndex];
            }
            else
            {
                availableIndexes.Clear();

                for (int i = 0; i < eventDescriptions.Count; i++)
                {
                    availableIndexes.Add(i);
                }

                for (int i = availableIndexes.Count - 1; i >= 0; i--)
                {
                    if (excludedIndexes.Contains(i))
                    {
                        availableIndexes.RemoveAt(i);
                    }
                }

                int randomIndex = Random.Range(0, availableIndexes.Count);
                int randomIndexValue = availableIndexes[randomIndex];
                var eventDescription = eventDescriptions[randomIndexValue];

                if (excludedIndexes.Count == _excludePrevious)
                {
                    excludedIndexes.RemoveAt(excludedIndexes.Count - 1);
                    excludedIndexes.Insert(0, randomIndexValue);
                }
                else
                {
                    excludedIndexes.Insert(0, randomIndexValue);
                }

                return eventDescription;
            }
        }

        private void ExcludePreviousSanityCheck()
        {
            if (eventReferences.Count <= 1)
            {
                excludePrevious = 0;
            }
            else if (excludePrevious >= eventReferences.Count)
            {
                excludePrevious = eventReferences.Count - 1;
            }
        }

        void OnValidate()
        {
            ExcludePreviousSanityCheck();

            if (eventReferences == null || eventReferences.Count < 1)
            {
                currentAttenuationToDraw = default;
                clickCounter = 0;
            }
            else
            {
                if (clickCounter <= eventReferences.Count - 1)
                {
                    currentAttenuationToDraw = eventReferences[clickCounter];
                }
                else
                {
                    clickCounter = 0;
                    currentAttenuationToDraw = eventReferences[clickCounter];
                }
            }
        }

#if UNITY_EDITOR

        public void IncrementClickCounter()
        {
            if (eventReferences != null && eventReferences.Count > 0)
            {
                float maxIndex = eventReferences.Count - 1;
                clickCounter++;

                if (clickCounter > maxIndex)
                {
                    clickCounter = 0;
                    currentAttenuationToDraw = eventReferences[clickCounter];
                }
                else
                {
                    currentAttenuationToDraw = eventReferences[clickCounter];
                }
            }
        }
#endif
    }
}