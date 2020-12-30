// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Ambience Systems/Opening Ambience Transitioner")]
    public class OpeningAmbienceTransitioner : MonoBehaviour
    {
        [FMODUnity.EventRef]
        public string baseAmbience;
        private FMOD.Studio.EventInstance baseAmbienceInstance;

        public List<AudioTriggerArea> audioTriggerAreas = new List<AudioTriggerArea>();
        private List<AudioTriggerArea> validAudioTriggerAreas = new List<AudioTriggerArea>();
        public List<OpeningSpotAmbience> openingSpotAmbiences = new List<OpeningSpotAmbience>();
        private List<OpeningSpotAmbience> enteredSpotAmbienceAreas = new List<OpeningSpotAmbience>();
        
        // 1 = Inside
        // 0 = Outside
        public int InsideStatus { get; private set; } = 0;
        private bool ambienceStarted = false;

        private int insideCounter = 0;

        private bool initializationSuccesfull = false;

        void Awake()
        {
            if (string.IsNullOrEmpty(baseAmbience))
            {
                Debug.LogError("FMOD event reference is null or empty for Base Ambience Area " + gameObject.name + ".");
                return;
            }

            FMOD.Studio.EventDescription eventDescription;

            FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.getEvent(baseAmbience, out eventDescription);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError("FMOD event reference is not valid for base ambience " + gameObject.name + ".");
                return;
            }

            if (audioTriggerAreas != null && audioTriggerAreas.Count > 0)
            {         
                foreach (var audioTriggerArea in audioTriggerAreas)
                {
                    if (audioTriggerArea != null)
                    {
                        audioTriggerArea.OnTriggerAreaEvent += AudioTriggerArea_OnTriggerAreaEvent;
                        validAudioTriggerAreas.Add(audioTriggerArea);
                    }
                }
            }

            foreach (var spotAmbience in openingSpotAmbiences)
            {
                if (spotAmbience != null)
                {
                    spotAmbience.InitializeSpotAmbience(this);
                }    
            }

            initializationSuccesfull = true;
        }

        private void AudioTriggerArea_OnTriggerAreaEvent(object sender, AudioTriggerAreaEventArgs e)
        {
            if (!initializationSuccesfull)
                return;

            if (e.triggerEventType == AudioTriggerAreaEventArgs.TriggerEventType.TriggerEnter)
            {
                insideCounter++;

                if (insideCounter == 1)
                {
                    if (!ambienceStarted)
                    {
                        StartAmbience();
                    }

                    InsideStatus = 1;
                    
                    if (baseAmbienceInstance.isValid())
                    {
                        baseAmbienceInstance.setVolume(InsideStatus);
                        UpdateInsideStatusForSpots();
                    }
                }
            }
            else
            {
                insideCounter--;

                if (insideCounter == 0)
                {
                    InsideStatus = 0;

                    if (baseAmbienceInstance.isValid())
                    {
                        baseAmbienceInstance.setVolume(InsideStatus);
                        UpdateInsideStatusForSpots();
                    }

                    if (enteredSpotAmbienceAreas.Count < 1) 
                    {
                        StopAmbience(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    }
                }
            }
        }

        private void StartAmbience()
        {
            if (ambienceStarted)
                return;

            baseAmbienceInstance = FMODUnity.RuntimeManager.CreateInstance(baseAmbience);

            if (baseAmbienceInstance.isValid())
            {
                baseAmbienceInstance.setVolume(InsideStatus);
                baseAmbienceInstance.start();
            }

            ambienceStarted = true;
        }

        private void UpdateInsideStatusForSpots()
        {
            foreach (var spotAmbience in enteredSpotAmbienceAreas)
            {
                if (spotAmbience != null)
                {
                    spotAmbience.UpdateInsideStatus(1 - InsideStatus);
                }
            }
        }

        private void StopAmbience(FMOD.Studio.STOP_MODE stopMode)
        {
            if (!ambienceStarted)
                return;

            if (baseAmbienceInstance.isValid())
            {
                baseAmbienceInstance.stop(stopMode);
                baseAmbienceInstance.release();
            }

            foreach (var spotAmbience in enteredSpotAmbienceAreas)
            {
                if (spotAmbience != null)
                {
                    spotAmbience.StopSpotAmbience();
                }
            }

            ambienceStarted = false;
        }

        public void ReportEnteredSpotAmbienceArea (OpeningSpotAmbience spotAmbience)
        {
            if (!enteredSpotAmbienceAreas.Contains(spotAmbience))
            {
                enteredSpotAmbienceAreas.Add(spotAmbience);

                if (!ambienceStarted)
                {
                    StartAmbience();
                }

                spotAmbience.StartSpotAmbience(1 - InsideStatus);
            }
        }

        public void ReportExitedSpotAmbienceArea (OpeningSpotAmbience spotAmbience)
        {
            if (enteredSpotAmbienceAreas.Contains(spotAmbience))
            {
                int index = enteredSpotAmbienceAreas.IndexOf(spotAmbience);
                enteredSpotAmbienceAreas.RemoveAt(index);

                if (enteredSpotAmbienceAreas.Count < 1 && InsideStatus == 0)
                {
                    StopAmbience(FMOD.Studio.STOP_MODE.IMMEDIATE);
                }
            }
        }

        void OnDestroy()
        {
            StopAmbience(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            foreach (var audioTriggerArea in validAudioTriggerAreas)
            {
                audioTriggerArea.OnTriggerAreaEvent -= AudioTriggerArea_OnTriggerAreaEvent;
            }       
        }

        void Reset()
        {
            var copyPosition = transform.position;
            copyPosition.x = 0.0f;
            copyPosition.y = 0.0f;
            copyPosition.z = 0.0f;
            transform.position = copyPosition;

            var copyScale = transform.localScale;
            copyScale.x = 1.0f;
            copyScale.y = 1.0f;
            copyScale.z = 1.0f;
            transform.localScale = copyScale;

            var copyRotation = transform.rotation;
            copyRotation.x = 0.0f;
            copyRotation.y = 0.0f;
            copyRotation.z = 0.0f;
            copyRotation.w = 0.0f;
            transform.rotation = copyRotation;
        }
    }   
}