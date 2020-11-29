// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Pause Menu System/Pause Menu Audio")]
    public class PauseMenuAudio : MonoBehaviour 
    {
        // Uses the singleton pattern.
        public static PauseMenuAudio instance { get; private set; }

        private bool gameIsPaused = false;

        [FMODUnity.EventRef]
        public string pauseMenuSnaphot;

        private FMOD.Studio.EventDescription eventDescription;
        private FMOD.Studio.EventInstance eventInstance;

        // Busses to pause when in pause menu.
        // Do not add return busses.
        [Tooltip("For example: bus:/Music")]
        public List<string> bussesToPause = new List<string>();

        private List<FMOD.Studio.Bus> _bussesToPause = new List<FMOD.Studio.Bus>();

        [Tooltip("Add an AHDSR modulator to the 'intensity' value of the pause menu snaphot and match its attack time with this value.")]
        [Range(0.0f, 1.0f)]
        public float waitBeforePausing = 0.35f;

        private bool coroutineRunning = false;
        private bool initializationSuccesfull = false;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (string.IsNullOrEmpty(pauseMenuSnaphot))
            {
                Debug.LogError("Pause menu snapshot is null or empty");
                return;
            }

            FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.getEvent(pauseMenuSnaphot, out eventDescription);

            if(result != FMOD.RESULT.OK)
            {
                Debug.LogError("Path for pause menu snapshot is not valid. Fmod error: " + result);
                return;
            }

            for (int i = 0; i < bussesToPause.Count; i++)
            {
                string busPath = bussesToPause[i];
                
                if(!string.IsNullOrEmpty(busPath))
                {
                    FMOD.Studio.Bus bus;
                    result = FMODUnity.RuntimeManager.StudioSystem.getBus(busPath, out bus);

                    if(result == FMOD.RESULT.OK)
                    {
                        _bussesToPause.Add(bus);
                    }
                    else
                    {
                        Debug.LogError("Bus path '" + busPath + "' is invalid. Fmod error: " + result);
                    }
                }
            }

            initializationSuccesfull = true;
        }

        public void TogglePauseState(bool paused)
        {
            if (!initializationSuccesfull) { return; }

            if (paused)
            {
                gameIsPaused = true;
                SetAudioPauseStatus();
            }
            else
            {
                gameIsPaused = false;
                SetAudioPauseStatus();
            }
        }

        private void SetAudioPauseStatus()
        {
            if (gameIsPaused)
            {
                if (!coroutineRunning)
                {
                    coroutineRunning = true;
                    StartCoroutine(ExecuteAfterTime());
                    ActivateSnapshot(); 
                }
            }
            else
            {
                StopAllCoroutines();
                coroutineRunning = false;
                UnpauseBusses();
                DeactivateSnapshot();
            }
        }

        private IEnumerator ExecuteAfterTime()
        {
            yield return new WaitForSecondsRealtime(waitBeforePausing);
            coroutineRunning = false;

            if (gameIsPaused)
            {
                PauseBusses();
            }
        }

        private void ActivateSnapshot()
        {
            FMOD.RESULT result = eventDescription.createInstance(out eventInstance);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError("Activation of pause menu snapshot failed. Fmod error: " + result);
                return;
            }

            eventInstance.start();
        }

        private void DeactivateSnapshot()
        {
            if (eventInstance.isValid())
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                eventInstance.release();
            }
        }

        private void PauseBusses()
        {
            for (int i = 0; i < _bussesToPause.Count; i++)
            {
                FMOD.Studio.Bus bus = _bussesToPause[i];

                bus.setPaused(true);
            }
        }

        private void UnpauseBusses()
        {
            for (int i = 0; i < _bussesToPause.Count; i++)
            {
                FMOD.Studio.Bus bus = _bussesToPause[i];

                bus.setPaused(false);
            }
        }

        void OnDisable()
        {
            DeactivateSnapshot();
            StopAllCoroutines();

            for (int i = 0; i < _bussesToPause.Count; i++)
            {
                FMOD.Studio.Bus bus = _bussesToPause[i];

                bus.setPaused(false);
            }
        }

        void OnDestroy()
        {
            DeactivateSnapshot();
            StopAllCoroutines();

            for (int i = 0; i < _bussesToPause.Count; i++)
            {
                FMOD.Studio.Bus bus = _bussesToPause[i];

                bus.setPaused(false);
            }
        }

        // Game's pause menu system should call this method before loading the main menu scene & unloading the current scene.
        public void ExitToMainMenu()
        {
            if(gameIsPaused)
            {
                DeactivateSnapshot();
                StopAllCoroutines();

                for (int i = 0; i < _bussesToPause.Count; i++)
                {
                    FMOD.Studio.Bus bus = _bussesToPause[i];

                    bus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE); 
                    bus.setPaused(false);
                }

                gameIsPaused = false;
                coroutineRunning = false;
            }
        }
    }
}    