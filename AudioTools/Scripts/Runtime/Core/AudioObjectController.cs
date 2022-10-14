// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using FMODUnity;
using UnityEngine;
using System.Collections.Generic;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Core/Audio Object Controller")]
    public class AudioObjectController : MonoBehaviour
    {
        public enum ControlMethod
        {
            Event,
            Reference
        }

        public enum TriggerOn
        {
            None,
            Start,
            OnDisable,
            OnDestroy,
            OnTriggerEnter,
            OnTriggerExit
        }

        public TriggerOn triggerOn;
        public ControlMethod controlMethod;
        public ControlAction controlAction;
        public ParamRef[] parameters;

        [HideInInspector]
        public List<EventTag> eventTags = new List<EventTag>();

        [HideInInspector]
        public List<AudioObject> audioObjects = new List<AudioObject>();

        [HideInInspector]
        public AudioTriggerArea audioTriggerArea;

        void Awake()
        {
           if (audioTriggerArea != null)
           {
               audioTriggerArea.Triggered += TriggeredHandler;
           }  
        }

        void Start()
        {
            Trigger(TriggerOn.Start);
        }

        void OnDisable()
        {
            Trigger(TriggerOn.OnDisable);
        }

        void OnDestroy()
        {
            Trigger(TriggerOn.OnDestroy);

            if (audioTriggerArea != null)
            {
                audioTriggerArea.Triggered -= TriggeredHandler;
            }
        }

        private void TriggeredHandler(TriggerEventType triggerEventType)
        {
            if (triggerEventType == TriggerEventType.TriggerEnter)
            {
                Trigger(TriggerOn.OnTriggerEnter);
            }
            else if (triggerEventType == TriggerEventType.TriggerExit)
            {
                Trigger(TriggerOn.OnTriggerExit);
            }
        }

        private void Trigger(TriggerOn triggerOn) 
        {
            if (triggerOn != this.triggerOn)
                return;

            switch (controlMethod)
            {
                case ControlMethod.Event:
                    {
                        foreach (var eventTag in eventTags)
                        {
                            if (eventTag != null)
                            {
                                EventManager.PostEvent(new ControlActionEventArguments(eventTag, controlAction, parameters));
                            }
                        }
                    }
                    break;
                case ControlMethod.Reference:
                    {
                        foreach (var audioObject in audioObjects)
                        {
                            if (audioObject != null)
                            {
                                audioObject.Control(controlAction, parameters);
                            }
                        }
                    }
                    break;
            }
        }
    }
}