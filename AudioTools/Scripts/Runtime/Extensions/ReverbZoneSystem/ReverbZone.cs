// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Reverb Zone System/Reverb Zone")]
    public class ReverbZone : MonoBehaviour
    {
        public string snapshotPath = "snapshot:/";
        public List<AudioTriggerArea> audioTriggerAreas = new List<AudioTriggerArea>();
        private List<AudioTriggerArea> validAudioTriggerAreas = new List<AudioTriggerArea>();

        FMOD.Studio.EventInstance snapshotInstance;

        private bool initializationSuccesfull = false;
        private int insideCount = 0;

        void Start()
        {
            if (audioTriggerAreas != null && audioTriggerAreas.Count > 0)
            {
                foreach (var audioTriggerArea in audioTriggerAreas)
                {
                    if (audioTriggerArea != null)
                    {
                        validAudioTriggerAreas.Add(audioTriggerArea);
                        audioTriggerArea.OnTriggerAreaEvent += AudioTriggerArea_OnTriggerAreaEvent;
                    }
                }
            }

            if (validAudioTriggerAreas.Count < 1)
                return;

            if (string.IsNullOrEmpty(snapshotPath))
                return;

            initializationSuccesfull = true;
        }

        private void AudioTriggerArea_OnTriggerAreaEvent(object sender, AudioTriggerAreaEventArgs e)
        {
            if (e.triggerEventType == AudioTriggerAreaEventArgs.TriggerEventType.TriggerEnter)
            {
                insideCount++;

                if (insideCount == 1 && initializationSuccesfull)
                {
                    snapshotInstance = FMODUnity.RuntimeManager.CreateInstance(snapshotPath);

                    if (snapshotInstance.isValid())
                    {
                        snapshotInstance.start();
                    }
                    else
                    {
                        Debug.LogError("Snapshot event path is invalid for Reverb Zone " + gameObject.name + ".");
                    }
                }
            }
            else
            {
                insideCount--;

                if (insideCount == 0 && initializationSuccesfull)
                {
                    if (snapshotInstance.isValid())
                    {
                        snapshotInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        snapshotInstance.release();                        
                    }
                }
            }
        }

        void OnValidate()
        {         
            if (string.IsNullOrEmpty(snapshotPath))
            {
                snapshotPath = "snapshot:/";
            }          
        }

        void Reset()
        {
            gameObject.name = "ReverbZone";    
        }

        void OnDestroy()
        {
            if (snapshotInstance.isValid())
            {
                snapshotInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                snapshotInstance.release();
            }

            foreach (var audioTriggerArea in validAudioTriggerAreas)
            {
                audioTriggerArea.OnTriggerAreaEvent -= AudioTriggerArea_OnTriggerAreaEvent;
            }
        }
    }
}