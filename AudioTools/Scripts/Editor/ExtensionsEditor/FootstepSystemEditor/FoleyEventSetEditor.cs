// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FMODUnity;

namespace AudioTools
{
    [CustomEditor(typeof(FoleyEventSet))]
    public class FoleyEventSetEditor : Editor
    {
        SerializedProperty foleyType;
        SerializedProperty movementType;
        SerializedProperty foleyData;

        private void OnEnable()
        {
            foleyType = serializedObject.FindProperty("foleyType");
            movementType = serializedObject.FindProperty("movementType");
            foleyData = serializedObject.FindProperty("foleyData");
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as FoleyEventSet;

            serializedObject.Update();

            EditorGUILayout.Space();
   
            EditorGUILayout.PropertyField(foleyType);
            EditorGUILayout.PropertyField(movementType);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Combination"))
            {
                for (int i = 0; i < targetScript.foleyData.Length; i++)
                {
                    FoleyEventSet.FoleyData data = targetScript.foleyData[i];

                    if (data != null)
                    {
                        if (data.foleyName == targetScript.foleyType.ToString() + "_" + targetScript.movementType.ToString())
                        {
                            EditorUtility.DisplayDialog("Duplicate Warning", "The combination '" + data.foleyName + 
                                                        "' already exists for this Foley Event Set.", "Ok");
                            return;
                        }
                    }
                }

                FoleyEventSet.FoleyData newData = new FoleyEventSet.FoleyData();
                newData.foleyName = targetScript.foleyType.ToString() + "_" + targetScript.movementType.ToString();
                FoleyEventSet.FoleyData[] dataCopy = new FoleyEventSet.FoleyData[targetScript.foleyData.Length + 1];
                targetScript.foleyData.CopyTo(dataCopy, 0);
                dataCopy[dataCopy.Length - 1] = newData;
                targetScript.foleyData = dataCopy;
            }

            if (GUILayout.Button("Remove Combination"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete the combination '"
                          + targetScript.foleyType.ToString() + "_" + targetScript.movementType.ToString() + "'?",
                          "Delete", "Cancel");

                if (doProceed)
                {
                    bool exists = false;

                    for (int i = targetScript.foleyData.Length - 1; i > -1; i--)
                    {
                        FoleyEventSet.FoleyData data = targetScript.foleyData[i];

                        if (data != null)
                        {
                            if (data.foleyName == targetScript.foleyType.ToString() + "_" + targetScript.movementType.ToString())
                            {
                                var tempList = new List<FoleyEventSet.FoleyData>(targetScript.foleyData);
                                tempList.RemoveAt(i);
                                var newArray = tempList.ToArray();
                                targetScript.foleyData = newArray;
                                exists = true;
                            }
                        }
                    }

                    if (!exists)
                    {
                        EditorUtility.DisplayDialog("Removing Failed", "Combination '" + targetScript.foleyType.ToString() + "_" 
                                                    + targetScript.movementType.ToString() + "' does not exist.", "Ok");
                    }
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
                    var emptyArray = new FoleyEventSet.FoleyData[0];
                    targetScript.foleyData = emptyArray;
                    AddAllCombinations(); 
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove All Combinations"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete all combinations? " +
                                                             "This operation cannot be undone.", "Delete", "Cancel");

                if (doProceed)
                {
                    var emptyArray = new FoleyEventSet.FoleyData[0];
                    targetScript.foleyData = emptyArray;
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

            for (int i = 0; i < foleyData.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(targetScript.foleyData[i].foleyName, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(EditorGUIUtility.fieldWidth));
                SerializedProperty options = foleyData.GetArrayElementAtIndex(i).FindPropertyRelative("eventOptions");
                EditorGUILayout.PropertyField(options, new GUIContent("Event Options"), true);
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
        private void AddAllCombinations()
        {
            var targetScript = target as FoleyEventSet;

            foreach (FoleyType foleyType in System.Enum.GetValues(typeof(FoleyType)))
            {
                foreach (MovementType movementType in System.Enum.GetValues(typeof(MovementType)))
                {
                    FoleyEventSet.FoleyData newFoleyData = new FoleyEventSet.FoleyData();
                    newFoleyData.foleyName = foleyType.ToString() + "_" + movementType.ToString();
                    FoleyEventSet.FoleyData[] foleyDataCopy = new FoleyEventSet.FoleyData[targetScript.foleyData.Length + 1];
                    targetScript.foleyData.CopyTo(foleyDataCopy, 0);
                    foleyDataCopy[foleyDataCopy.Length - 1] = newFoleyData;
                    targetScript.foleyData = foleyDataCopy;
                }
            }
        }

        private void AutoFindEventReferences()
        {
            var targetScript = target as FoleyEventSet;
            var allEvents = FMODUnity.EventManager.Events;

            for (int i = 0; i < targetScript.foleyData.Length; i++)
            {
                FoleyEventSet.FoleyData comb = targetScript.foleyData[i];
                List<string> foundFmodEvents = new List<string>();

                if (!string.IsNullOrEmpty(comb.foleyName))
                {
                    foreach (EditorEventRef eventRef in allEvents)
                    {
                        if (eventRef.Path.Contains(comb.foleyName))
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