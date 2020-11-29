// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(BaseAmbienceArea))]
    public class BaseAmbienceAreaEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetScript = target as BaseAmbienceArea;

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Spot Ambience"))
            {
                var spotGameObj = new GameObject();
                spotGameObj.name = "SpotAmbience";
                spotGameObj.transform.parent = targetScript.transform;

                var spotAmbienceComponent = spotGameObj.AddComponent<SpotAmbience>();
                targetScript.spotAmbiences.Add(spotAmbienceComponent);
            }
        }
    }
}