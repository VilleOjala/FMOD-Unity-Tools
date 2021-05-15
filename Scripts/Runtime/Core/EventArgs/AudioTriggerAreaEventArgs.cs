// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;

namespace AudioTools
{
    public class AudioTriggerAreaEventArgs : EventArgs
    {
        public TriggerEventType triggerEventType;

        public enum TriggerEventType
        {
            TriggerEnter,
            TriggerExit
        }
    }
}