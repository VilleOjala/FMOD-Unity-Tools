// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FMODUnityTools
{
    [CreateAssetMenu(fileName = "KeyOffsetData", menuName = "FMOD Unity Tools/Key Offset Data")]
    public class KeyOffsetData : ScriptableObject
    {
        public List<KeyOffset> keyOffsets = new List<KeyOffset>();

        // Provide a text file with overriden line offsets for specifics keys. The format of the file should be:
        // Hello,0.5
        // Sailor,-2
        // Foo,3
        // Bar,-2.3
        // etc...

        [Space(12)]
        public TextAsset textFile;

        [HideInInspector]
        public string keyToRemove = "";

        [Serializable]
        public class KeyOffset
        {
            public string key;
            public float offset;         
        }
    }
}