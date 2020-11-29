// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(SpotAmbience))]
    public class SpotAmbienceEditor : Editor
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

            var targetScript = target as SpotAmbience;

            if (targetScript.spatialAudioRoomAware)
            {
                EditorGUILayout.PropertyField(initialRoom);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}