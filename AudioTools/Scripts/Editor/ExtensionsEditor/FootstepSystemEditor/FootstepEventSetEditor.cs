// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FMODUnity;

namespace AudioTools
{
    [CustomEditor(typeof(FootstepEventSet), true)]
    public class FootstepEventSetEditor : Editor
    {
        SerializedProperty combinationData;
        SerializedProperty shoeOrFeetType;
        SerializedProperty surfaceType;
        SerializedProperty movementType;

        private void OnEnable()
        {      
            combinationData = serializedObject.FindProperty("combinationData");
            shoeOrFeetType = serializedObject.FindProperty("shoeOrFeetType");
            surfaceType = serializedObject.FindProperty("surfaceType");
            movementType = serializedObject.FindProperty("movementType");
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as FootstepEventSet; 

            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(shoeOrFeetType);
            EditorGUILayout.PropertyField(surfaceType);
            EditorGUILayout.PropertyField(movementType);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Combination")) 
            {
                for (int i = 0; i < targetScript.combinationData.Length; i++)
                {
                    FootstepEventSet.Combination comb = targetScript.combinationData[i];

                    if (comb != null)
                    {
                        if (comb.combination == targetScript.shoeOrFeetType.ToString() + "_" + targetScript.surfaceType.ToString() + "_" + targetScript.movementType.ToString())
                        {
                            EditorUtility.DisplayDialog("Duplicate Warning", "The combination '" + comb.combination + "' already exists for this Footstep EventSet.", "Ok");
                            return;
                        }
                    }
                }

                FootstepEventSet.Combination newCombination = new FootstepEventSet.Combination();
                newCombination.combination = targetScript.shoeOrFeetType.ToString() + "_" + targetScript.surfaceType.ToString() + "_" + targetScript.movementType.ToString();
                FootstepEventSet.Combination[] combinationsCopy = new FootstepEventSet.Combination[targetScript.combinationData.Length + 1];
                targetScript.combinationData.CopyTo(combinationsCopy, 0);
                combinationsCopy[combinationsCopy.Length - 1] = newCombination;
                targetScript.combinationData = combinationsCopy;

            }

            if (GUILayout.Button("Remove Combination"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete the combination '"
                                          + targetScript.shoeOrFeetType.ToString() + "_" + targetScript.surfaceType.ToString() + "_" + targetScript.movementType.ToString() + "'?",
                                          "Delete", "Cancel");

                if (doProceed)
                {
                    bool exists = false;

                    for (int i = targetScript.combinationData.Length - 1; i > -1; i--)
                    {
                        FootstepEventSet.Combination comb = targetScript.combinationData[i];

                        if (comb != null)
                        {
                            if (comb.combination == targetScript.shoeOrFeetType.ToString() + "_" + targetScript.surfaceType.ToString() + "_" + targetScript.movementType.ToString())
                            {
                                var tempList = new List<FootstepEventSet.Combination>(targetScript.combinationData);
                                tempList.RemoveAt(i);
                                var newArray = tempList.ToArray();
                                targetScript.combinationData = newArray;
                                exists = true;
                            }
                        }
                    }

                    if (!exists)
                    {
                        EditorUtility.DisplayDialog("Removing Failed", "Combination '"
                        + targetScript.shoeOrFeetType.ToString() + "_" + targetScript.surfaceType.ToString() + "_" + targetScript.movementType.ToString() + "' does not exist.", "Ok");
                    }
                }    
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear & Add All Combinations"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to add all combinations? This operation will first clear all current combinations.", 
                                                             "Proceed", "Cancel");

                if (doProceed)
                {
                    var emptyArray = new FootstepEventSet.Combination[0];
                    targetScript.combinationData = emptyArray;
                    AddAllCombinations();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove All Combinations"))
            {
                bool doProceed = EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete all combinations? This operation cannot be undone.","Delete", "Cancel");

                if (doProceed)
                {
                    var emptyArray = new FootstepEventSet.Combination[0];
                    targetScript.combinationData = emptyArray;
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
            EditorGUILayout.Space();

            serializedObject.Update();
                    
            for (int i = 0; i < combinationData.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(targetScript.combinationData[i].combination, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(EditorGUIUtility.fieldWidth));
                SerializedProperty combination = combinationData.GetArrayElementAtIndex(i).FindPropertyRelative("eventOptions");
                EditorGUILayout.PropertyField(combination, new GUIContent("Event Options"), true);
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddAllCombinations()
        {
            var targetScript = target as FootstepEventSet;

            foreach (ShoeOrFeetType shoeOrFeet in System.Enum.GetValues(typeof(ShoeOrFeetType)))
            {
                foreach (SurfaceType surfaceType in System.Enum.GetValues(typeof(SurfaceType)))
                {
                    foreach (MovementType movementType in System.Enum.GetValues(typeof(MovementType)))
                    {
                        FootstepEventSet.Combination newCombination = new FootstepEventSet.Combination();
                        newCombination.combination = shoeOrFeet.ToString() + "_" + surfaceType.ToString() + "_" + movementType.ToString();
                        FootstepEventSet.Combination[] combinationsCopy = new FootstepEventSet.Combination[targetScript.combinationData.Length + 1];
                        targetScript.combinationData.CopyTo(combinationsCopy, 0);
                        combinationsCopy[combinationsCopy.Length - 1] = newCombination;
                        targetScript.combinationData = combinationsCopy;
                    }
                }
            }
        }

        private void AutoFindEventReferences()
        {
            var targetScript = target as FootstepEventSet;
            var allEvents = FMODUnity.EventManager.Events;

            for (int i = 0; i < targetScript.combinationData.Length; i++)
            {
                FootstepEventSet.Combination comb = targetScript.combinationData[i];
                List<string> foundFmodEvents = new List<string>();

                if (!string.IsNullOrEmpty(comb.combination))
                {
                    foreach (EditorEventRef eventRef in allEvents)
                    {
                        if (eventRef.Path.Contains(comb.combination))
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