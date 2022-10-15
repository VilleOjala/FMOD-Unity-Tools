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
        SerializedProperty wallOcclusion;
        SerializedProperty initialState;
        SerializedProperty traversalMaxCost;

        void OnEnable()
        {
            openEnvelope = serializedObject.FindProperty("openEnvelope");
            closeEnvelope = serializedObject.FindProperty("closeEnvelope");
            openFadeTime = serializedObject.FindProperty("openFadeTime");
            closeFadeTime = serializedObject.FindProperty("closeFadeTime");
            wallOcclusion = serializedObject.FindProperty("wallOcclusion");
            initialState = serializedObject.FindProperty("initialState");
            traversalMaxCost = serializedObject.FindProperty("traversalMaxCost");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as SpatialAudioPortal;
            serializedObject.Update();

            if (targetScript.portalType == SpatialAudioPortal.PortalType.Opening)
            {
                EditorGUILayout.PropertyField(initialState);
                EditorGUILayout.PropertyField(openEnvelope);
                EditorGUILayout.PropertyField(closeEnvelope);
                EditorGUILayout.PropertyField(openFadeTime);
                EditorGUILayout.PropertyField(closeFadeTime);
                EditorGUILayout.PropertyField(traversalMaxCost);

                string debug = string.Copy(targetScript.debugPortalStatus);
                EditorGUILayout.LabelField(debug);
            }
            else
            {
                EditorGUILayout.PropertyField(wallOcclusion);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}