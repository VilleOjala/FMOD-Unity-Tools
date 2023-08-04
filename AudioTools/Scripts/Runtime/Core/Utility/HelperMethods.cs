// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace FMODUnityTools
{
    public static class HelperMethods 
    {
        public static bool CheckIfInsideCollider(Vector3 position, Collider collider)
        {
            if (collider == null)
                return false;

            return Mathf.Approximately((collider.ClosestPoint(position) - position).magnitude, 0);
        }

        public static bool GetIfLayerMaskContainsLayer(int layer, LayerMask layerMask)
        {
            return layerMask == (layerMask | 1 << layer);
        }

        public static bool TryGetListenerPosition(out Vector3 listenerPosition)
        {
            listenerPosition = default;
            var result = RuntimeManager.StudioSystem.getListenerAttributes(0, out FMOD.ATTRIBUTES_3D attributes, out FMOD.VECTOR attenuationPosition);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError(result);
                return false;
            }
            else
            {
                listenerPosition = GetUnityVector(attenuationPosition);
                return true;
            }
        }

        public static Vector3 GetUnityVector(FMOD.VECTOR fmodVector)
        {
            return new Vector3(fmodVector.x, fmodVector.y, fmodVector.z);
        }

        public static bool TryRetrieveDescriptionIfNotAlreadyValid(EventReference eventReference, ref EventDescription eventDescription)
        {
            if (eventDescription.isValid())
            {
                return true;
            }
            else
            {
                if (eventReference.IsNull)
                {
                    Debug.LogWarning("Referenced EventDescription is invalid and the provided EventReference is null.");
                    return false;
                }
                else
                {
                    eventDescription = RuntimeManager.GetEventDescription(eventReference);

                    if (eventDescription.isValid())
                    {
                        return true;
                    }

                    Debug.LogWarning("Provided event reference is not null but nevertheless invalid.");
                    return false;
                }
            }
        }

        public static Vector3 GetEventInstancePosition(EventInstance eventInstance)
        {
            if (!eventInstance.isValid())
                return default;

            var result = eventInstance.get3DAttributes(out FMOD.ATTRIBUTES_3D attributes);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError(result);
            }

            return GetUnityVector(attributes.position);
        }

        public static bool InitializeLocalParameterID(EventDescription eventDescription, string parameterName, ref PARAMETER_ID parameterID, bool debugPrint = true)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                if (debugPrint)
                {
                    Debug.LogError("Local Parameter_ID initialization failed. Provided parameter name was null or empty.");
                }

                return false;
            }

            if (!eventDescription.isValid())
            {
                if (debugPrint)
                {
                    Debug.LogError("Local Parameter_ID initialization failed. Provided EventDescription was invalid.");
                }

                return false;
            }

            var result = eventDescription.getParameterDescriptionByName(parameterName, out PARAMETER_DESCRIPTION description);

            if (result != FMOD.RESULT.OK)
            {
                if (debugPrint)
                {
                    Debug.Log(result + " " + parameterName + " " + description.id);
                }

                return false;
            }

            parameterID = description.id;
            return true;
        }
    }
}