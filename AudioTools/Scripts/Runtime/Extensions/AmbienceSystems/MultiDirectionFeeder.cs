// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

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

        [EventRef]
        public string feederEvent;
        public bool startFeedingOnAwake = false;

        private EventDescription feederEventDescription;
        private EventInstance feederEventInstance;

        private List<MultiDirectionOutput> playingOutputInstances = new List<MultiDirectionOutput>();

        public string debugTotalAttenuation = "";

        void Awake()
        {
            if (!string.IsNullOrEmpty(feederEvent))
            {
                feederEventDescription =  RuntimeManager.GetEventDescription(feederEvent);
            }

            if (audioObjectTag != null)
            {
                AudioObjectMessenger.StartAudioObjectEvent += AudioObjectMessenger_StartAudioObjectEvent;
                AudioObjectMessenger.StopAudioObjectEvent += AudioObjectMessenger_StopAudioObjectEvent;
            }

            if (startFeedingOnAwake)
            {
                StartFeederEvent();
            }
        }

        private void AudioObjectMessenger_StartAudioObjectEvent(object sender, AudioObjectMessengerEventArgs e)
        {
            if (e.audioObjectTag == audioObjectTag)
            {
                StartFeederEvent();
            }
        }

        private void AudioObjectMessenger_StopAudioObjectEvent(object sender, AudioObjectMessengerEventArgs e)
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

            if (totalSpatializerAttenuations > 1)
            {
                foreach (var outputInstance in playingOutputInstances)
                {
                    if (outputInstance != null)
                    {
                        float weight = outputInstance.SpatializerAttenuation / totalSpatializerAttenuations;
                        outputInstance.SetVolume(weight);
                    }
                }
            }
            else
            {
                foreach (var outputInstance in playingOutputInstances)
                {
                    if (outputInstance != null)
                    {
                        outputInstance.SetVolume(1);
                    }
                }
            }

            #if UNITY_EDITOR
            debugTotalAttenuation = totalSpatializerAttenuations.ToString(); 
            #endif
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
            AudioObjectMessenger.StartAudioObjectEvent -= AudioObjectMessenger_StartAudioObjectEvent;
            AudioObjectMessenger.StopAudioObjectEvent -= AudioObjectMessenger_StopAudioObjectEvent;
        }
    }
}