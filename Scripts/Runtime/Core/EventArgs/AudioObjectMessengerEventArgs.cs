// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using UnityEngine;

namespace AudioTools
{
    public class AudioObjectMessengerEventArgs : EventArgs
    {
        public AudioObjectTag audioObjectTag;
        public Transform transformToFollow;
    }
}