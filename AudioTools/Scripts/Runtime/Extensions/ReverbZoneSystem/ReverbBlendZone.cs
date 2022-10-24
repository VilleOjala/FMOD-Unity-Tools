// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Reverb Zone System/Reverb Blend Zone")]
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
        public Collider blendAreaCollider;
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
            {
                Debug.LogWarning("Parameters missing for ReverbBlendZone.");
                return;
            }

            var result = RuntimeManager.StudioSystem.getParameterDescriptionByName(frontParameter, out PARAMETER_DESCRIPTION frontDescription);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError(result);
                return;
            }

            frontID = frontDescription.id;
            result = RuntimeManager.StudioSystem.getParameterDescriptionByName(backParameter, out PARAMETER_DESCRIPTION backDescription);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError(result);
                return;
            }

            backID = backDescription.id;
         
            if (blendArea == null || backWall == null || frontWall == null ||
                backWallCollider == null || frontWallCollider == null || blendAreaCollider == null)
            {
                Debug.LogError("ReverbBlendZone is missing some of its components.");
                return;
            }

            initializationSuccesfull = true;
        }

        private void Update()
        {
            if (!initializationSuccesfull)
                return;

            if (blendAreaCollider == null)
            {
                ResetParameterValues();
                return;
            }

            bool foundListener = HelperMethods.TryGetListenerPosition(out Vector3 listenerPosition);

            if (!foundListener)
            {
                ResetParameterValues();
                return;
            }

            bool isInside = HelperMethods.CheckIfInsideCollider(listenerPosition, blendAreaCollider);

            if (isInside)
            {
                CalculateReverbBlending(listenerPosition);
            }
            else
            {
                ResetParameterValues();
            }
        }

        private void CalculateReverbBlending(Vector3 listenerPosition)
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

            Vector3 negativeWallClosestPoint = negativeCollider.ClosestPoint(listenerPosition);
            Vector3 positiveWallClosestPoint = positiveCollider.ClosestPoint(listenerPosition);
            float distanceToNegativeWall = Vector3.Distance(listenerPosition, negativeWallClosestPoint);
            float distanceToPositiveWall = Vector3.Distance(listenerPosition, positiveWallClosestPoint);
            float totalDistance = distanceToNegativeWall + distanceToPositiveWall;

            if (totalDistance > 0)
            {
                float positiveWeight = distanceToNegativeWall / totalDistance;
                float negativeWeight = distanceToPositiveWall / totalDistance;

                RuntimeManager.StudioSystem.setParameterByID(frontID, positiveWeight, true);
                RuntimeManager.StudioSystem.setParameterByID(backID, negativeWeight, true);
            }        
        }

        private void ResetParameterValues()
        {
            if (initializationSuccesfull)
            {
                RuntimeManager.StudioSystem.setParameterByID(frontID, 1, true);
                RuntimeManager.StudioSystem.setParameterByID(backID, 1, true);
            }
        }

        void OnDestroy()
        {
            if (initializationSuccesfull)
            {
                ResetParameterValues();
            }         
        }
    }
}