// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Reverb Zone System/Reverb Blend Zone")]
    [RequireComponent(typeof(Rigidbody))]
    public class ReverbBlendZone : MonoBehaviour
    {
        [ParamRef]
        public string frontParameter;
        [ParamRef]
        public string backParameter;

        private PARAMETER_ID frontID;
        private PARAMETER_ID backID;

        public BlendAxis blendAxis = BlendAxis.z;

        [HideInInspector]
        public GameObject blendArea;
        [HideInInspector]
        public GameObject backWall;
        [HideInInspector]
        public GameObject frontWall;
        [HideInInspector]
        public GameObject leftWall;
        [HideInInspector]
        public GameObject rightWall;
        [HideInInspector]
        public GameObject downWall;
        [HideInInspector]
        public GameObject upWall;

        [HideInInspector]
        public Collider backWallCollider;
        [HideInInspector]
        public Collider frontWallCollider;
        [HideInInspector]
        public Collider leftWallCollider;
        [HideInInspector]
        public Collider rightWallCollider;
        [HideInInspector]
        public Collider downWallCollider;
        [HideInInspector]
        public Collider upWallCollider;

        private float checkInterval = 0.1f;
        Coroutine coroutine;

        AudioActorTag playerTag;
        private bool alreadyCachedPlayerTag = false;
        int insideCounter = 0;

        private bool initializationSuccesfull = false;

        public enum BlendAxis
        {
            z,
            y,
            x 
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

            PARAMETER_DESCRIPTION frontDescription;
            result = RuntimeManager.StudioSystem.getParameterDescriptionByName(frontParameter, out frontDescription);
            if (result != FMOD.RESULT.OK)
                return;
            else
            {
                frontID = frontDescription.id;
            }

            PARAMETER_DESCRIPTION backDescription;
            result = RuntimeManager.StudioSystem.getParameterDescriptionByName(backParameter, out backDescription);
            if (result != FMOD.RESULT.OK)
                return;
            else
            {
                backID = backDescription.id;
            }

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

                    RuntimeManager.StudioSystem.setParameterByID(frontID, positiveWeight, true);
                    RuntimeManager.StudioSystem.setParameterByID(backID, negativeWeight, true);
                }
            }
        }

        // Default value should have been set to '1' inside FMOD Studio
        private void ResetParameterValues()
        {
            if (initializationSuccesfull)
            {
                FMODUnity.RuntimeManager.StudioSystem.setParameterByID(frontID, 1, true);
                FMODUnity.RuntimeManager.StudioSystem.setParameterByID(backID, 1, true);
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