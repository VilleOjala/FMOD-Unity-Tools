// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace FMODUnityTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioObjectController))]
    public class AudioObjectControllerEditor : Editor
    {
        SerializedProperty controlMethod;
        SerializedProperty triggerOn;
        SerializedProperty eventTags;
        SerializedProperty audioObjects;
        SerializedProperty audioTriggerArea;
        
        void OnEnable()
        {
            controlMethod = serializedObject.FindProperty("controlMethod");
            triggerOn = serializedObject.FindProperty("triggerOn");
            eventTags = serializedObject.FindProperty("eventTags");
            audioObjects = serializedObject.FindProperty("audioObjects");
            audioTriggerArea = serializedObject.FindProperty("audioTriggerArea");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();

            if (triggerOn.enumValueIndex == (int)AudioObjectController.TriggerOn.OnTriggerEnter || 
                triggerOn.enumValueIndex == (int)AudioObjectController.TriggerOn.OnTriggerExit)
            {
                EditorGUILayout.PropertyField(audioTriggerArea, new GUIContent("Audio Trigger Area"));
            }

            if (controlMethod.enumValueIndex == (int)AudioObjectController.ControlMethod.Event)
            {
                EditorGUILayout.PropertyField(eventTags, new GUIContent("Event Tags"));
            }
            else if (controlMethod.enumValueIndex == (int)AudioObjectController.ControlMethod.Reference)
            {
                EditorGUILayout.PropertyField(audioObjects, new GUIContent("Audio Objects"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}