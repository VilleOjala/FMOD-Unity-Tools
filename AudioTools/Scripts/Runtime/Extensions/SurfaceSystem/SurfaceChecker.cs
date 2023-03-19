// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Surface System/Surface Checker")]
    public class SurfaceChecker : MonoBehaviour
    {
        public static SurfaceChecker Instance { get; private set; }
        public TerrainSurfaceData terrainSurfaceData;
        private Dictionary<TerrainLayer, SurfaceType> layerToSurfaceData = new Dictionary<TerrainLayer, SurfaceType>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                if (terrainSurfaceData == null)
                {
                    Debug.LogError("TerrainSurfaceData is null for the SurfaceChecker singleton " + gameObject.name);
                    return;
                }

                foreach (var item in terrainSurfaceData.Items)
                {
                    if (item == null || item.terrainLayer == null)
                        continue;
                    
                    if (!layerToSurfaceData.ContainsKey(item.terrainLayer))
                    {
                        layerToSurfaceData.Add(item.terrainLayer, item.surfaceType);
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool TryGetSurfaceType(Vector3 rayOrigin, Vector3 rayDirection, LayerMask layerMask, float rayLength, WaterDepthThresholds depthThreholds, out SurfaceInfo surfaceInfo)
        {
            surfaceInfo.surfaceType = SurfaceType.UNSET;
            surfaceInfo.waterDepth = WaterDepth.None;
            surfaceInfo.position = default;

            float distanceToClosestWaterTag = float.MaxValue;
            bool waterTagFound = false;
            Vector3 closestTaggedWaterPoint = default;

            float distanceToClosestGroundTag = float.MaxValue;
            bool groundTagFound = false;
            Vector3 closestTaggedGroundPoint = default;
            SurfaceType surfaceTypeFromGroundTag = SurfaceType.UNSET;

            float distanceToClosestTerrain = float.MaxValue;
            bool terrainFound = false;
            Vector3 closestTerrainPoint = default;
            SurfaceType surfaceTypeFromTerrain = SurfaceType.UNSET;

            var hits = Physics.RaycastAll(rayOrigin, rayDirection, rayLength, layerMask, QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                float distance = Vector3.Distance(rayOrigin, hit.point);

                if (hit.collider is TerrainCollider && hit.collider.gameObject.TryGetComponent(out Terrain terrain))
                {
                    terrainFound = true;

                    if (distance < distanceToClosestTerrain)
                    {
                        var weights = GetTerrainLayerRelativeWeights(terrain, hit.point);
                        TryGetDominantSurfaceTypeFromTerrainLayerWeights(in weights, out surfaceTypeFromTerrain);
                        distanceToClosestTerrain = distance;
                        closestTerrainPoint = hit.point;
                    }

                    continue;
                }

                var surfaceTag = hit.collider.GetComponentInParent<SurfaceTag>();

                if (surfaceTag == null)
                    continue;

                if (surfaceTag.surfaceType == SurfaceType.Water)
                {
                    waterTagFound = true;

                    if (distance < distanceToClosestWaterTag)
                    {
                        distanceToClosestWaterTag = distance;
                        closestTaggedWaterPoint = hit.point;
                    }
                }
                else
                {
                    groundTagFound = true;

                    if (distance < distanceToClosestGroundTag)
                    {
                        surfaceTypeFromGroundTag = surfaceTag.surfaceType;
                        distanceToClosestGroundTag = distance;
                        closestTaggedGroundPoint = hit.point;
                    }
                }
            }

            if (!groundTagFound && !waterTagFound && !terrainFound)
                return false;

            if (waterTagFound && distanceToClosestWaterTag < distanceToClosestTerrain && distanceToClosestWaterTag < distanceToClosestGroundTag)
            {
                surfaceInfo.surfaceType = SurfaceType.Water;
                surfaceInfo.position = closestTaggedWaterPoint;
                float waterDepth;

                if (distanceToClosestGroundTag <= distanceToClosestTerrain)
                {
                    waterDepth = Vector3.Distance(closestTaggedWaterPoint, closestTaggedGroundPoint);
                }
                else
                {
                    waterDepth = Vector3.Distance(closestTaggedWaterPoint, closestTerrainPoint);
                }

                if (waterDepth >= depthThreholds.deepDepthThreshold)
                {
                    surfaceInfo.waterDepth = WaterDepth.Deep;
                }
                else if (waterDepth >= depthThreholds.mediumDepthThreshold)
                {
                    surfaceInfo.waterDepth = WaterDepth.Medium;
                }
                else
                {
                    surfaceInfo.waterDepth = WaterDepth.Shallow;
                }
            }
            else if (terrainFound && !groundTagFound)
            {
                surfaceInfo.surfaceType = surfaceTypeFromTerrain;
                surfaceInfo.position = closestTerrainPoint;
            }
            else if (!terrainFound && groundTagFound)
            {
                surfaceInfo.surfaceType = surfaceTypeFromGroundTag;
                surfaceInfo.position = closestTaggedGroundPoint;
            }
            else if (terrainFound && groundTagFound)
            {
                if (distanceToClosestGroundTag <= distanceToClosestTerrain)
                {
                    surfaceInfo.surfaceType = surfaceTypeFromGroundTag;
                    surfaceInfo.position = closestTaggedGroundPoint;
                }
                else
                {
                    surfaceInfo.surfaceType = surfaceTypeFromTerrain;
                    surfaceInfo.position = closestTerrainPoint;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private Dictionary<TerrainLayer, float> GetTerrainLayerRelativeWeights(Terrain terrain, Vector3 worldPosition)
        {
            var data = new Dictionary<TerrainLayer, float>();
            var terrainData = terrain.terrainData;
            Vector3 splatPosition = new Vector3();
            Vector3 terrainPosition = terrain.transform.position;

            splatPosition.x = ((worldPosition.x - terrainPosition.x) / terrainData.size.x) * terrainData.alphamapWidth;
            splatPosition.z = ((worldPosition.z - terrainPosition.z) / terrainData.size.z) * terrainData.alphamapHeight;

            float[,,] splatMap = terrainData.GetAlphamaps((int)splatPosition.x, (int)splatPosition.z, 1, 1);

            float weightsSum = 0;

            for (int i = 0; i < splatMap.Length; i++)
            {
                weightsSum += splatMap[0, 0, i];
            }

            if (weightsSum > 0)
            {
                for (int i = 0; i < splatMap.Length; i++)
                {
                    TerrainLayer layer = terrainData.terrainLayers[i];
                    float weight = splatMap[0, 0, i];
                    float relativeWeight = weight / weightsSum;
                    data.Add(layer, relativeWeight);
                }
            }

            return data;
        }

        private bool TryGetDominantSurfaceTypeFromTerrainLayerWeights(in Dictionary<TerrainLayer, float> terrainLayerWeights, out SurfaceType surfaceType)
        {
            var combinedSurfaceTypeWeights = new Dictionary<SurfaceType, float>();
            bool foundValidSurfaceTypeFromTerrainTextures = false;
            surfaceType = SurfaceType.UNSET;

            foreach (var item in terrainLayerWeights)
            {
                TerrainLayer terrainLayer = item.Key;
                float weight = item.Value;

                if (layerToSurfaceData.ContainsKey(terrainLayer))
                {
                    SurfaceType terrainLayerSurfaceType = layerToSurfaceData[terrainLayer];

                    if (combinedSurfaceTypeWeights.ContainsKey(terrainLayerSurfaceType))
                    {
                        float combinedWeight = combinedSurfaceTypeWeights[terrainLayerSurfaceType];
                        combinedWeight += weight;
                        combinedSurfaceTypeWeights[terrainLayerSurfaceType] = combinedWeight;
                    }
                    else
                    {
                        combinedSurfaceTypeWeights.Add(terrainLayerSurfaceType, weight);
                    }

                    foundValidSurfaceTypeFromTerrainTextures = true;
                }
            }

            if (foundValidSurfaceTypeFromTerrainTextures)
            {
                KeyValuePair<SurfaceType, float> highestWeightSurface = default;
                bool firstEntryHandled = false;

                foreach (var item in combinedSurfaceTypeWeights)
                {
                    if (!firstEntryHandled)
                    {
                        highestWeightSurface = item;
                        firstEntryHandled = true;
                    }
                    else if (item.Value > highestWeightSurface.Value)
                    {
                        highestWeightSurface = item;
                    }
                }

                surfaceType = highestWeightSurface.Key;
                return true;
            }

            return false;
        }
    }
}