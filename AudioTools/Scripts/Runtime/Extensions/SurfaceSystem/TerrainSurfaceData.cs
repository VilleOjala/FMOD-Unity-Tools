// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;

namespace FMODUnityTools
{
    [CreateAssetMenu(fileName = "TerrainSurfaceData", menuName = "FMOD Unity Tools/Terrain Surface Data")]
    public class TerrainSurfaceData : ScriptableObject
    {
        [System.Serializable]
        public class TerrainSurfaceDataItem
        {
            public TerrainLayer terrainLayer;
            public SurfaceType surfaceType;
        }

        [SerializeField]
        private List<TerrainSurfaceDataItem> items = new List<TerrainSurfaceDataItem>();
        public List<TerrainSurfaceDataItem> Items { get { return items; } }
    }
}
