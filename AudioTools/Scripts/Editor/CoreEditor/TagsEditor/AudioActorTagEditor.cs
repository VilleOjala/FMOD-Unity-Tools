// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(AudioActorTag))]
    public class AudioActorTagEditor : Editor
    {
        SerializedProperty customTriggererTag;

        private void OnEnable()
        {
            customTriggererTag = serializedObject.FindProperty("customTriggererTag");
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as AudioActorTag;

            DrawDefaultInspector();

            serializedObject.Update();

            if (targetScript.triggererType == TriggererType.Custom)
            {
                EditorGUILayout.PropertyField(customTriggererTag);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}