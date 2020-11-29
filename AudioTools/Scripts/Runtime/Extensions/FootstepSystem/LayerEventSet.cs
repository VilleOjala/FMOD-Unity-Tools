// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [CreateAssetMenu(fileName = "NewLayerEventSet", menuName = "Audio Tools/Layer Event Set", order = 4)]
    public class LayerEventSet : ScriptableObject
    {
        public SurfaceLayerType layerType = SurfaceLayerType.None;
        public MovementType movementType = MovementType.Walk;

        public LayerData[] layerData = new LayerData[0];

        [System.Serializable]
        public class LayerData
        {
            public string layerName;
            [FMODUnity.EventRef]
            public string[] eventOptions;
        }

        public Dictionary<string, string[]> GetValidData()
        {
            Dictionary<string, string[]> layerEventSetData = new Dictionary<string, string[]>();

            for (int i = 0; i < layerData.Length; i++)
            {
                LayerData data = layerData[i];

                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.layerName))
                    {
                        if (data.eventOptions != null && data.eventOptions.Length > 0)
                        {
                            var tempList = new List<string>();

                            for (int j = 0; j < data.eventOptions.Length; j++)
                            {
                                string eventOption = data.eventOptions[j];

                                if (!string.IsNullOrEmpty(eventOption))
                                {
                                    tempList.Add(eventOption);
                                }
                            }

                            if (tempList.Count > 0)
                            {
                                layerEventSetData.Add(data.layerName, tempList.ToArray());
                            }
                        }
                    }
                }
            }

            return layerEventSetData;
        }
    }
}