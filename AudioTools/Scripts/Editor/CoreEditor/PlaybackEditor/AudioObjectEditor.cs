// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEngine;
using UnityEditor;
using FMODUnity;

namespace AudioTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioObject))]
    public class AudioObjectEditor : Editor
    {
        SerializedProperty initialRoom;

        public void OnEnable()
        {
            initialRoom = serializedObject.FindProperty("initialRoom");
        }

        public override void OnInspectorGUI()
        {
            var audioObject = target as AudioObject;

            DrawDefaultInspector();

            serializedObject.Update();

            if (audioObject.spatialAudioRoomAware)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(initialRoom, new GUIContent("Initial Room (optional)"));
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(audioObject.currentAttenuationToDraw, GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button("Update/change the event for attenuation gizmo", GUILayout.MaxHeight(22), GUILayout.MaxWidth(600)))
            {
                audioObject.IncrementClickCounter();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
        
        public void OnSceneGUI()
        {
            var audioObject = target as AudioObject;

            string path = audioObject.currentAttenuationToDraw;
            if (string.IsNullOrEmpty(path)) { return; }
            EditorEventRef editorEvent = EventManager.EventFromPath(path);
            if (editorEvent != null && editorEvent.Is3D)
            {
                float minDistance = editorEvent.MinDistance;
                float maxDistance = editorEvent.MaxDistance;
                Handles.RadiusHandle(Quaternion.identity, audioObject.transform.position, minDistance); 
                Handles.RadiusHandle(Quaternion.identity, audioObject.transform.position, maxDistance); 
            }
        }
    }
}