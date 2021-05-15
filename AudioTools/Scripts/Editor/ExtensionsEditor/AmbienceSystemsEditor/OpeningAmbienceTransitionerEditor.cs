// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(OpeningAmbienceTransitioner))]
    public class OpeningAmbienceTransitionerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetScript = target as OpeningAmbienceTransitioner;

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Opening Spot Ambience"))
            {
                var spotGameObj = new GameObject();
                spotGameObj.name = "OpeningSpotAmbience";
                spotGameObj.transform.parent = targetScript.transform;

                var spotAmbienceComponent = spotGameObj.AddComponent<OpeningSpotAmbience>();
                targetScript.openingSpotAmbiences.Add(spotAmbienceComponent);
            }
        }
    }
}