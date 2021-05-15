// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Core/Audio Animation Event Mediator")]
    public class AudioAnimationEventMediator : MonoBehaviour
    {
        [Tooltip("All Audio Objects triggered from the animation clip events of the associated Animator will be set to follow this transform.")]
        public Transform followTransformOverride;

        void Reset()
        {
            #if UNITY_EDITOR
            var animator = gameObject.GetComponent<Animator>();

            if (animator == null)
            {

                EditorUtility.DisplayDialog("", "Audio Animation Event Mediator can only be added on a game object containing an Animator.", "Ok");                
                DestroyImmediate(this);
            }
            #endif
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