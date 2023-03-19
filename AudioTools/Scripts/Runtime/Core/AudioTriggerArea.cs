// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace FMODUnityTools
{
    public enum TriggerEventType
    {
        TriggerEnter,
        TriggerExit
    }

    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("FMOD Unity Tools/Core/Audio Trigger Area")]
    public class AudioTriggerArea : MonoBehaviour
    {
        public enum TriggererType
        {
            None,
            Listener, 
            LayerMask
        }

        private List<Collider> colliders = new List<Collider>();

        [SerializeField]
        private TriggererType triggererType = TriggererType.Listener;
        public TriggererType Triggerer { get => triggererType; } 

        [HideInInspector]
        public LayerMask layerMask;
        private int triggerCounter;
        private bool listenerInside;
        public Action<TriggerEventType> Triggered;

        void Awake()
        {
            /* Always disable debug trigger meshes when going to the playmode to avoid accidentally leaving them on. 
             * Colors can be manually turned on again at runtime by using the 'Toggle Debug Colors On/Off' Inspector button. */
            var meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = false;
            }

            colliders = GetTriggerColliders();
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (triggererType != TriggererType.LayerMask)
                return;

            if (!HelperMethods.GetIfLayerMaskContainsLayer(other.gameObject.layer, layerMask))
                return;

            if (triggerCounter == 0)
            {
                ExecuteOnEnter();
            }

            triggerCounter++;
        }

        void OnTriggerExit(Collider other)
        {
            if (triggererType != TriggererType.LayerMask)
                return;

            if (!HelperMethods.GetIfLayerMaskContainsLayer(other.gameObject.layer, layerMask))
                return;

            if (triggerCounter == 1)
            {
                ExecuteOnExit();
            }

            triggerCounter--;
        }

        private void Update()
        {
            if (triggererType != TriggererType.Listener)
                return;

            bool foundListener = HelperMethods.TryGetListenerPosition(out Vector3 listenerPosition);

            if (!foundListener && listenerInside)
            {
                ExecuteOnExit();
                listenerInside = false;
                return;
            }

            bool isInside = false;

            foreach (var collider in colliders)
            {
                if (collider != null && HelperMethods.CheckIfInsideCollider(listenerPosition, collider))
                {
                    isInside = true;
                    break;
                }
            }

            if (isInside && !listenerInside)
            {
                ExecuteOnEnter();
            }
            else if (!isInside && listenerInside)
            {
                ExecuteOnExit();
            }

            listenerInside = isInside;
        }

        private void ExecuteOnEnter()
        {
            Triggered?.Invoke(TriggerEventType.TriggerEnter);
        }

        private void ExecuteOnExit()
        {
            Triggered?.Invoke(TriggerEventType.TriggerExit);
        }

        public List<Collider> GetTriggerColliders()
        {
            var allColliders = GetComponentsInChildren<Collider>();
            var triggerColliders = allColliders.Where(x => x.isTrigger == true).ToList();
            return triggerColliders;
        }
    }    
}