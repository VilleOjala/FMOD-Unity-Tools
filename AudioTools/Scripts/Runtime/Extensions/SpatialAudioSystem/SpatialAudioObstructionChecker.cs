// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace FMODUnityTools
{
    public static class SpatialAudioObstructionChecker
    {
        // The minimum hit point angle is the largest angle between "listener-to-hit-point" and "listener-to-emitter" 
        // -direction vectors where the ray is still registered as obstructed. This is done to prevent false positives 
        // in situations where the player actually stands in between the emitter and obstructing object but the offset point
        // raycasts still hit the "obstructing" object.
        const float MaximumHitPointAngle = 62.0f;

        public static float ObstructionCheck(Vector3 listenerPosition, Vector3 emitterPosition, bool requireObstructionTag, LayerMask layerMask,
                                              QueryTriggerInteraction queryTriggers, float raycastSpread, bool drawDebugLines, Collider ignoreSelf = null)
        {
            float numberOfRaysObstructed = 0; // out of 9.

            Vector3 listenerToEmitterDirection = emitterPosition - listenerPosition;
            Vector3 emitterToListenerDirection = listenerPosition - emitterPosition;

            Vector3 leftFromListenerDirection = Vector3.Cross(listenerToEmitterDirection, Vector3.up).normalized;
            Vector3 leftFromListenerPosition = listenerPosition + (leftFromListenerDirection * raycastSpread);

            Vector3 leftFromEmitterDirection = Vector3.Cross(emitterToListenerDirection, Vector3.up).normalized;
            Vector3 leftFromEmitterPosition = emitterPosition + (leftFromEmitterDirection * raycastSpread);

            Vector3 rightFromListenerDirection = -Vector3.Cross(listenerToEmitterDirection, Vector3.up).normalized;
            Vector3 rightFromListenerPosition = listenerPosition + (rightFromListenerDirection * raycastSpread);

            Vector3 rightFromEmitterDirection = -Vector3.Cross(emitterToListenerDirection, Vector3.up).normalized;
            Vector3 rightFromEmitterPosition = emitterPosition + (rightFromEmitterDirection * raycastSpread);

            if (requireObstructionTag)
            {
                numberOfRaysObstructed += RaycastTaggedObstruction(emitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(leftFromEmitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(rightFromEmitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(leftFromEmitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(rightFromEmitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(emitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(emitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(leftFromEmitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);
                numberOfRaysObstructed += RaycastTaggedObstruction(rightFromEmitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines, listenerPosition, listenerToEmitterDirection);

                return numberOfRaysObstructed / 9;
            }
            else
            {
                numberOfRaysObstructed += RaycastObstruction(emitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(leftFromEmitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(rightFromEmitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(leftFromEmitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(rightFromEmitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(emitterPosition, leftFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(emitterPosition, rightFromListenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(leftFromEmitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);
                numberOfRaysObstructed += RaycastObstruction(rightFromEmitterPosition, listenerPosition, layerMask, queryTriggers, drawDebugLines, listenerToEmitterDirection, listenerPosition, ignoreSelf);

                return numberOfRaysObstructed / 9;
            }
        }

        private static int RaycastTaggedObstruction(Vector3 origin, Vector3 end, LayerMask layerMask, 
                                                    QueryTriggerInteraction queryTriggers, bool debug, 
                                                    Vector3 listenerPosition, Vector3 listenerToEmitterDirection)
        {
            Vector3 direction = (end - origin).normalized;
            float rayLength = Vector3.Distance(origin, end);

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, rayLength, layerMask, queryTriggers);

            for (int i = 0; i < hits.Length; i++)
            {
                if (SpatialAudioManager.Instance != null && SpatialAudioManager.Instance.IsObstructingCollider(hits[i].collider))
                {
                    Vector3 listenerToHitPointDirection = hits[i].point - listenerPosition;

                    float angle = Vector3.Angle(listenerToEmitterDirection, listenerToHitPointDirection);

                    if (angle < MaximumHitPointAngle)
                    {
                        if (debug)
                            DebugDraw(origin, end, Color.red);
                        return 1;
                    }
                    else
                    {
                        if (debug)
                            DebugDraw(origin, end, Color.green);
                        return 0;
                    }
                }

                var tag = hits[i].collider.gameObject.GetComponent<SpatialAudioObstructionTag>();

                if (tag != null)
                {
                    if (debug)
                        DebugDraw(origin, end, Color.red);
                    return 1;
                }
            }
            if (debug)
                DebugDraw(origin, end, Color.green);
            return 0;
        }

        private static int RaycastObstruction(Vector3 origin, Vector3 end, LayerMask layerMask, QueryTriggerInteraction queryTriggers, bool debug,
                                              Vector3 listenerToEmitterDirection, Vector3 listenerPosition, Collider ignoreSelf = null)
        {
            if (Physics.Linecast(origin, end, out RaycastHit hit, layerMask, queryTriggers))
            {
                if (ignoreSelf != null && hit.collider == ignoreSelf)
                {
                    if (debug)
                        DebugDraw(origin, end, Color.green);
                    return 0;
                }
                else
                {
                    Vector3 listenerToHitPointDirection = hit.point - listenerPosition;
                    float angle = Vector3.Angle(listenerToEmitterDirection, listenerToHitPointDirection);

                    if (angle < MaximumHitPointAngle)
                    {
                        if (debug)
                            DebugDraw(origin, end, Color.red);
                        return 1;
                    }

                    if (debug)
                        DebugDraw(origin, end, Color.green);

                    return 0;
                }
            }
            else
            {
                if (debug)
                    DebugDraw(origin, end, Color.green);

                return 0;
            }
        }

        private static void DebugDraw(Vector3 origin, Vector3 end, Color color)
        {
            Debug.DrawLine(origin, end, color);
        }
    }
}