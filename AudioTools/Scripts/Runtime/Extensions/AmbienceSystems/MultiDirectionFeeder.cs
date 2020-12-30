// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Ambience Systems/Multi Direction Feeder")]
    public class MultiDirectionFeeder : MonoBehaviour
    {
        public AudioObjectTag audioObjectTag;

        [FMODUnity.EventRef]
        public string feederEvent;
        public bool startFeedingOnAwake = false;

        private EventDescription feederEventDescription;
        private EventInstance feederEventInstance;

        private List<MultiDirectionOutput> playingOutputInstances = new List<MultiDirectionOutput>();

        void Awake()
        {
            if (!string.IsNullOrEmpty(feederEvent))
            {
                feederEventDescription =  RuntimeManager.GetEventDescription(feederEvent);
            }

            if (audioObjectTag != null)
            {
                AudioObjectMessenger.OnStartAudioObjectEvent += AudioObjectMessenger_OnStartAudioObjectEvent;
                AudioObjectMessenger.OnStopAudioObjectEvent += AudioObjectMessenger_OnStopAudioObjectEvent;
            }

            if (startFeedingOnAwake)
            {
                StartFeederEvent();
            }
        }

        private void AudioObjectMessenger_OnStartAudioObjectEvent(object sender, AudioObjectMessengerEventArgs e)
        {
            if (e.audioObjectTag == audioObjectTag)
            {
                StartFeederEvent();
            }
        }

        private void AudioObjectMessenger_OnStopAudioObjectEvent(object sender, AudioObjectMessengerEventArgs e)
        {
            if (e.audioObjectTag == audioObjectTag)
            {
                StopFeederEvent();
            }
        }

        void LateUpdate()
        {
            if (playingOutputInstances.Count == 1)
            {
                if (playingOutputInstances[0] != null)
                {
                    playingOutputInstances[0].SetVolume(1);
                }
            }
            else if (playingOutputInstances.Count > 1)
            {
                CalculateWeightedVolumes();
            }
        }

        public void ReportOutputStart(MultiDirectionOutput outputInstance)
        {
            if (!playingOutputInstances.Contains(outputInstance))
            {
                playingOutputInstances.Add(outputInstance);
            }
        }

        public void ReportOutputStop(MultiDirectionOutput outputInstance)
        {
            if (playingOutputInstances.Contains(outputInstance))
            {
                int index = playingOutputInstances.IndexOf(outputInstance);
                playingOutputInstances.RemoveAt(index);
            }
        }

        private void StartFeederEvent()
        {
            if (!feederEventInstance.isValid() && feederEventDescription.isValid())
            {
                feederEventDescription.createInstance(out feederEventInstance);
                feederEventInstance.start();
            }
        }

        private void StopFeederEvent()
        {
            if (feederEventInstance.isValid())
            {
                feederEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                feederEventInstance.release();
            }
        }

        private void CalculateWeightedVolumes()
        {
            float totalSpatializerAttenuations = 0;

            foreach (var outputInstance in playingOutputInstances)
            {
                if (outputInstance != null)
                {
                    totalSpatializerAttenuations += outputInstance.SpatializerAttenuation;
                }
            }

            foreach (var outputInstance in playingOutputInstances)
            {
                if (outputInstance != null)
                {
                    float weight = outputInstance.SpatializerAttenuation / totalSpatializerAttenuations;
                    outputInstance.SetVolume(weight);
                }
            }
        }

        public void SetParameter(string parameterName, float parameterValue)
        {
            if (feederEventInstance.isValid())
            {
                FMOD.RESULT result = feederEventInstance.setParameterByName(parameterName, parameterValue);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError("FMOD error: " + result);
                }      
            }
        }

        void OnDestroy() 
        {
            StopFeederEvent();
            AudioObjectMessenger.OnStartAudioObjectEvent -= AudioObjectMessenger_OnStartAudioObjectEvent;
            AudioObjectMessenger.OnStopAudioObjectEvent -= AudioObjectMessenger_OnStopAudioObjectEvent;
        }
    }
}