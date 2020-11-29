// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Reverb Zone System/Reverb Blend Zone")]
    [RequireComponent(typeof(Rigidbody))]
    public class ReverbBlendZone : MonoBehaviour
    {
        [FMODUnity.ParamRef]
        public string frontParameter;
        [FMODUnity.ParamRef]
        public string backParameter;

        public BlendAxis blendAxis = BlendAxis.z;

        [HideInInspector]
        public GameObject blendArea = null;
        [HideInInspector]
        public GameObject backWall = null;
        [HideInInspector]
        public GameObject frontWall = null;
        [HideInInspector]
        public GameObject leftWall = null;
        [HideInInspector]
        public GameObject rightWall = null;
        [HideInInspector]
        public GameObject downWall = null;
        [HideInInspector]
        public GameObject upWall = null;

        [HideInInspector]
        public Collider backWallCollider = null;
        [HideInInspector]
        public Collider frontWallCollider = null;
        [HideInInspector]
        public Collider leftWallCollider = null;
        [HideInInspector]
        public Collider rightWallCollider = null;
        [HideInInspector]
        public Collider downWallCollider = null;
        [HideInInspector]
        public Collider upWallCollider = null;

        private float checkInterval = 0.1f; // 10 FPS
        Coroutine coroutine;

        AudioActorTag playerTag = null;
        private bool alreadyCachedPlayerTag = false;
        int insideCounter = 0;

        private bool initializationSuccesfull = false;

        public enum BlendAxis
        {
            z = 0,
            y = 1,
            x = 2
        }

        void Start()
        {
            if (blendArea != null)
            {
                var meshRenderer = blendArea.GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                }
            }

            if (string.IsNullOrEmpty(frontParameter) || string.IsNullOrEmpty(backParameter))
                return;

            FMOD.RESULT result;
            FMOD.Studio.PARAMETER_DESCRIPTION parameterDescription;

            result = FMODUnity.RuntimeManager.StudioSystem.getParameterDescriptionByName(frontParameter, out parameterDescription);
            if (result != FMOD.RESULT.OK)
                return;

            result = FMODUnity.RuntimeManager.StudioSystem.getParameterDescriptionByName(backParameter, out parameterDescription);
            if (result != FMOD.RESULT.OK)
                return;

            if (blendArea == null || backWall == null || frontWall == null ||
                backWallCollider == null || frontWallCollider == null)
                return;

            initializationSuccesfull = true;
        }

        IEnumerator BlendValueCheck()
        {
            while (insideCounter > 0)
            {
                CalculateBlendValue();
                yield return new WaitForSeconds(checkInterval);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

            if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
            {
                insideCounter++;

                if (insideCounter == 1 && initializationSuccesfull)
                {
                    if (!alreadyCachedPlayerTag)
                    {
                        playerTag = audioActorTag;
                        alreadyCachedPlayerTag = true;
                    }

                    CalculateBlendValue();
                    coroutine = StartCoroutine(BlendValueCheck());
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

            if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
            {
                insideCounter--;

                if (insideCounter == 0 && initializationSuccesfull)
                {
                    StopCoroutine(coroutine);
                }
            }
        }

        private void CalculateBlendValue()
        {
            if (playerTag != null)
            {
                Collider positiveCollider = null;
                Collider negativeCollider = null;

                switch (blendAxis)
                {
                    case BlendAxis.z:
                        positiveCollider = frontWallCollider;
                        negativeCollider = backWallCollider;
                        break;
                    case BlendAxis.y:
                        positiveCollider = upWallCollider;
                        negativeCollider = downWallCollider;
                        break;
                    case BlendAxis.x:
                        positiveCollider = rightWallCollider;
                        negativeCollider = leftWallCollider;
                        break;
                    default:
                        break;
                }

                Vector3 playerPosition = playerTag.transform.position;

                Vector3 negativeWallClosestPoint = negativeCollider.ClosestPoint(playerPosition);
                Vector3 positiveWallClosestPoint = positiveCollider.ClosestPoint(playerPosition);

                float distanceToNegativeWall = Vector3.Distance(playerPosition, negativeWallClosestPoint);
                float distanceToPositiveWall = Vector3.Distance(playerPosition, positiveWallClosestPoint);

                float totalDistance = distanceToNegativeWall + distanceToPositiveWall;

                if (totalDistance > 0)
                {
                    float positiveWeight = distanceToNegativeWall / totalDistance;
                    float negativeWeight = distanceToPositiveWall / totalDistance;

                    FMODUnity.RuntimeManager.StudioSystem.setParameterByName(frontParameter, positiveWeight, true);
                    FMODUnity.RuntimeManager.StudioSystem.setParameterByName(backParameter, negativeWeight, true);
                }
            }
        }

        // Default value should have been set to '1' inside FMOD Studio
        private void ResetParameterValues()
        {
            if (initializationSuccesfull)
            {
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName(frontParameter, 1, true);
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName(backParameter, 1, true);
            }
        }

        void OnDestroy()
        {
            if (initializationSuccesfull)
            {
                ResetParameterValues();
            }         
        }

        void Reset()
        {
            gameObject.name = "ReverbBlendZone";
            var rigidBody = gameObject.GetComponent<Rigidbody>();

            if (rigidBody != null)
            {
                rigidBody.isKinematic = true;
                rigidBody.useGravity = false;
                rigidBody.hideFlags = HideFlags.NotEditable;
            }
        }
    }
}