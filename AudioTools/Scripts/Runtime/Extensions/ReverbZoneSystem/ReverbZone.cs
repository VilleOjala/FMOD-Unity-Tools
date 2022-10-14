// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Reverb Zone System/Reverb Zone")]
    public class ReverbZone : MonoBehaviour
    {
        public EventReference snaphotReference;
        private EventDescription snapshotDescription;
        private EventInstance snapshotInstance;
        public AudioTriggerArea audioTriggerArea;

        private void Awake()
        {
            if (audioTriggerArea != null)
            {
                audioTriggerArea.Triggered += TriggeredHandler;
            }

            HelperMethods.TryRetrieveDescriptionIfNotAlreadyValid(snaphotReference, ref snapshotDescription);
        }

        private void TriggeredHandler(TriggerEventType triggerEventType)
        {
            if (triggerEventType == TriggerEventType.TriggerEnter)
            {
                if (!snapshotDescription.isValid())
                    return;

                if (!snapshotInstance.isValid())
                {
                    snapshotDescription.createInstance(out snapshotInstance);
                    snapshotInstance.start();
                }
                else
                {
                    snapshotInstance.getPlaybackState(out PLAYBACK_STATE playbackState);

                    if (playbackState == PLAYBACK_STATE.STARTING || playbackState == PLAYBACK_STATE.PLAYING)
                        return;

                    if (playbackState == PLAYBACK_STATE.STOPPING || playbackState == PLAYBACK_STATE.STOPPED)
                    {
                        StopIfPlaying();
                    }
                }
            }
            else if (triggerEventType == TriggerEventType.TriggerExit)
            {
                StopIfPlaying();
            }
        }

        private void OnDisable()
        {
            StopIfPlaying();

            if (audioTriggerArea != null)
            { 
                audioTriggerArea.Triggered -= TriggeredHandler;
            }
        }

        private void StopIfPlaying()
        {
            if (snapshotInstance.isValid())
            {
                snapshotInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                snapshotInstance.release();
            }
        }
    }
}