// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Spatial Audio System/Spatial Audio Obstruction Tag")]
    public class SpatialAudioObstructionTag : MonoBehaviour
    {
        public bool tagActive = true;
        public Collider[] colliders;

        private bool hasBeenDisabled = false;

        void Start()
        {
            if (tagActive && colliders != null && SpatialAudioManager.Instance != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        SpatialAudioManager.Instance.AddObstructingCollider(colliders[i]);
                }
            }              
        }

        void OnEnable()
        {
            if (hasBeenDisabled && tagActive && colliders != null && SpatialAudioManager.Instance != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        SpatialAudioManager.Instance.AddObstructingCollider(colliders[i]);
                }
            }
        }

        void OnDestroy()
        {
            if (tagActive && colliders != null && SpatialAudioManager.Instance != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        SpatialAudioManager.Instance.RemoveObstructingCollider(colliders[i]);
                }
            }
        }

        void OnDisable()
        {
            hasBeenDisabled = true;

            if (tagActive && colliders != null && SpatialAudioManager.Instance != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        SpatialAudioManager.Instance.RemoveObstructingCollider(colliders[i]);
                }
            }
        }

        void Reset()
        {
            var c = gameObject.GetComponents<Collider>();
            colliders = c;
        }
    }
}