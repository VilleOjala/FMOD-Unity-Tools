﻿// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace FMODUnityTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpatialAudioPortal))]
    public class SpatialAudioPortalEditor : Editor
    {
        SerializedProperty openEnvelope;
        SerializedProperty closeEnvelope;
        SerializedProperty openFadeTime;
        SerializedProperty closeFadeTime;
        SerializedProperty initialState;
        SerializedProperty maxClosednessCost;

        void OnEnable()
        {
            openEnvelope = serializedObject.FindProperty("openEnvelope");
            closeEnvelope = serializedObject.FindProperty("closeEnvelope");
            openFadeTime = serializedObject.FindProperty("openFadeTime");
            closeFadeTime = serializedObject.FindProperty("closeFadeTime");
            initialState = serializedObject.FindProperty("initialState");
            maxClosednessCost = serializedObject.FindProperty("maxClosednessCost");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as SpatialAudioPortal;
            serializedObject.Update();
            EditorGUILayout.PropertyField(initialState);
            EditorGUILayout.PropertyField(openEnvelope);
            EditorGUILayout.PropertyField(closeEnvelope);
            EditorGUILayout.PropertyField(openFadeTime);
            EditorGUILayout.PropertyField(closeFadeTime);
            EditorGUILayout.PropertyField(maxClosednessCost);
            string debug = string.Copy(targetScript.debugPortalStatus);
            EditorGUILayout.LabelField(debug);
            serializedObject.ApplyModifiedProperties();
        }
    }
}