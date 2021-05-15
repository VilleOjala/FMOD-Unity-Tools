// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(OpeningSpotAmbience))]
    public class OpeningSpotAmbienceEditor : Editor
    {
        SerializedProperty initialRoom;

        void OnEnable()
        {
            initialRoom = serializedObject.FindProperty("initialRoom");   
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            serializedObject.Update();

            var targetScript = target as OpeningSpotAmbience;

            if (targetScript.spatialAudioRoomAware)
            {
                EditorGUILayout.PropertyField(initialRoom);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}