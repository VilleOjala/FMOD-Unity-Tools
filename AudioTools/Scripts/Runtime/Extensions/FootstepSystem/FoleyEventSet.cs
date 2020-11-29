// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [CreateAssetMenu(fileName = "NewFoleyEventSet", menuName = "Audio Tools/Foley Event Set", order = 3)]
    public class FoleyEventSet : ScriptableObject
    {
        public FoleyType foleyType = FoleyType.Naked;
        public MovementType movementType = MovementType.Walk;

        public FoleyData[] foleyData = new FoleyData[0];

        [System.Serializable]
        public class FoleyData
        {
            public string foleyName;
            [FMODUnity.EventRef]
            public string[] eventOptions;
        }

        public Dictionary<string, string[]> GetValidData()
        {
            Dictionary<string, string[]> foleyEventSetData = new Dictionary<string, string[]>();

            for (int i = 0; i < foleyData.Length; i++)
            {
                FoleyData data = foleyData[i];

                if (data != null)
                {
                    if (!string.IsNullOrEmpty(data.foleyName))
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
                                foleyEventSetData.Add(data.foleyName, tempList.ToArray());
                            }
                        }
                    }
                }
            }

            return foleyEventSetData;
        }
    }
}