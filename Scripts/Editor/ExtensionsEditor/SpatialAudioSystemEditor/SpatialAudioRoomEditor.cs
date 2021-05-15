// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(SpatialAudioRoom))]
    public class SpatialAudioRoomEditor : Editor
    {
        private int elementToRemove = 0;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetScript = target as SpatialAudioRoom;

            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button("Add Room Connection"))
            {
                targetScript.roomConnections.Add(null); 
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button("Remove Room Connection"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to remove Element " + 
                                                             elementToRemove + "?. This action cannot be undone.",
                                                             "Delete", "Cancel");

                if (doProceed && targetScript.roomConnections.Count > 0)
                {
                    if (elementToRemove < 0 || elementToRemove > targetScript.roomConnections.Count - 1)
                    {
                        EditorUtility.DisplayDialog("Error", "Removal failed. Element index was invalid.", "Ok");
                        return; 
                    }
                    
                    targetScript.roomConnections.RemoveAt(elementToRemove);
                }
            }
            EditorGUILayout.EndHorizontal();
           
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            elementToRemove = EditorGUILayout.IntField("Element #:", elementToRemove);
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
        }        
    }
}