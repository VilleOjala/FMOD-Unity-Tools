// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Spatial Audio System/Spatial Audio Obstruction Tag")]
    public class SpatialAudioObstructionTag : MonoBehaviour
    {
        [SerializeField]
        private bool tagActive = true;
        private List<Collider> colliders;
        private bool hasBeenDisabled = false;

        private void RetrieveAndReportColliders()
        {
            colliders = GetComponentsInChildren<Collider>().ToList();

            if (SpatialAudioManager.Instance != null)
            {
                foreach (var collider in colliders)
                {
                    SpatialAudioManager.Instance.AddObstructingCollider(collider);
                }
            }
        }

        private void UnreportColliders()
        {
            if (SpatialAudioManager.Instance != null)
            {
                foreach (var collider in colliders)
                {
                    if (collider == null)
                        continue;

                    SpatialAudioManager.Instance.RemoveObstructingCollider(collider);
                }
            }
        }

        private void Start()
        {
            if (tagActive)
            {
                RetrieveAndReportColliders(); 
            }              
        }

        private void OnEnable()
        {
            if (hasBeenDisabled && tagActive)
            {
                RetrieveAndReportColliders();
            }
        }

        private void OnDisable()
        {
            if (tagActive)
            {
                hasBeenDisabled = true;
                UnreportColliders();
            }
        }

        private void OnDestroy()
        {
            if (tagActive)
            {
                UnreportColliders();
            }
        }
    }
}