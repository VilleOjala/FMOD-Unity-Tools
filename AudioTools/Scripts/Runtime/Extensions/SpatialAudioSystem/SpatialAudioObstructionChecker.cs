// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace FMODUnityTools
{
    public static class SpatialAudioObstructionChecker
    {
        public static float ObstructionCheck(Vector3 listenerPosition, Vector3 emitterPosition, LayerMask layerMask, 
                                             QueryTriggerInteraction queryTriggers, float raycastSpread, bool drawDebugLines)
        {
            float numberOfRaysObstructed = 0; // out of 9.

            Vector3 listenerToEmitterDirection = (emitterPosition - listenerPosition).normalized;
            Vector3 emitterToListenerDirection = -listenerToEmitterDirection;

            Vector3 leftFromListenerDirection = Vector3.Cross(listenerToEmitterDirection, Vector3.up);
            Vector3 leftFromListenerPosition = listenerPosition + leftFromListenerDirection * raycastSpread;

            Vector3 leftFromEmitterDirection = Vector3.Cross(emitterToListenerDirection, Vector3.up);
            Vector3 leftFromEmitterPosition = emitterPosition + leftFromEmitterDirection * raycastSpread;

            Vector3 rightFromListenerDirection = -Vector3.Cross(listenerToEmitterDirection, Vector3.up);
            Vector3 rightFromListenerPosition = listenerPosition + rightFromListenerDirection * raycastSpread;

            Vector3 rightFromEmitterDirection = -Vector3.Cross(emitterToListenerDirection, Vector3.up);
            Vector3 rightFromEmitterPosition = emitterPosition + rightFromEmitterDirection * raycastSpread;

            numberOfRaysObstructed += ObstructionRaycast(emitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(leftFromEmitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(rightFromEmitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(leftFromEmitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(rightFromEmitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(emitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(emitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(leftFromEmitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines);
            numberOfRaysObstructed += ObstructionRaycast(rightFromEmitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines);

            return numberOfRaysObstructed / 9;
        }

        private static int ObstructionRaycast(Vector3 origin, Vector3 end, LayerMask layerMask, QueryTriggerInteraction queryTriggers, bool debug) 
        {
            int result = 0;
            var color = Color.green;

            if (Physics.Linecast(origin, end, layerMask, queryTriggers))
            {
                color = Color.red;
                result = 1;      
            }

#if UNITY_EDITOR

            if (debug)
            {
                DebugDraw(origin, end, color);
            }
#endif
            return result;          
        }

        private static void DebugDraw(Vector3 origin, Vector3 end, Color color)
        {
            Debug.DrawLine(origin, end, color);
        }
    }
}