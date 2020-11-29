// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FMODUnity;

namespace AudioTools
{
    [CustomEditor(typeof(LayerEventSet))]
    public class LayerEventSetEditor : Editor
    {
        SerializedProperty layerType;
        SerializedProperty movementType;
        SerializedProperty layerData;

        private void OnEnable()
        {
            layerType = serializedObject.FindProperty("layerType");
            movementType = serializedObject.FindProperty("movementType");
            layerData = serializedObject.FindProperty("layerData");
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as LayerEventSet;

            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(layerType);
            EditorGUILayout.PropertyField(movementType);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Combination"))
            {
                if (targetScript.layerType == SurfaceLayerType.None)
                {
                    EditorUtility.DisplayDialog("Error", "Cannot create a Layer Event Set for the Surface Layer Type '" + SurfaceLayerType.None + "'.", "Ok");
                    return;
                }

                for (int i = 0; i < targetScript.layerData.Length; i++)
                {
                    LayerEventSet.LayerData data = targetScript.layerData[i];

                    if (data != null)
                    {
                        if (data.layerName == targetScript.layerType.ToString() + "_" + targetScript.movementType.ToString())
                        {
                            EditorUtility.DisplayDialog("Duplicate Warning", "The layer type '" + data.layerName + "' already exists for this Layer Event Set.", "Ok");
                            return;
                        }
                    }
                }

                LayerEventSet.LayerData newData = new LayerEventSet.LayerData();
                newData.layerName = targetScript.layerType.ToString() + "_" + targetScript.movementType.ToString();
                LayerEventSet.LayerData[] dataCopy = new LayerEventSet.LayerData[targetScript.layerData.Length + 1];
                targetScript.layerData.CopyTo(dataCopy, 0);
                dataCopy[dataCopy.Length - 1] = newData;
                targetScript.layerData = dataCopy;

            }

            if (GUILayout.Button("Remove Combination"))
            {
                bool exists = false;

                for (int i = targetScript.layerData.Length - 1; i > -1; i--)
                {
                    LayerEventSet.LayerData data = targetScript.layerData[i];

                    if (data != null)
                    {
                        if (data.layerName == targetScript.layerType.ToString() + "_" + targetScript.movementType.ToString())
                        {
                            var tempList = new List<LayerEventSet.LayerData>(targetScript.layerData);
                            tempList.RemoveAt(i);
                            var newArray = tempList.ToArray();
                            targetScript.layerData = newArray;
                            exists = true;
                        }
                    }
                }

                if (!exists)
                {
                    EditorUtility.DisplayDialog("Removing Failed", "Layer type '"
                    + targetScript.layerType.ToString() + "_" + targetScript.movementType.ToString() + "' does not exist.", "Ok");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear & Add All Combinations"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to add all combinations? " +
                                                             "This operation will first clear all current combinations.",
                                                             "Proceed", "Cancel");

                if (doProceed)
                {
                    var emptyArray = new LayerEventSet.LayerData[0];
                    targetScript.layerData = emptyArray;
                    AddAllCombinations();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove All Combinations"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete all combinations? This operation cannot be undone.", "Delete", "Cancel");

                if (doProceed)
                {
                    var emptyArray = new LayerEventSet.LayerData[0];
                    targetScript.layerData = emptyArray;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve FMOD Event References"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to try automatically retrieving FMOD event references? " +
                                                             "This will erase all currently added event references. " +
                                                             "The event name in FMOD Studio must contain the combination string.", "Proceed", "Cancel");
                if (doProceed)
                {
                    AutoFindEventReferences();
                }
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            serializedObject.Update();

            for (int i = 0; i < layerData.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(targetScript.layerData[i].layerName, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(EditorGUIUtility.fieldWidth));
                SerializedProperty options = layerData.GetArrayElementAtIndex(i).FindPropertyRelative("eventOptions");
                EditorGUILayout.PropertyField(options, new GUIContent("Event Options"), true);
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddAllCombinations()
        {
            var targetScript = target as LayerEventSet;

            foreach (SurfaceLayerType layerType in System.Enum.GetValues(typeof(SurfaceLayerType)))
            {
                if (layerType == SurfaceLayerType.None)
                {
                    continue;
                }

                foreach (MovementType movementType in System.Enum.GetValues(typeof(MovementType)))
                {
                    LayerEventSet.LayerData newLayerData = new LayerEventSet.LayerData();
                    newLayerData.layerName = layerType.ToString() + "_" + movementType.ToString();
                    LayerEventSet.LayerData[] layerDataCopy = new LayerEventSet.LayerData[targetScript.layerData.Length + 1];
                    targetScript.layerData.CopyTo(layerDataCopy, 0);
                    layerDataCopy[layerDataCopy.Length - 1] = newLayerData;
                    targetScript.layerData = layerDataCopy;
                }
            }
        }

        private void AutoFindEventReferences()
        {
            var targetScript = target as LayerEventSet;
            var allEvents = FMODUnity.EventManager.Events;

            for (int i = 0; i < targetScript.layerData.Length; i++)
            {
                LayerEventSet.LayerData comb = targetScript.layerData[i];
                List<string> foundFmodEvents = new List<string>();

                if (!string.IsNullOrEmpty(comb.layerName))
                {
                    foreach (EditorEventRef eventRef in allEvents)
                    {
                        if (eventRef.Path.Contains(comb.layerName))
                        {
                            foundFmodEvents.Add(eventRef.Path);
                        }
                    }

                    foundFmodEvents.Sort();
                    var eventOptionsArray = foundFmodEvents.ToArray();
                    comb.eventOptions = eventOptionsArray;
                }
            }
        }
    }
}