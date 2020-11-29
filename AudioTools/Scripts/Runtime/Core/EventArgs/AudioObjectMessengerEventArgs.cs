// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System;
using UnityEngine;

namespace AudioTools
{
    public class AudioObjectMessengerEventArgs : EventArgs
    {
        public AudioObjectTag audioObjectTag = null;
        public Transform transformToFollow = null;
    }
}