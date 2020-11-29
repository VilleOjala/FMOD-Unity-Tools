// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [CreateAssetMenu(fileName = "NewFootstepEventSet", menuName = "Audio Tools/Footstep Event Set", order = 2)]
    public class FootstepEventSet : ScriptableObject
    {
        public SurfaceType surfaceType = SurfaceType.Concrete;
        public ShoeOrFeetType shoeOrFeetType = ShoeOrFeetType.Barefeet;
        public MovementType movementType = MovementType.Walk;

        public Combination[] combinationData = new Combination[0];
 
        [System.Serializable]
        public class Combination
        {
            public string combination;
            [FMODUnity.EventRef]
            public string[] eventOptions;
        }

        public Dictionary<string, string[]> GetValidData()
        {
            Dictionary<string, string[]> footstepEventSetData = new Dictionary<string, string[]>();

            for (int i = 0; i < combinationData.Length; i++)
            {
                Combination comb = combinationData[i];

                if (comb != null)
                {
                    if (!string.IsNullOrEmpty(comb.combination))
                    {
                        if (comb.eventOptions != null && comb.eventOptions.Length > 0)
                        {
                            var tempList = new List<string>();

                            for (int j = 0; j < comb.eventOptions.Length; j++)
                            {
                                string eventOption = comb.eventOptions[j];

                                if (!string.IsNullOrEmpty(eventOption))
                                {
                                    tempList.Add(eventOption);
                                }
                            }

                            if (tempList.Count > 0)
                            {
                                footstepEventSetData.Add(comb.combination, tempList.ToArray());
                            }
                        }
                    }
                }                            
            }

            return footstepEventSetData;
        }
    }
}