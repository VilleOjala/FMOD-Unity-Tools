// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioObjectTriggerer))]
    public class AudioObjectTriggererEditor : Editor
    {
        SerializedProperty triggeringType;
        SerializedProperty triggerOn;
        SerializedProperty triggeringAction;
        SerializedProperty audioObjectTag;
        SerializedProperty audioObject;
        SerializedProperty audioTriggerArea;
        SerializedProperty followTransform;

        void OnEnable()
        {
            triggeringType = serializedObject.FindProperty("triggeringType");
            triggerOn = serializedObject.FindProperty("triggerOn");
            triggeringAction = serializedObject.FindProperty("triggeringAction");
            audioObjectTag = serializedObject.FindProperty("audioObjectTag");
            audioObject = serializedObject.FindProperty("audioObject");
            audioTriggerArea = serializedObject.FindProperty("audioTriggerArea");
            followTransform = serializedObject.FindProperty("followTransform");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            serializedObject.Update();

            if (triggeringType.enumValueIndex == 0)
            {
                if (triggeringAction.enumValueIndex != 3)
                {
                    EditorGUILayout.PropertyField(audioObjectTag, new GUIContent("Audio Object Tag"));
                }

                if (triggerOn.enumValueIndex == 4 || triggerOn.enumValueIndex == 5)
                {
                    EditorGUILayout.PropertyField(audioTriggerArea, new GUIContent("Audio Trigger Area"));
                }

                if (triggeringAction.enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(followTransform, new GUIContent("Follow Transform (optional)"));
                }
            }
            else
            {
                if (triggeringAction.enumValueIndex != 3)
                {
                    EditorGUILayout.PropertyField(audioObject, new GUIContent("Audio Object"));
                }

                if (triggerOn.enumValueIndex == 4 || triggerOn.enumValueIndex == 5)
                {
                    EditorGUILayout.PropertyField(audioTriggerArea, new GUIContent("Audio Trigger Area"));
                }

                if (triggeringAction.enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(followTransform, new GUIContent("Follow Transform (optional)"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}