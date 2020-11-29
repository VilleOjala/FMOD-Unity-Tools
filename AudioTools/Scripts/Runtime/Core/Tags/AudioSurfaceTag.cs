// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

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