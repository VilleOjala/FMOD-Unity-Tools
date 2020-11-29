// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(FootstepPlaybackHandler))]
    public class FootstepPlaybackHandlerEditor : Editor
    {
        static bool showTriggeringStuff = true;
        static bool showSurfaceCheckStuff = true;
        static bool showEventSetImportStuff = true;

        //

        SerializedProperty referenceTransform;
        SerializedProperty animator;
        SerializedProperty leftFootPosition;
        SerializedProperty rightFootPosition;
        SerializedProperty foleyPosition;
        SerializedProperty groundedParameter;
        SerializedProperty crouchedParameter;
        SerializedProperty stairsUpParameter;
        SerializedProperty stairsDownParameter;
        SerializedProperty adjustMaximumVelocity;
        SerializedProperty movingThresholdVelocity;
        SerializedProperty crouchedWalkAdjustment;
        SerializedProperty crouchedRunAdjustment;
        SerializedProperty thresholdHeight;
        SerializedProperty crouchedThresholdAdjustment;
        SerializedProperty distanceWhenGrounded;
        SerializedProperty minLimit;
        SerializedProperty maxLimit;
        SerializedProperty walkLimit;
        SerializedProperty runLimit;

        //

        SerializedProperty fallbackSurface;
        SerializedProperty fallbackLayer;
        SerializedProperty raycastLayerMask;
        SerializedProperty queryTriggerInteraction;
        SerializedProperty raycastMaxDistance;
        SerializedProperty adjustRaycastOriginY;

        //

        SerializedProperty shoeOrFeetType;
        SerializedProperty foleyType;
        SerializedProperty footstepEventSet;
        SerializedProperty foleyEventSet;
        SerializedProperty layerEventSet;

        bool roomAware = false;

        void OnEnable()
        {
            referenceTransform = serializedObject.FindProperty("referenceTransform");
            animator = serializedObject.FindProperty("animator");
            leftFootPosition = serializedObject.FindProperty("leftFootPosition");
            rightFootPosition = serializedObject.FindProperty("rightFootPosition");
            foleyPosition = serializedObject.FindProperty("foleyPosition");
            groundedParameter = serializedObject.FindProperty("groundedParameter");
            crouchedParameter = serializedObject.FindProperty("crouchedParameter");
            stairsUpParameter = serializedObject.FindProperty("stairsUpParameter");
            stairsDownParameter = serializedObject.FindProperty("stairsDownParameter");
            adjustMaximumVelocity = serializedObject.FindProperty("adjustMaximumVelocity");
            movingThresholdVelocity = serializedObject.FindProperty("movingThresholdVelocity");
            crouchedWalkAdjustment = serializedObject.FindProperty("crouchedWalkAdjustment");
            crouchedRunAdjustment = serializedObject.FindProperty("crouchedRunAdjustment");
            thresholdHeight = serializedObject.FindProperty("thresholdHeight");
            crouchedThresholdAdjustment = serializedObject.FindProperty("crouchedThresholdAdjustment");
            distanceWhenGrounded = serializedObject.FindProperty("distanceWhenGrounded");
            minLimit = serializedObject.FindProperty("minLimit");
            maxLimit = serializedObject.FindProperty("maxLimit");
            walkLimit = serializedObject.FindProperty("walkLimit");
            runLimit = serializedObject.FindProperty("runLimit");

            //

            fallbackSurface = serializedObject.FindProperty("fallbackSurface");
            fallbackLayer = serializedObject.FindProperty("fallbackLayer");
            raycastLayerMask = serializedObject.FindProperty("raycastLayerMask");
            queryTriggerInteraction = serializedObject.FindProperty("queryTriggerInteraction");
            raycastMaxDistance = serializedObject.FindProperty("raycastMaxDistance");
            adjustRaycastOriginY = serializedObject.FindProperty("adjustRaycastOriginY");

            //

            shoeOrFeetType = serializedObject.FindProperty("shoeOrFeetType");
            foleyType = serializedObject.FindProperty("foleyType");
            footstepEventSet = serializedObject.FindProperty("footstepEventSet");
            foleyEventSet = serializedObject.FindProperty("foleyEventSet");
            layerEventSet = serializedObject.FindProperty("layerEventSet");
        }

        public override void OnInspectorGUI()
        {
            var targetScript = target as FootstepPlaybackHandler;

            serializedObject.Update();

            EditorGUILayout.Space();

            //

            showTriggeringStuff = EditorGUILayout.Foldout(showTriggeringStuff, "TRIGGERING");

            if (showTriggeringStuff)
            {
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(referenceTransform);
                EditorGUILayout.PropertyField(animator);
                EditorGUILayout.PropertyField(leftFootPosition);
                EditorGUILayout.PropertyField(rightFootPosition);
                EditorGUILayout.PropertyField(foleyPosition);
                EditorGUILayout.PropertyField(groundedParameter);
                EditorGUILayout.PropertyField(crouchedParameter);
                EditorGUILayout.PropertyField(stairsUpParameter);
                EditorGUILayout.PropertyField(stairsDownParameter);
                EditorGUILayout.PropertyField(adjustMaximumVelocity);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(new GUIContent("Adjust Triggering Velocities:"),
                                             ref targetScript.walkLimit, ref targetScript.runLimit, minLimit.intValue, maxLimit.floatValue);

                targetScript.SanityCheck();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Walk Limit:", walkLimit.floatValue.ToString("#0.00"));
                EditorGUILayout.LabelField("Run Limit:", runLimit.floatValue.ToString("#0.00"));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changed Triggering Velocities");
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(movingThresholdVelocity);
                EditorGUILayout.PropertyField(crouchedWalkAdjustment);
                EditorGUILayout.PropertyField(crouchedRunAdjustment);
                EditorGUILayout.PropertyField(thresholdHeight);
                EditorGUILayout.PropertyField(crouchedThresholdAdjustment);
                EditorGUILayout.PropertyField(distanceWhenGrounded);

                EditorGUILayout.LabelField("Left foot height: " + targetScript.leftHeightDebug);
                EditorGUILayout.LabelField("Right foot height: " + targetScript.rightHeightDebug);
                EditorGUILayout.LabelField("Animator velocity: " + targetScript.velocityDebug);

                EditorGUILayout.Space();
            }

            //

            showSurfaceCheckStuff = EditorGUILayout.Foldout(showSurfaceCheckStuff, "SURFACE CHECK");

            if (showSurfaceCheckStuff)
            {
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(fallbackSurface);
                EditorGUILayout.PropertyField(fallbackLayer);
                EditorGUILayout.PropertyField(raycastLayerMask);
                EditorGUILayout.PropertyField(queryTriggerInteraction);
                EditorGUILayout.PropertyField(raycastMaxDistance);
                EditorGUILayout.PropertyField(adjustRaycastOriginY);

                EditorGUILayout.Space();
            }

            //

            showEventSetImportStuff = EditorGUILayout.Foldout(showEventSetImportStuff, "PLAYBACK");

            if (showEventSetImportStuff)
            {
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(shoeOrFeetType, new GUIContent("Shoe Or Feet Type"));
                EditorGUILayout.PropertyField(foleyType, new GUIContent("Foley Type"));

                roomAware = EditorGUILayout.Toggle(new GUIContent("Import As Room Aware"), roomAware);

                if (roomAware)
                {
                    targetScript.spatialAudioRoomAware = true;
                }
                else
                {
                    targetScript.spatialAudioRoomAware = false;
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                // Footsteps
                EditorGUILayout.PropertyField(footstepEventSet, new GUIContent("Footstep Event Set"));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Import Footstep Event Set"))
                {
                    if (targetScript.footstepEventSet == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Footstep Event Set is null.", "Ok");
                    }
                    else if (targetScript.footstepEventSetImported)
                    {
                        EditorUtility.DisplayDialog("Error", "A Footstep Event Set has already been imported. " +
                                                             "Remove the old one first and then import the new set again.", "Ok");
                    }
                    else
                    {
                        ImportFootstepEventSet();
                    }
                }

                if (GUILayout.Button("Remove Footstep Event Set"))
                {
                    if (targetScript.footstepEventSetImported)
                    {
                        RemoveFootstepEventSet();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                // Foley

                EditorGUILayout.PropertyField(foleyEventSet, new GUIContent("Foley Event Set"));

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Import Foley Event Set"))
                {
                    if (targetScript.foleyEventSet == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Foley Event Set is null.", "Ok");
                    }
                    else if (targetScript.foleyEventSetImported)
                    {
                        EditorUtility.DisplayDialog("Error", "A Foley Event Set has already been imported. " +
                                                             "Remove the old one first and then import the new set again.", "Ok");
                    }
                    else
                    {
                        ImportFoleyEventSet();
                    }
                }

                if (GUILayout.Button("Remove Foley Event Set"))
                {
                    if (targetScript.foleyEventSetImported)
                    {
                        RemoveFoleyEventSet();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                // Layer

                EditorGUILayout.PropertyField(layerEventSet, new GUIContent("Layer Event Set"));

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Import Layer Event Set"))
                {
                    if (targetScript.layerEventSet == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Layer Event Set is null.", "Ok");
                    }
                    else if (targetScript.layerEventSetImported)
                    {
                        EditorUtility.DisplayDialog("Error", "A Layer Event Set has already been imported. " +
                                                             "Remove the old one first and then import the new set again.", "Ok");
                    }
                    else
                    {
                        ImportLayerEventSet();
                    }
                }

                if (GUILayout.Button("Remove Layer Event Set"))
                {
                    if (targetScript.layerEventSetImported)
                    {
                        RemoveLayerEventSet();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ImportFootstepEventSet()
        {
            var targetScript = target as FootstepPlaybackHandler;

            var data = targetScript.footstepEventSet.GetValidData();

            if (data.Count < 1)
            {
                EditorUtility.DisplayDialog("Import failed", "Footstep Event Set '" + targetScript.footstepEventSet.name + "' does not contain any valid data.", "Ok");
                return;
            }

            GameObject parentGameObj = new GameObject();
            parentGameObj.name = "Footsteps";
            parentGameObj.transform.parent = targetScript.transform;
            targetScript.footstepParentObject = parentGameObj;

            foreach (KeyValuePair<string, string[]> keyValuePair in data)
            {
                GameObject gameObj = new GameObject();
                gameObj.name = keyValuePair.Key;
                gameObj.transform.parent = parentGameObj.transform;

                AudioObject audioObject = gameObj.AddComponent<AudioObject>();
                audioObject.SetEventReferences(keyValuePair.Value);

                if (targetScript.spatialAudioRoomAware)
                {
                    audioObject.spatialAudioRoomAware = true;
                }

                FootstepPlaybackHandler.FootstepImport footstepImport = new FootstepPlaybackHandler.FootstepImport();
                footstepImport.key = keyValuePair.Key;
                footstepImport.audioObject = audioObject;
                targetScript.footstepImports.Add(footstepImport);
            }

            targetScript.footstepEventSetImported = true;
        }

        private void RemoveFootstepEventSet()
        {
            var targetScript = target as FootstepPlaybackHandler;

            foreach (FootstepPlaybackHandler.FootstepImport import in targetScript.footstepImports)
            {
                if (import.audioObject != null)
                {
                    DestroyImmediate(import.audioObject.gameObject);
                }
            }

            targetScript.footstepImports.Clear();
            DestroyImmediate(targetScript.footstepParentObject);
            targetScript.footstepParentObject = null;
            targetScript.footstepEventSetImported = false;
        }

        private void ImportFoleyEventSet()
        {
            var targetScript = target as FootstepPlaybackHandler;

            var data = targetScript.foleyEventSet.GetValidData();

            if (data.Count < 1)
            {
                EditorUtility.DisplayDialog("Import failed", "Foley Event Set '" + targetScript.foleyEventSet.name + "' does not contain any valid data.", "Ok");
                return;
            }

            GameObject parentGameObj = new GameObject();
            parentGameObj.name = "Foley";
            parentGameObj.transform.parent = targetScript.transform;
            targetScript.foleyParentObject = parentGameObj;

            foreach (KeyValuePair<string, string[]> keyValuePair in data)
            {
                GameObject gameObj = new GameObject();
                gameObj.name = keyValuePair.Key;
                gameObj.transform.parent = parentGameObj.transform;

                AudioObject audioObject = gameObj.AddComponent<AudioObject>();
                audioObject.SetEventReferences(keyValuePair.Value);

                if (targetScript.spatialAudioRoomAware)
                {
                    audioObject.spatialAudioRoomAware = true;
                }

                FootstepPlaybackHandler.FoleyImport foleyImport = new FootstepPlaybackHandler.FoleyImport();
                foleyImport.key = keyValuePair.Key;
                foleyImport.audioObject = audioObject;
                targetScript.foleyImports.Add(foleyImport);
            }

            targetScript.foleyEventSetImported = true;
        }

        private void RemoveFoleyEventSet()
        {
            var targetScript = target as FootstepPlaybackHandler;

            foreach (FootstepPlaybackHandler.FoleyImport import in targetScript.foleyImports)
            {
                if (import.audioObject != null)
                {
                    DestroyImmediate(import.audioObject.gameObject);
                }
            }

            targetScript.foleyImports.Clear();
            DestroyImmediate(targetScript.foleyParentObject);
            targetScript.foleyParentObject = null;
            targetScript.foleyEventSetImported = false;
        }

        private void ImportLayerEventSet()
        {
            var targetScript = target as FootstepPlaybackHandler;

            var data = targetScript.layerEventSet.GetValidData();

            if (data.Count < 1)
            {
                EditorUtility.DisplayDialog("Import failed", "Layer Event Set '" + targetScript.layerEventSet.name + "' does not contain any valid data.", "Ok");
                return;
            }

            GameObject parentGameObj = new GameObject();
            parentGameObj.name = "Layer";
            parentGameObj.transform.parent = targetScript.transform;
            targetScript.layerParentObject = parentGameObj;

            foreach (KeyValuePair<string, string[]> keyValuePair in data)
            {
                GameObject gameObj = new GameObject();
                gameObj.name = keyValuePair.Key;
                gameObj.transform.parent = parentGameObj.transform;

                AudioObject audioObject = gameObj.AddComponent<AudioObject>();
                audioObject.SetEventReferences(keyValuePair.Value);

                if (targetScript.spatialAudioRoomAware)
                {
                    audioObject.spatialAudioRoomAware = true;
                }

                FootstepPlaybackHandler.LayerImport layerImport = new FootstepPlaybackHandler.LayerImport();
                layerImport.key = keyValuePair.Key;
                layerImport.audioObject = audioObject;
                targetScript.layerImports.Add(layerImport);
            }
            targetScript.layerEventSetImported = true;
        }

        private void RemoveLayerEventSet()
        {
            var targetScript = target as FootstepPlaybackHandler;

            foreach (FootstepPlaybackHandler.LayerImport import in targetScript.layerImports)
            {
                if (import.audioObject != null)
                {
                    DestroyImmediate(import.audioObject.gameObject);
                }
            }
            targetScript.layerImports.Clear();
            DestroyImmediate(targetScript.layerParentObject);
            targetScript.layerParentObject = null;
            targetScript.layerEventSetImported = false;
        }
    }
}