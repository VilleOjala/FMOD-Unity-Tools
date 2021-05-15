// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using System;

namespace AudioTools
{
    public static class AudioObjectMessenger
    {
        public static event EventHandler<AudioObjectMessengerEventArgs> StartAudioObjectEvent;
        public static event EventHandler<AudioObjectMessengerEventArgs> StopAudioObjectEvent;
        public static event EventHandler<AudioObjectMessengerEventArgs> StopPersistentAudioObjectsEvent;
        public static event EventHandler StopAllPersistantAudioObjectsEvent;

        public static void StartAudioObjects(AudioObjectTag audioObjectTag, Transform followTransform = null) 
        {
            var eventArguments = new AudioObjectMessengerEventArgs();
            eventArguments.audioObjectTag = audioObjectTag;
            eventArguments.transformToFollow = followTransform;
            StartAudioObjectEvent?.Invoke(null, eventArguments);
        }

        public static void StopAudioObjects(AudioObjectTag audioObjectTag)
        {
            var eventArguments = new AudioObjectMessengerEventArgs();
            eventArguments.audioObjectTag = audioObjectTag;
            StopAudioObjectEvent?.Invoke(null, eventArguments);
        }

        public static void StopPersistentAudioObjects(AudioObjectTag audioObjectTag)
        {
            var eventArguments = new AudioObjectMessengerEventArgs();
            eventArguments.audioObjectTag = audioObjectTag;

            StopPersistentAudioObjectsEvent?.Invoke(null, eventArguments);
        }

        public static void StopAllPersistentAudioObjects()
        {
            StopAllPersistantAudioObjectsEvent?.Invoke(null, EventArgs.Empty);
        }
    }
}