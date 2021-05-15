// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace AudioTools
{
    public class AudioAnimatorStateEventBehaviour : StateMachineBehaviour
    {
        public AnimatorStateEventType animatorStateEventType = AnimatorStateEventType.None;
        public TriggeringAction triggerAction = TriggeringAction.StartSound;
        public AudioObjectTag audioObjectTag;

        [Tooltip("Overrides the default position of the sound with the position of the game object that contains the animator. " +
                 "Only works with the 'Start Sound' -event type.")]
        public bool overrideWithAnimatorPosition = false;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animatorStateEventType == AnimatorStateEventType.OnStateEnter)
            {
                if (overrideWithAnimatorPosition)
                {
                    SendEventToAudioObjectControllers(animator.gameObject.transform);
                }
                else
                {
                    SendEventToAudioObjectControllers();
                }
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animatorStateEventType == AnimatorStateEventType.OnStateUpdate)
            {
                SendEventToAudioObjectControllers();
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animatorStateEventType == AnimatorStateEventType.OnStateExit)
            {
                SendEventToAudioObjectControllers();
            }
        }

        private void SendEventToAudioObjectControllers(Transform overrideTransform = null)
        {
            switch (triggerAction)
            {
                case TriggeringAction.StartSound:
                    {
                        if (audioObjectTag != null)
                        {
                            if (overrideTransform != null)
                            {
                                AudioObjectMessenger.StartAudioObjects(audioObjectTag, overrideTransform);
                            }
                            else
                            {
                                AudioObjectMessenger.StartAudioObjects(audioObjectTag);
                            }
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

        public enum AnimatorStateEventType
        {
            None,
            OnStateEnter,
            OnStateUpdate,
            OnStateExit 
        }
    }
}