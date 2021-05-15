// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [CreateAssetMenu(fileName = "NewVoiceOverDurationSet", menuName = "Audio Tools/Voiceover Duration Set", order = 4)]
    public class VoiceoverDurationSet : ScriptableObject
    {
        public List<KeyDuration> keyDurations = new List<KeyDuration>();

        [Space(12)]

        // Provide a text file with overriden line durations for specifics keys. The format of the file should be:

        // Hello, 4.3
        // Sailor, 0.7
        // Foo, 5
        // Bar, 14
        //
        // etc...
        public TextAsset textFile;

        [HideInInspector]
        public string keyToRemove = "";

        [System.Serializable]
        public class KeyDuration
        {
            public string key;
            public float duration;         
        }
    }
}