// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using UnityEngine;

namespace FMODUnityTools
{
    // Different solid ground -type surfaces can be added / removed, but 'Water' should always be included for the SurfaceChecker logic to work.
    public enum SurfaceType
    {
        UNSET = 0,
        Dirt = 1,
        Rock = 2,
        Sand = 3,
        Wood = 4,
        Water = 5
    }

    public enum WaterDepth
    {
        None = 0,
        Shallow = 1,
        Medium = 2,
        Deep = 3
    }

    [Serializable]
    public struct WaterDepthThresholds
    {
        [Min(0)] public float mediumDepthThreshold;
        [Min(0)] public float deepDepthThreshold;
    }

    public struct SurfaceInfo
    {
        public SurfaceType surfaceType;
        public WaterDepth waterDepth;
        public Vector3 position;
    }
}