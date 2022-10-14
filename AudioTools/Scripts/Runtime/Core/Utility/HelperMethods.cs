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

            return (collider.ClosestPoint(position) - position).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
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
                    Debug.LogWarning("Referenced event description is invalid and the provided event reference is null.");
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
    }
}