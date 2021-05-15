// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AudioTools
{
    [CustomEditor(typeof(VoiceoverDurationSet))]
    public class VoiceoverDurationSetEditor : Editor
    {
        SerializedProperty keyToRemove;

        void OnEnable()
        {
            keyToRemove = serializedObject.FindProperty("keyToRemove");
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as VoiceoverDurationSet;

            DrawDefaultInspector();

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import As Text File"))
            {
                if (targetScript.textFile != null)
                {
                    var text = targetScript.textFile.text;
                    string[] keyValuePairs = Regex.Split(text, "\n|\r|\n\r");

                    for (int i = 0; i < keyValuePairs.Length; i++)
                    {
                        if (string.IsNullOrEmpty(keyValuePairs[i]))
                            continue;

                        var keyValuePair = keyValuePairs[i];
                        var split = keyValuePair.Split(',');

                        if (split.Length < 2)
                            continue;

                        string keyName = split[0];
                        string valueName = split[1];
                        float valueFloat;

                        bool isFloat = float.TryParse(valueName, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out valueFloat);

                        if (!string.IsNullOrEmpty(valueName) && isFloat && valueFloat >= 0)
                        {
                            var newKeyDuration = new VoiceoverDurationSet.KeyDuration();
                            newKeyDuration.key = keyName;
                            newKeyDuration.duration = valueFloat;
                            targetScript.keyDurations.Add(newKeyDuration);
                        }                     
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Empty"))
            {
                targetScript.keyDurations.Add(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove All"))
            {
                bool proceeed = EditorUtility.DisplayDialog("Confirm Removal", "Are you sure you want to remove all keys? " +
                                                             "This action cannot be undone.", "Remove", "Cancel");

                if (proceeed)
                {
                    targetScript.keyDurations.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Key"))
            {
                bool keyFound = false;

                for (int i = targetScript.keyDurations.Count -1; i >= 0; i--)
                {
                    var key = targetScript.keyDurations[i].key;

                    if(key == targetScript.keyToRemove)
                    {
                        bool proceeed = EditorUtility.DisplayDialog("Confirm Key Removal", "Are you sure you want to delete the key '" + key + 
                                                                    "' at list index " + i + "?", "Remove", "Cancel");

                        if(proceeed)
                        {
                            targetScript.keyDurations.RemoveAt(i);
                            keyFound = true;
                        }
                    }
                }

                if (!keyFound)
                {
                    EditorUtility.DisplayDialog("Key Not Found", "No keys with the name '" + targetScript.keyToRemove + "' were found.", "Ok");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(keyToRemove);

            serializedObject.ApplyModifiedProperties();
        }
    }    
}