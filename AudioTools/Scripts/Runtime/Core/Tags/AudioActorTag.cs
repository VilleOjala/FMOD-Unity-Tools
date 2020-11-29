// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEditor;
using UnityEngine;

namespace AudioTools
{
    [RequireComponent(typeof(SphereCollider))]
    [AddComponentMenu("Audio Tools/Core/Audio Actor Tag")]
    public class AudioActorTag : MonoBehaviour 
    {
        public Transform followTransform;
        public SphereCollider triggerCollider;
        public TriggererType triggererType = TriggererType.Nothing;

        [HideInInspector]
        public string customTriggererTag;

        void OnValidate()
        {       
            // Give an editor-time warning if other 'Player' Audio Actor Tags are found in the scene. 
            if (triggererType == TriggererType.Player)
            {
                AudioActorTag[] tags = FindObjectsOfType<AudioActorTag>();

                if (tags != null)
                {
                    for (int i = 0; i < tags.Length; i++)
                    {
                        AudioActorTag tag = tags[i];

                        if (tag.triggererType == TriggererType.Player && tag != this)
                        {
                            EditorUtility.DisplayDialog("Warning", "The scene has multiple Audio Triggerer Tag - components set as 'Player'. " +
                                                        "Ensure that the player position is only set once.", "Ok");
                            triggererType = TriggererType.Nothing;
                        }
                    }
                }
            }         
        }

        void Reset()
        {
            triggerCollider = GetComponent<SphereCollider>();

            if(triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = 0.01f;
            }
        }

        void Update()
        {
            if (followTransform != null)
            {
                gameObject.transform.position = followTransform.position;
                gameObject.transform.rotation = followTransform.rotation;
            }
        }
    }
}