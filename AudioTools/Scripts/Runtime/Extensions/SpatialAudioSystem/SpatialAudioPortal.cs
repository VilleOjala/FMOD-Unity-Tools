// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    // Only instantiate Spatial Audio Portals with the 'Add Spatial Audio Protal" -button of the Spatial Audio Manager.

    [AddComponentMenu("Audio Tools/Extensions/Spatial Audio System/Spatial Audio Portal")]
    public class SpatialAudioPortal : MonoBehaviour
    {
        [Tooltip("Optionally give the portal a unique name for more informative debug messages.")]
        public string portalName;

        public PortalType portalType = PortalType.Opening;

        [HideInInspector]
        public PortalInitialState initialState = PortalInitialState.Open;

        [HideInInspector]
        public BoxCollider portalCollider;
        
        [HideInInspector]
        public MeshRenderer meshRenderer;

        private List<SpatialAudioRoom> connectedRooms = new List<SpatialAudioRoom>();
        
        [Range(0.0f, 1.0f), HideInInspector]
        public float wallOcclusion = 1.0f;

        [HideInInspector]
        [Range(0.0f, 1.0f)]
        public float traversalMaxCost = 0.0f;

        [HideInInspector]
        public AnimationCurve openEnvelope = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [HideInInspector]
        public AnimationCurve closeEnvelope = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));

        [Range(0.0f, 10.0f), HideInInspector]
        public float openFadeTime = 0.3f;

        [Range(0.0f, 10.0f), HideInInspector]
        public float closeFadeTime = 0.3f;

        // 0 = portal open
        // 1 = portal closed
        private float portalStatus = 0; 
        public float PortalStatus
        {
            get { return portalStatus; }
        }

        [HideInInspector]
        public string debugPortalStatus = "";

        private bool inProgressOpen = false;
        private bool inProgressClose = false;

        public enum PortalType
        {
            Opening = 0,
            Wall = 1
        };

        public enum PortalInitialState
        {
            Open = 0,
            Closed = 1,
            ControlledFromOutside = 2
        };

        void Awake()
        {
            // Portals are open by default, so no need to ever explicitly set them as such.
            // The 'Controlled From Outside' is for marking portals the openness state of which is controlled by another script
            // <- e.g. code related to the status of physical doors etc.
            // <- Important to use this mode if the order of execution of scripts is unclear.
            if (portalType == PortalType.Opening && initialState == PortalInitialState.Closed)
            {
                SetPortalClosed(false);
            }       

            // In case we have forgotten to disable the debug trigger mesh renderer.
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }                    
        }

#if UNITY_EDITOR
        void Update()
        {
            if (portalType == PortalType.Opening)
            {
                if (portalStatus < 1 && inProgressOpen)
                    debugPortalStatus = "Portal status: Opening";
                else if(portalStatus < 1 && inProgressClose)
                    debugPortalStatus = "Portal status: Closing";
                else if(!inProgressClose && !inProgressOpen && portalStatus < 1)
                    debugPortalStatus = "Portal status: Open";
                else if(!inProgressClose && !inProgressOpen)
                    debugPortalStatus = "Portal status: Closed";
            }
        }
#endif

        public void SetPortalClosed(bool allowFade)
        {
            if (portalType == PortalType.Wall)
            {
                Debug.LogWarning("Cannot set portal '" + gameObject.name + "' as 'closed', since it is a wall.");
                return;
            }

            if (allowFade)
                StartCoroutine(ClosePortal(portalStatus, closeFadeTime));
            else
                StartCoroutine(ClosePortal(portalStatus, 0));
        }

        public void SetPortalOpen(bool allowFade)
        {
            if (portalType == PortalType.Wall)
            {
                Debug.LogWarning("Cannot set portal '" + gameObject.name + "' as 'open', since it is a wall.");
                return;
            }

            if (allowFade)
                StartCoroutine(OpenPortal(portalStatus, openFadeTime));
            else
                StartCoroutine(OpenPortal(portalStatus, 0));
        }

        public void SetConnectedRoom(SpatialAudioRoom room)
        {
            if (connectedRooms.Count >= 2)
            {
                string offendingPortal = (string.IsNullOrEmpty(portalName) ? gameObject.name : portalName);
                Debug.LogError("Spatial Audio Portal '" + offendingPortal + "' already has been assigned with a pair of rooms that it connects. " +
                               "Each portal can only connect one room pair.");
            }
            else if (room != null)
            {
                connectedRooms.Add(room);
            }
        }

        public List<SpatialAudioRoom> GetConnectedRooms()
        {
            return connectedRooms;
        }

        private IEnumerator OpenPortal(float start, float duration)
        {
            duration = portalStatus * duration;
            float fadeTimePassed = 0.0f;
            inProgressOpen = true;
            inProgressClose = false;

            while (fadeTimePassed < duration && !inProgressClose)
            {
                fadeTimePassed += Time.deltaTime;
                float percent = Mathf.Clamp01(fadeTimePassed / duration);
                float curvePercent = openEnvelope.Evaluate(percent);
                curvePercent = Mathf.Clamp01(curvePercent);
                portalStatus = start - (start * curvePercent);

                yield return null;
            }

            if (!inProgressClose)
            {
                portalStatus = 0.0f;
            }

            inProgressOpen = false;
        }

        private IEnumerator ClosePortal(float start, float duration)
        {
            duration = (1 - portalStatus) * duration;
            float fadeTimePassed = 0.0f;
            inProgressClose = true;
            inProgressOpen = false;

            while (fadeTimePassed < duration && !inProgressOpen)
            {
                fadeTimePassed += Time.deltaTime;
                float percent = Mathf.Clamp01(fadeTimePassed / duration);
                float curvePercent = closeEnvelope.Evaluate(percent);
                curvePercent = Mathf.Clamp01(curvePercent);
                float totalFade = 1.0f - start;
                portalStatus = start + (totalFade * curvePercent);

                yield return null;
            }

            if (!inProgressOpen)
            {
                portalStatus = 1.0f;
            }

            inProgressClose = false;
        }

        void OnValidate()
        {
            var copyPortalTransformScale = transform.localScale;        
            copyPortalTransformScale.z = 0;
            transform.localScale = copyPortalTransformScale;

            if (!string.IsNullOrEmpty(portalName))
                gameObject.name = portalName;
            else
                gameObject.name = "SpatialAudioPortal";
        }

        void Reset()
        {
            // Check if a layer with the name "AudioTooslPortal" has been created.
            // If found, automatically assign this layer to the portal gameObject.
            int layerIndex = LayerMask.NameToLayer("AudioToolsPortal");

            if (layerIndex > -1)
            {
                gameObject.layer = layerIndex;
            }
        }
    }
}