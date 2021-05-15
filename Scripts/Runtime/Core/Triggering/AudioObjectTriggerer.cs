// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Core/Audio Object Triggerer")]
    public class AudioObjectTriggerer : MonoBehaviour
    {
        public TriggerOn triggerOn = TriggerOn.None;
        public TriggeringType triggeringType = TriggeringType.Event;
        public TriggeringAction triggeringAction = TriggeringAction.StartSound;

        [HideInInspector]
        public AudioObjectTag audioObjectTag;

        [HideInInspector]
        public AudioObject audioObject;

        [HideInInspector]
        public AudioTriggerArea audioTriggerArea;

        [HideInInspector]
        public Transform followTransform;

        void Awake()
        {
           if ((triggerOn == TriggerOn.OnTriggerEnter || triggerOn == TriggerOn.OnTriggerExit) && audioTriggerArea != null) 
           {
                audioTriggerArea.OnTriggerAreaEvent += OnTriggerAreaEvents; 
           }     
        }

        void Start()
        {
            InitialTriggerBranch(TriggerOn.Start);
        }

        void OnDisable()
        {
            InitialTriggerBranch(TriggerOn.OnDisable);
        }

        void OnDestroy()
        {
            InitialTriggerBranch(TriggerOn.OnDestroy);
        }

        private void OnTriggerAreaEvents(object sender, AudioTriggerAreaEventArgs eventArgs)
        {
            if (eventArgs.triggerEventType == AudioTriggerAreaEventArgs.TriggerEventType.TriggerEnter)
            {
                InitialTriggerBranch(TriggerOn.OnTriggerEnter);
            }
            else
            {
                InitialTriggerBranch(TriggerOn.OnTriggerExit);
            }
        }

        private void InitialTriggerBranch(TriggerOn argTriggerOn) 
        {
            if (argTriggerOn != triggerOn) { return; }

            switch (triggeringType)
            {
                case TriggeringType.Event:
                    {
                        if (audioObjectTag == null && triggeringAction != TriggeringAction.StopAllPersistentSounds)
                        {
                            return;
                        }
                        else
                        {
                            TriggerWithEvent();
                        }
                    }
                    break;
                case TriggeringType.DirectReference:
                    {
                        if (audioObject == null && triggeringAction != TriggeringAction.StopAllPersistentSounds)
                        {
                            return;
                        }
                        else
                        {
                            TriggerWithReference();
                        }
                    }
                    break;
            }
        }

        private void TriggerWithEvent()
        {
            switch (triggeringAction)
            {
                case TriggeringAction.StartSound:
                    {
                        if (followTransform != null)
                        {
                            AudioObjectMessenger.StartAudioObjects(audioObjectTag, followTransform);
                        }
                        else
                        {
                            AudioObjectMessenger.StartAudioObjects(audioObjectTag);
                        }
                    }
                    break;
                case TriggeringAction.StopSound:
                    AudioObjectMessenger.StopAudioObjects(audioObjectTag);
                    break;
                case TriggeringAction.StopPersistentSound:
                    AudioObjectMessenger.StopPersistentAudioObjects(audioObjectTag);
                    break;
                case TriggeringAction.StopAllPersistentSounds:
                    AudioObjectMessenger.StopAllPersistentAudioObjects();
                    break;
            }
        }
        private void TriggerWithReference()
        {
            switch (triggeringAction)
            {
                case TriggeringAction.StartSound:
                    {
                        if (followTransform != null)
                        {
                            audioObject.TriggerDirectly(TriggeringAction.StartSound, followTransform);
                        }
                        else
                        {
                            audioObject.TriggerDirectly(TriggeringAction.StartSound);
                        }
                    }
                    break;
                case TriggeringAction.StopSound:
                    audioObject.TriggerDirectly(TriggeringAction.StopSound);
                    break;
                case TriggeringAction.StopPersistentSound:
                    audioObject.TriggerDirectly(TriggeringAction.StopPersistentSound);
                    break;
                case TriggeringAction.StopAllPersistentSounds:
                    AudioObjectMessenger.StopAllPersistentAudioObjects();
                    break;
            }
        }
    }
}