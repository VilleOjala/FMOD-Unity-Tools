// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System;

namespace AudioTools
{
    public class AudioTriggerAreaEventArgs : EventArgs
    {
        public TriggerEventType triggerEventType;
        public enum TriggerEventType
        {
            TriggerEnter = 0,
            TriggerExit = 1
        }
    }
}