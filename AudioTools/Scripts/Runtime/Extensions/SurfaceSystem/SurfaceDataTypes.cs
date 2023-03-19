// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using UnityEngine;

namespace FMODUnityTools
{
    /* Surfaces can be added / removed as per project's needs, except for 'Water', which should always be included. 
     * This is because the logic in SurfaceChecker class for figuring out the water depth is only run if the surface type
     * is first determined to be 'Water'. The numbering of parameter options inside FMOD Studio authoring tool should match 
     * those in SurfaceType and WaterDepth enums, as numbers (instead of string labels) are used for parameter setting in this toolset. */

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