// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Ambience System/Base Ambience Area")]
    public class BaseAmbienceArea : MonoBehaviour
    {
        [FMODUnity.EventRef]
        public string baseAmbience;
        private FMOD.Studio.EventInstance baseAmbienceInstance;

        [Range(0.0f, 10.0f)]
        public float transitionTime = 1.0f;

        public List<AudioTriggerArea> audioTriggerAreas = new List<AudioTriggerArea>();
        private List<AudioTriggerArea> validAudioTriggerAreas = new List<AudioTriggerArea>();
        public List<SpotAmbience> spotAmbiences = new List<SpotAmbience>();
        
        // 1 = Inside
        // 0 = Outside
        public float InsideStatus { get; private set; } = 0;
        private float cacheInsideStatus = 0;

        private int insideCounter = 0;
        private bool inProgressEnter = false;
        private bool inProgressExit = false;

        private bool initializationSuccesfull = false;

        void Start()
        {
            if (string.IsNullOrEmpty(baseAmbience))
            {
                Debug.LogError("FMOD event reference is null or empty for Base Ambience Area " + gameObject.name + ".");
                return;
            }

            baseAmbienceInstance = FMODUnity.RuntimeManager.CreateInstance(baseAmbience);

            if (!baseAmbienceInstance.isValid())
            {
                Debug.LogError("FMOD event reference is invalid for Base Ambience Area " + gameObject.name + ".");
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

                if (validAudioTriggerAreas.Count < 1)
                {
                    baseAmbienceInstance.release();
                    return;
                }
            }

            foreach (var spotAmbience in spotAmbiences)
            {
                if (spotAmbience != null)
                {
                    spotAmbience.InitializeSpotAmbience(this);
                }    
            }

            baseAmbienceInstance.setVolume(InsideStatus);
            baseAmbienceInstance.start();
            initializationSuccesfull = true;
        }

        private void AudioTriggerArea_OnTriggerAreaEvent(object sender, AudioTriggerAreaEventArgs e)
        {
            if (e.triggerEventType == AudioTriggerAreaEventArgs.TriggerEventType.TriggerEnter)
            {
                insideCounter++;

                if (insideCounter == 1)
                {
                    StartCoroutine(EnterArea(transitionTime));
                }
            }
            else
            {
                insideCounter--;

                if (insideCounter == 0)
                {
                    StartCoroutine(ExitArea(transitionTime));
                }
            }
        }

        void Update()
        {
            if (initializationSuccesfull && InsideStatus != cacheInsideStatus && baseAmbienceInstance.isValid())
            {
                cacheInsideStatus = InsideStatus;
                baseAmbienceInstance.setVolume(InsideStatus);          
            }
        }

        IEnumerator EnterArea(float duration)
        {
            float startValue = InsideStatus;
            float totalFade = 1 - startValue;
            duration *= (1 - startValue);

            inProgressEnter = true;
            inProgressExit = false;
            float timePassed = 0.0f;

            while (timePassed < duration && !inProgressExit)
            {
                timePassed += Time.deltaTime;
                float completion = timePassed / duration;
                completion = Mathf.Clamp01(completion);
                InsideStatus = startValue + (totalFade * completion);

                yield return null;
            }

            if (!inProgressExit)
            {
                InsideStatus = 1.0f;
            }

            inProgressEnter = false;
        }

        IEnumerator ExitArea(float duration)
        {
            float startValue = InsideStatus;
            float totalFade = startValue;
            duration *= startValue;

            inProgressExit = true;
            inProgressEnter = false;
            float timePassed = 0.0f;

            while (timePassed < duration && !inProgressEnter)
            {
                timePassed += Time.deltaTime;
                float completion = timePassed / duration;
                completion = Mathf.Clamp01(completion);
                InsideStatus = startValue - (totalFade * completion);

                yield return null;
            }

            if (!inProgressEnter)
            {
                InsideStatus = 0;
            }

            inProgressExit = false;
        }

        public bool BaseAmbienceIsValid()
        {
            if (baseAmbienceInstance.isValid())
                return true;
            else
                return false;           
        }

        public int GetBaseAmbiencePlaybackPosition()
        {
            int playbackPosition = 0;

            if (baseAmbienceInstance.isValid())
            {
                baseAmbienceInstance.getTimelinePosition(out playbackPosition);
            }

            return playbackPosition;   
        }

        void OnDestroy()
        {
            if (baseAmbienceInstance.isValid())
            {
                baseAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                baseAmbienceInstance.release();
            }

            foreach (var audioTriggerArea in validAudioTriggerAreas)
            {
                audioTriggerArea.OnTriggerAreaEvent -= AudioTriggerArea_OnTriggerAreaEvent;
            }       
        }

        void Reset()
        {
            gameObject.name = "BaseAmbienceArea";

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