// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEditor;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Core/Audio Animation Event Mediator")]
    public class AudioAnimationEventMediator : MonoBehaviour
    {
        [Tooltip("All Audio Objects triggered from the animation clip events of the associated Animator will be set to follow this transform.")]
        public Transform followTransformOverride = null;

        void Reset()
        {
            var animator = gameObject.GetComponent<Animator>();

            if (animator == null)
            {
                EditorUtility.DisplayDialog("", "Audio Animation Event Mediator can only be added on a game object containing an Animator.", "Ok");
                DestroyImmediate(this);
            }
        }

        // Use this method name when assigning animation events to keyframes 
        // and pass an Audio Object Tag as an argument to target the desired Audio Objects. 
        public void OnAnimationEvent(Object audioObjectTag)
        {
            if (audioObjectTag is AudioObjectTag)
            {
                if (followTransformOverride != null)
                {
                    AudioObjectMessenger.StartAudioObjects((AudioObjectTag)audioObjectTag, followTransformOverride);
                }
                else
                {
                    AudioObjectMessenger.StartAudioObjects((AudioObjectTag)audioObjectTag);
                }
            }
        }
    }
}