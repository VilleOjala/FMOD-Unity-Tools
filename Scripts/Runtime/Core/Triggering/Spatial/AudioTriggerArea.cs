// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using UnityEngine;

namespace AudioTools
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Audio Tools/Core/Audio Trigger Area")]
    public class AudioTriggerArea : MonoBehaviour
    {
        [Tooltip("Give an optional name for the Audio Trigger Area ")]
        public string triggerAreaName = "";

        private Collider[] colliders;

        public event EventHandler<AudioTriggerAreaEventArgs> OnTriggerAreaEvent;

        [Space(5)]
        public RequiredTags requireTag = RequiredTags.Player;
        [HideInInspector]
        public string customRequiredTag;

        private int singletonTriggerCounter = 0;

        void Awake()
        {
            colliders = gameObject.GetComponentsInChildren<Collider>();

            // Always disable debug trigger meshes when going to the playmode to avoid accidentally leaving them on. 
            // <- Doing this without references is a bit ugly / bug prone, though.
            // Colors can be manually turned on again at runtime by using the 'Toggle Debug Colors On/Off' inspector button.

            var meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = false;
            }
        }

        void Reset()
        {         
            var rigidbody = gameObject.GetComponent<Rigidbody>();

            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            // Check if a layer with the name "AudioToolsGeneral" has been created.
            // If found, automatically assign this layer to the Audio Trigger Area game object.
            int layerIndex = LayerMask.NameToLayer("AudioToolsGeneral");

            if (layerIndex > -1)
            {
                gameObject.layer = layerIndex;
            }

            var copyPosition = transform.position;
            copyPosition.x = 0.0f;
            copyPosition.y = 0.0f;
            copyPosition.z = 0.0f;
            transform.position = copyPosition;

            var copyScale = transform.localScale;
            copyScale.x = 1.0f;
            copyScale.y = 1.0f;
            copyScale.z = 1.0f;
            transform.localScale = copyScale;

            var copyRotation = transform.rotation;
            copyRotation.x = 0.0f;
            copyRotation.y = 0.0f;
            copyRotation.z = 0.0f;
            copyRotation.w = 0.0f;
            transform.rotation = copyRotation;
        }

        void OnTriggerEnter(Collider other)
        {
            AudioActorTag audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

            switch (requireTag)
            {
                case RequiredTags.Player:
                    {
                        if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
                        {
                            if (singletonTriggerCounter == 0)
                            {
                                singletonTriggerCounter++;
                                ExecuteOnEnter();
                            }
                            else
                            {
                                singletonTriggerCounter++;
                            }
                        }
                    }
                    break;
                case RequiredTags.NonPlayer:
                    {
                        if (audioActorTag != null && audioActorTag.triggererType == TriggererType.NonPlayer)
                        {
                            ExecuteOnEnter();
                        }
                    }
                    break;
                case RequiredTags.AnyTagged:
                    {
                        if (audioActorTag != null)
                        {
                            ExecuteOnEnter();
                        }
                    }
                    break;
                case RequiredTags.Custom:
                    {
                        if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Custom &&
                            !string.IsNullOrEmpty(customRequiredTag) &&
                            !string.IsNullOrEmpty(audioActorTag.customTriggererTag) &&
                            customRequiredTag == audioActorTag.customTriggererTag)
                        {
                            if (singletonTriggerCounter == 0)
                            {
                                singletonTriggerCounter++;
                                ExecuteOnEnter();
                            }
                            else
                            {
                                singletonTriggerCounter++;
                            }
                        }
                    }
                    break;
                case RequiredTags.None:
                    { ExecuteOnEnter(); }
                    break;                  
                default:
                    break;
            }
        }

        void OnTriggerExit(Collider other)
        {
            var audioActorTag = other.gameObject.GetComponent<AudioActorTag>();

            switch (requireTag)
            {
                case RequiredTags.Player:
                    {
                        if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Player)
                        {
                            singletonTriggerCounter--;

                            if (singletonTriggerCounter == 0)
                            {
                                ExecuteOnExit();
                            }
                        }
                    }
                    break;
                case RequiredTags.NonPlayer:
                    {
                        if (audioActorTag != null && audioActorTag.triggererType == TriggererType.NonPlayer)
                        {
                            ExecuteOnExit();
                        }
                    }
                    break;
                case RequiredTags.AnyTagged:
                    {
                        if (audioActorTag != null)
                        {
                            ExecuteOnExit();
                        }
                    }
                    break;
                case RequiredTags.Custom:
                    {
                        if (audioActorTag != null && audioActorTag.triggererType == TriggererType.Custom &&
                            !string.IsNullOrEmpty(customRequiredTag) &&
                            !string.IsNullOrEmpty(audioActorTag.customTriggererTag) &&
                            customRequiredTag == audioActorTag.customTriggererTag)
                        {
                            singletonTriggerCounter--;

                            if (singletonTriggerCounter == 0)
                            {
                                ExecuteOnExit();
                            }
                        }
                    }
                    break;
                case RequiredTags.None:
                    { ExecuteOnExit(); }
                    break;
                default:
                    break;
            }
        }

        private void ExecuteOnEnter()
        {
            AudioTriggerAreaEventArgs eventArgs = new AudioTriggerAreaEventArgs();
            eventArgs.triggerEventType = AudioTriggerAreaEventArgs.TriggerEventType.TriggerEnter;
            OnTriggerAreaEvent?.Invoke(this, eventArgs);
        }

        private void ExecuteOnExit()
        {
            AudioTriggerAreaEventArgs eventArgs = new AudioTriggerAreaEventArgs();
            eventArgs.triggerEventType = AudioTriggerAreaEventArgs.TriggerEventType.TriggerExit;
            OnTriggerAreaEvent?.Invoke(this, eventArgs);
        }

        public Collider[] GetColliders()
        {
            if (colliders == null || colliders.Length < 1)
            {
                colliders = gameObject.GetComponentsInChildren<Collider>();
                return colliders;
            }
            else
            {
                return colliders;
            }
        }

        void OnValidate()
        {
            if (!string.IsNullOrEmpty(triggerAreaName))
                gameObject.name = triggerAreaName;
            else
                gameObject.name = "AudioTriggerArea";
        }
    }    
}