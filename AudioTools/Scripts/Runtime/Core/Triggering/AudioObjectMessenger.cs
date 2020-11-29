// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEngine;
using System;

namespace AudioTools
{
    public static class AudioObjectMessenger
    {
        public static event EventHandler<AudioObjectMessengerEventArgs> OnStartAudioObjectEvent;
        public static event EventHandler<AudioObjectMessengerEventArgs> OnStopAudioObjectEvent;
        public static event EventHandler<AudioObjectMessengerEventArgs> OnStopPersistentAudioObjectsEvent;
        public static event EventHandler OnStopAllPersistantAudioObjectEvents;

        public static void StartAudioObjects(AudioObjectTag audioObjectTag, Transform followTransform = null) 
        {
            var eventArguments = new AudioObjectMessengerEventArgs();
            eventArguments.audioObjectTag = audioObjectTag;
            eventArguments.transformToFollow = followTransform;
            OnStartAudioObjectEvent?.Invoke(null, eventArguments);
        }

        public static void StopAudioObjects(AudioObjectTag audioObjectTag)
        {
            var eventArguments = new AudioObjectMessengerEventArgs();
            eventArguments.audioObjectTag = audioObjectTag;
            OnStopAudioObjectEvent?.Invoke(null, eventArguments);
        }

        public static void StopPersistentAudioObjects(AudioObjectTag audioObjectTag)
        {
            var eventArguments = new AudioObjectMessengerEventArgs();
            eventArguments.audioObjectTag = audioObjectTag;

            OnStopPersistentAudioObjectsEvent?.Invoke(null, eventArguments);
        }

        public static void StopAllPersistentAudioObjects()
        {
            OnStopAllPersistantAudioObjectEvents?.Invoke(null, EventArgs.Empty);
        }
    }
}