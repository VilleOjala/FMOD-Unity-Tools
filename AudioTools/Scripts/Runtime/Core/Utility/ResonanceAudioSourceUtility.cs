// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using System;

namespace AudioTools
{
    public static class ResonanceAudioSourceUtility
    {
        public static float GetResonanceAudioSourceMaxDistance(FMOD.Studio.EventInstance eventInstance)
        {
            if (!eventInstance.isValid())
            {
                Debug.LogError("Resonance Audio Source max distance could not be retrieved. FMOD Studio Event Instance was not valid.");
                return -1;
            }

            FMOD.RESULT result;

            FMOD.ChannelGroup channelGroup;
            result = eventInstance.getChannelGroup(out channelGroup);
            if (result != FMOD.RESULT.OK)
            {
                Debug.Log(result);
                return -1;
            }

            int dspNumber;
            result = channelGroup.getNumDSPs(out dspNumber);
            if (result != FMOD.RESULT.OK)
            {
                Debug.Log(result);
                return -1;
            }

            for (int i = 0; i < dspNumber; i++)
            {
                FMOD.DSP dsp;
                result = channelGroup.getDSP(i, out dsp);
                if (result != FMOD.RESULT.OK)
                {
                    Debug.Log(result);
                    continue;
                }

                result = dsp.getInfo(out string name, out uint version, out int channels, out int configWidth, out int configHeight);
                if (result != FMOD.RESULT.OK)
                {
                    Debug.Log(result);
                    continue;
                }

                if (name == "Resonance Audio Source")
                {
                    result = dsp.getNumParameters(out int parameterNumber);
                    if (result != FMOD.RESULT.OK)
                    {
                        Debug.Log(result);
                        continue;
                    }

                    for (int j = 0; j < parameterNumber; j++)
                    {
                        result = dsp.getParameterInfo(j, out FMOD.DSP_PARAMETER_DESC description);
                        if (result != FMOD.RESULT.OK)
                        {
                            Debug.Log(result);
                            continue;
                        }

                        string stringName = System.Text.Encoding.UTF8.GetString(description.name);

                        if (String.Equals(stringName, "Max Distance", StringComparison.InvariantCultureIgnoreCase))
                        {
                            result = dsp.getParameterFloat(j, out float maxDistanceValue);
                            if (result != FMOD.RESULT.OK)
                            {
                                Debug.Log(result);
                                continue;
                            }

                            return maxDistanceValue;
                        }
                    }
                }
            }

            Debug.LogError("Resonance Audio Source max distance could not be retrieved");
            return -1;
        }

        public static bool SetResonanceAudioSourceMaxDistance(FMOD.Studio.EventInstance eventInstance, float maxDistanceValue)
        {
            if (!eventInstance.isValid() || maxDistanceValue < 0)
            {
                return false;
            }

            FMOD.RESULT result;

            FMOD.ChannelGroup channelGroup;
            result = eventInstance.getChannelGroup(out channelGroup);
            if (result != FMOD.RESULT.OK)
            {
                Debug.Log(result);
                return false;
            }

            int dspNumber;
            result = channelGroup.getNumDSPs(out dspNumber);
            if (result != FMOD.RESULT.OK)
            {
                Debug.Log(result);
                return false;
            }

            for (int i = 0; i < dspNumber; i++)
            {
                FMOD.DSP dsp;
                result = channelGroup.getDSP(i, out dsp);
                if (result != FMOD.RESULT.OK)
                {
                    Debug.Log(result);
                    continue;
                }

                result = dsp.getInfo(out string name, out uint version, out int channels, out int configWidth, out int configHeight);
                if (result != FMOD.RESULT.OK)
                {
                    Debug.Log(result);
                    continue;
                }

                if (name == "Resonance Audio Source")
                {
                    result = dsp.getNumParameters(out int parameterNumber);
                    if (result != FMOD.RESULT.OK)
                    {
                        Debug.Log(result);
                        continue;
                    }

                    for (int j = 0; j < parameterNumber; j++)
                    {
                        result = dsp.getParameterInfo(j, out FMOD.DSP_PARAMETER_DESC description);
                        if (result != FMOD.RESULT.OK)
                        {
                            Debug.Log(result);
                            continue;
                        }

                        string stringName = System.Text.Encoding.UTF8.GetString(description.name);

                        if (String.Equals(stringName, "Max Distance", StringComparison.InvariantCultureIgnoreCase))
                        {
                            result = dsp.setParameterFloat(j, maxDistanceValue);
                            if (result != FMOD.RESULT.OK)
                            {
                                Debug.Log(result);
                                continue;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
