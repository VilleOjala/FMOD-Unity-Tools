// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Core/Audio Surface Tag")]
    public class AudioSurfaceTag : MonoBehaviour
    {
        public SurfaceType surfaceType = SurfaceType.Concrete;
        public SurfaceLayerType surfaceLayerType = SurfaceLayerType.None;

        public void ChangeSurfaceType(SurfaceType newSurfaceType)
        {
            surfaceType = newSurfaceType;
        }

        public void ChangeSurfaceLayerType(SurfaceLayerType newSurfaceLayerType)
        {
            surfaceLayerType = newSurfaceLayerType;
        }
    }
}