// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(VoiceoverPlaybackHandler))]
    public class VoiceoverPlaybackHandlerEditor : Editor
    {
        SerializedProperty initialRoom;

        void OnEnable()
        {
            initialRoom = serializedObject.FindProperty("initialRoom");  
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as VoiceoverPlaybackHandler;

            DrawDefaultInspector();

            serializedObject.Update();

            if (targetScript.spatialAudioRoomAware)
            {
                EditorGUILayout.PropertyField(initialRoom);
            }

            serializedObject.ApplyModifiedProperties();            
        }
    }
}