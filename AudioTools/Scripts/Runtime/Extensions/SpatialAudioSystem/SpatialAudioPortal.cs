// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMODUnityTools
{ 
    [AddComponentMenu("FMOD Unity Tools/Extensions/Spatial Audio System/Spatial Audio Portal"), 
     Tooltip("Only instantiate Spatial Audio Portals with the 'Add Spatial Audio Protal\" -button of the Spatial Audio Manager.")]
    public class SpatialAudioPortal : MonoBehaviour
    {
        [Tooltip("Optionally, give the portal a unique name for more informative debug messages.")]
        public string portalName;

        [HideInInspector]
        public PortalInitialState initialState = PortalInitialState.Open;

        [HideInInspector]
        public BoxCollider portalCollider;
        
        [HideInInspector]
        public MeshRenderer meshRenderer;
        private List<SpatialAudioRoom> connectedRooms = new List<SpatialAudioRoom>();
        
        [HideInInspector]
        [Range(0.0f, 1.0f)]
        public float traversalCost = 0.0f;

        [HideInInspector]
        public AnimationCurve openEnvelope = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [HideInInspector]
        public AnimationCurve closeEnvelope = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));

        [Range(0.0f, 15.0f), HideInInspector]
        public float openFadeTime = 0.3f;

        [Range(0.0f, 15.0f), HideInInspector]
        public float closeFadeTime = 0.3f;

        /// <summary>0 = portal open, 1 = portal closed
        /// </summary>
        public float PortalStatus { get; private set; } = 0;

        [HideInInspector]
        public string debugPortalStatus = "";

        private bool inProgressOpen = false;
        private bool inProgressClose = false;

        public enum PortalInitialState
        {
            Open,
            Closed
        };

        void Awake()
        {
            if (initialState == PortalInitialState.Closed)
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
            if (PortalStatus < 1 && inProgressOpen)
                debugPortalStatus = "Portal status: Opening";
            else if(PortalStatus < 1 && inProgressClose)
                debugPortalStatus = "Portal status: Closing";
            else if(!inProgressClose && !inProgressOpen && PortalStatus < 1)
                debugPortalStatus = "Portal status: Open";
            else if(!inProgressClose && !inProgressOpen)
                debugPortalStatus = "Portal status: Closed";            
        }
#endif

        public void SetPortalClosed(bool allowFade)
        {
            if (allowFade)
            {
                StartCoroutine(ClosePortal(PortalStatus, closeFadeTime));
            }
            else
            {
                StartCoroutine(ClosePortal(PortalStatus, 0));
            }
        }

        public void SetPortalOpen(bool allowFade)
        {
            if (allowFade)
            {
                StartCoroutine(OpenPortal(PortalStatus, openFadeTime));
            }
            else
            {
                StartCoroutine(OpenPortal(PortalStatus, 0));
            }
        }

        public void SetConnectedRoom(SpatialAudioRoom room)
        {
            if (connectedRooms.Count >= 2)
            {
                string offendingPortal = (string.IsNullOrEmpty(portalName) ? gameObject.name : portalName);
                Debug.LogError("Spatial Audio Portal '" + offendingPortal + "' already has been assigned with a pair of rooms that it connects. " +
                               "Each portal can only connect one pair of rooms.");
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
            duration = PortalStatus * duration;
            float fadeTimePassed = 0f;
            inProgressOpen = true;
            inProgressClose = false;

            while (fadeTimePassed < duration && !inProgressClose)
            {
                fadeTimePassed += Time.deltaTime;
                float percent = Mathf.Clamp01(fadeTimePassed / duration);
                float curvePercent = Mathf.Clamp01(openEnvelope.Evaluate(percent));
                PortalStatus = start - (start * curvePercent);
                yield return null;
            }

            if (!inProgressClose)
            {
                PortalStatus = 0.0f;
            }

            inProgressOpen = false;
        }

        private IEnumerator ClosePortal(float start, float duration)
        {
            duration = (1 - PortalStatus) * duration;
            float fadeTimePassed = 0.0f;
            inProgressClose = true;
            inProgressOpen = false;

            while (fadeTimePassed < duration && !inProgressOpen)
            {
                fadeTimePassed += Time.deltaTime;
                float percent = Mathf.Clamp01(fadeTimePassed / duration);
                float curvePercent = Mathf.Clamp01(closeEnvelope.Evaluate(percent));
                float totalFade = 1.0f - start;
                PortalStatus = start + (totalFade * curvePercent);
                yield return null;
            }

            if (!inProgressOpen)
            {
                PortalStatus = 1.0f;
            }

            inProgressClose = false;
        }

        void OnValidate()
        {
            var copyPortalTransformScale = transform.localScale;        
            copyPortalTransformScale.z = 0;
            transform.localScale = copyPortalTransformScale;
        }
    }
}