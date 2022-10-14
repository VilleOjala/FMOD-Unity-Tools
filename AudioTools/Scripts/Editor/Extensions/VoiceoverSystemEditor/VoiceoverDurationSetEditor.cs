// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Globalization;

namespace FMODUnityTools
{
    [CustomEditor(typeof(KeyOffsetData))]
    public class VoiceoverDurationSetEditor : Editor
    {
        SerializedProperty keyToRemove;

        void OnEnable()
        {
            keyToRemove = serializedObject.FindProperty("keyToRemove");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as KeyOffsetData;
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
                        bool isFloat = float.TryParse(valueName, NumberStyles.Any, CultureInfo.InvariantCulture, out valueFloat);

                        if (!string.IsNullOrEmpty(valueName) && isFloat)
                        {
                            var newKeyOffset = new KeyOffsetData.KeyOffset();
                            newKeyOffset.key = keyName;
                            newKeyOffset.offset = valueFloat;
                            targetScript.keyOffsets.Add(newKeyOffset);
                        }                     
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Empty"))
            {
                targetScript.keyOffsets.Add(null);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Remove All"))
            {
                bool proceeed = EditorUtility.DisplayDialog("Confirm Removal", "Are you sure you want to remove all keys? " +
                                                             "This action cannot be undone.", "Remove", "Cancel");
                if (proceeed)
                {
                    targetScript.keyOffsets.Clear();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Remove Key"))
            {
                bool keyFound = false;

                for (int i = targetScript.keyOffsets.Count -1; i >= 0; i--)
                {
                    var key = targetScript.keyOffsets[i].key;

                    if (key == targetScript.keyToRemove)
                    {
                        bool proceeed = EditorUtility.DisplayDialog("Confirm Key Removal", "Are you sure you want to delete the key '" + key + 
                                                                    "' at list index " + i + "?", "Remove", "Cancel");
                        if (proceeed)
                        {
                            targetScript.keyOffsets.RemoveAt(i);
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