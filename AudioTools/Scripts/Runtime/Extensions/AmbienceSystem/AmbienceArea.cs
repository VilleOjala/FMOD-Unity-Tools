// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/AmbienceSystem/Ambience Area")]
    public class AmbienceArea : MonoBehaviour
    {
        public EventReference feederEvent;
        private EventDescription feederDescription;
        private EventInstance feederInstance; 
        public EventReference outputEvent;
        private EventDescription outputDescription;
        private EventInstance outputInstance;
        public EventReference portalOutputEvent;
        private EventDescription portalOutputDescription;

        [SerializeField]
        private List<PortalOutput> portalOutputs = new List<PortalOutput>();
        private float portalOutputMaxDistance;
        public AudioTriggerArea triggerArea;
        private bool isInsideAmbienceArea;

        [SerializeField, Tooltip("Don't allow additive volume when the listener is within hearing range of multiple portal outputs. " +
                                 "Each portal output's contribution to the total experienced volume remains relative to its distance to the listener. " +
                                 "Portal output event has to use the default roll-off curve for this to work correctly.")]
        private bool usePortalOutputWeighting = true;

        private const float PortalStartStopPadding = 5.0f;

        [SerializeField]
        private bool debugVisualizePortalOutputs;

        [Serializable]
        private class PortalOutput
        {
            public Collider portalCollider; 
            [HideInInspector] 
            public bool isWithinRange;
            [HideInInspector] 
            public float normalizedCloseness;
            [HideInInspector] 
            public float weight;
            [HideInInspector] public Vector3 closestPosition;
            public EventInstance eventInstance;
            public SpatialAudioRoom spatialAudioRoom;
        }

        private void Start()
        {
            HelperMethods.TryRetrieveDescriptionIfNotAlreadyValid(feederEvent, ref feederDescription);
            HelperMethods.TryRetrieveDescriptionIfNotAlreadyValid(outputEvent, ref outputDescription);
            HelperMethods.TryRetrieveDescriptionIfNotAlreadyValid(portalOutputEvent, ref portalOutputDescription);

            if (portalOutputDescription.isValid())
            {
                portalOutputDescription.is3D(out bool is3D);

                if (is3D)
                {
                    portalOutputDescription.getMinMaxDistance(out float minDistance, out float maxDistance);
                    portalOutputMaxDistance = maxDistance;
                }
                else
                {
                    Debug.LogError("EventReference for portal outputs is not for a 3D event.");
                }
            }

            if (triggerArea != null)
            {
                triggerArea.Triggered += TriggeredHandler;
            }
        }

        private void OnDisable()
        {
            if (triggerArea != null)
            {
                triggerArea.Triggered -= TriggeredHandler;
            }

            StopEverything();
        }

        private void Update()
        {
            if (triggerArea == null)
            {
                Debug.LogWarning("AudioTriggerArea reference is null for AmbienceArea: " + gameObject.name);
                StopEverything();
                return;
            }

            bool foundListener = HelperMethods.TryGetListenerPosition(out Vector3 listenerPosition);

            if (!foundListener)
            {
                Debug.LogWarning("Listener position could not be determined.");
                return;
            }

            bool hasPortalOutputsWithinRange = false;
            float combinedNormalizedClosenesses = 0;

            foreach (var portalOutput in portalOutputs)
            {
                if (portalOutput == null)
                    continue;

                if (portalOutput.portalCollider == null || portalOutput.portalCollider == null)
                {
                    portalOutput.isWithinRange = false;
                    continue;
                }

                portalOutput.closestPosition = portalOutput.portalCollider.ClosestPoint(listenerPosition);
                float distance = Vector3.Distance(listenerPosition, portalOutput.closestPosition);
                portalOutput.weight = 1f;
               
                if (distance <= portalOutputMaxDistance + PortalStartStopPadding)
                {
                    hasPortalOutputsWithinRange = true;
                    portalOutput.isWithinRange = true;

                    if (portalOutputMaxDistance > 0)
                    {
                        // This assumes that the default 'linear squared' roll-off curve in FMOD Spatializer is being used. 
                        // Unfortunately, there is currently no handy way of polling the curve setting at runtime, which would
                        // allow automatic adjustment of this equation.
                        portalOutput.normalizedCloseness = Mathf.Pow(Mathf.Clamp01(1 - (distance / portalOutputMaxDistance)), 2f);
                        combinedNormalizedClosenesses += portalOutput.normalizedCloseness;
                    }
                }
                else
                {
                    portalOutput.isWithinRange = false;
                }
            }

            if (usePortalOutputWeighting)
            {
                foreach (var portalOutput in portalOutputs)
                {
                    if (portalOutput == null || !portalOutput.isWithinRange)
                        continue;

                    if (combinedNormalizedClosenesses > 0)
                    {
                        portalOutput.weight = portalOutput.normalizedCloseness / combinedNormalizedClosenesses;
                    }
                    else
                    {
                        portalOutput.weight = 1f;
                    }
                }
            }

            if (isInsideAmbienceArea && hasPortalOutputsWithinRange)
            {
                StartFeederIfNotPlaying();
                StartOrUpdateOutput(1f);
                StartOrUpdateWithinRangePortalOutputs(isAudible: false);
                StopOutsideRangePortalOutputs();
            }
            else if (!isInsideAmbienceArea && hasPortalOutputsWithinRange)
            {
                StartFeederIfNotPlaying();
                StartOrUpdateOutput(0f);
                StartOrUpdateWithinRangePortalOutputs(isAudible: true);
                StopOutsideRangePortalOutputs();
            }
            else if (isInsideAmbienceArea && !hasPortalOutputsWithinRange)
            {
                StartFeederIfNotPlaying();
                StartOrUpdateOutput(1f);
                StopOutsideRangePortalOutputs();
            }
            else
            {
                StopFeeder();
                StopOutput();
                StopAllPortalOutputs(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }

        private void StartFeederIfNotPlaying()
        {
            if (!feederInstance.isValid() && feederDescription.isValid())
            {
                feederDescription.createInstance(out feederInstance);
                feederInstance.setVolume(0f);
                feederInstance.start();
            }
        }

        private void StopFeeder()
        {
            if (feederInstance.isValid())
            {
                feederInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                feederInstance.release();
            }
        }

        private void StartOrUpdateOutput(float volume)
        {
            if (outputInstance.isValid())
            {
                outputInstance.setVolume(volume);
                return;
            }

            if (outputDescription.isValid())
            {
                outputDescription.createInstance(out outputInstance);
                outputInstance.setVolume(volume);
                outputInstance.start();
            }
        }

        private void StopOutput()
        {
            if (outputInstance.isValid())
            {
                outputInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                outputInstance.release();
            }
        }

        private void StartOrUpdateWithinRangePortalOutputs(bool isAudible)
        {
            if (!portalOutputDescription.isValid())
                return;

            foreach (var portalOutput in portalOutputs)
            {
                if (portalOutput == null || !portalOutput.isWithinRange)
                    continue;

                float volume = isAudible ? portalOutput.weight : 0f;

                if (!portalOutput.eventInstance.isValid())
                {
                    portalOutputDescription.createInstance(out portalOutput.eventInstance);
                    portalOutput.eventInstance.setVolume(volume);
                    var attributes = RuntimeUtils.To3DAttributes(portalOutput.closestPosition);
                    portalOutput.eventInstance.set3DAttributes(attributes);
                    portalOutput.eventInstance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, 0f); // Force spatializer min distance to zero

                    if (SpatialAudioManager.Instance != null && portalOutput.spatialAudioRoom != null)
                    {
                        SpatialAudioManager.Instance.RegisterRoomAwareInstance(portalOutput.eventInstance, portalOutput.spatialAudioRoom);
                    }

                    portalOutput.eventInstance.start();
                }
                else
                {
                    portalOutput.eventInstance.setVolume(volume);
                    var attributes = RuntimeUtils.To3DAttributes(portalOutput.closestPosition);
                    portalOutput.eventInstance.set3DAttributes(attributes);
                }
            }
        }
        
        private void StopOutsideRangePortalOutputs()
        {
            foreach (var portalOutput in portalOutputs)
            {
                if (portalOutput == null || portalOutput.isWithinRange)
                    continue;

                portalOutput.eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                portalOutput.eventInstance.release();
            }
        }
        
        private void StopAllPortalOutputs(FMOD.Studio.STOP_MODE stopMode)
        {
            foreach (var portalOutput in portalOutputs)
            {
                if (portalOutput == null)
                    continue;

                if (portalOutput.eventInstance.isValid())
                {
                    portalOutput.eventInstance.stop(stopMode);
                    portalOutput.eventInstance.release();
                }
            }
        }
        
        private void StopEverything()
        {
            StopFeeder();
            StopOutput();
            StopAllPortalOutputs(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        private void TriggeredHandler(TriggerEventType triggerEventType)
        {
            if (triggerEventType == TriggerEventType.TriggerEnter)
            {
                isInsideAmbienceArea = true;
            }
            else if (triggerEventType == TriggerEventType.TriggerExit)
            {
                isInsideAmbienceArea = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (debugVisualizePortalOutputs)
            {
                foreach (var portalOutput in portalOutputs)
                {
                    if (portalOutput == null)
                        continue;
                    
                    if (portalOutput.isWithinRange)
                    {
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                    }

                    Gizmos.DrawWireSphere(portalOutput.closestPosition, portalOutputMaxDistance);
                }
            }
        }
    }
}