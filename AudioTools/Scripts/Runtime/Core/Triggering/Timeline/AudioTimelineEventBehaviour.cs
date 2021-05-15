// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEngine.Playables;

namespace AudioTools
{
    public class AudioTimelineEventBehaviour : PlayableBehaviour
    {
        public AudioObjectTag audioObjectTag;
        public TriggeringAction triggeringAction;
        public Transform followTransform;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            switch (triggeringAction)
            {
                case TriggeringAction.StartSound:
                    {
                        if (audioObjectTag != null && followTransform != null)
                        {
                            AudioObjectMessenger.StartAudioObjects(audioObjectTag, followTransform);
                        }
                        else if (audioObjectTag != null)
                        {
                            AudioObjectMessenger.StartAudioObjects(audioObjectTag);
                        }
                    }
                    break;
                case TriggeringAction.StopSound:
                    {
                        if (audioObjectTag != null)
                        {
                            AudioObjectMessenger.StopAudioObjects(audioObjectTag);
                        }
                    }
                    break;
                case TriggeringAction.StopPersistentSound:
                    {
                        if (audioObjectTag != null)
                        {
                            AudioObjectMessenger.StopPersistentAudioObjects(audioObjectTag);
                        }
                    }
                    break;
                case TriggeringAction.StopAllPersistentSounds:
                    AudioObjectMessenger.StopAllPersistentAudioObjects();
                    break;
            }
        }
    }
}