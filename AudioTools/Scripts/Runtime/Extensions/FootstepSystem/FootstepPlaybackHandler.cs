// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Footstep System/Footstep Playback Handler")]
    public class FootstepPlaybackHandler : MonoBehaviour
    {
        #region Triggering Related Variables

        [Tooltip("This should be located at the base of the character's game object and not be any of Avatar-associated transforms.")]
        public Transform referenceTransform;

        public Animator animator;

        public Transform leftFootPosition;
        public Transform rightFootPosition;
        public Transform foleyPosition;

        [Space(12)]
        [Tooltip("The name of the boolean parameter inside the animator that tracks whether the characted is grounded or not. " +
                 "Obligatory for this tool to work.")]
        public string groundedParameter;

        [Tooltip("The name of the boolean parameter inside the animator that tracks whether the characted is crouched or not. " +
                 "If not provided, the character is always considered to be standing up.")]
        public string crouchedParameter;
        private bool hasCrouchParameter = false;

        [Tooltip("The name of the boolean parameter inside the animator that tracks whether the characted is moving up the stairs. " +
                 "If not provided, only the regular 'walk', 'run' and 'crouched' -movement types will be triggered.")]
        public string stairsUpParameter;
        private bool hasStairsUpParameter = false;

        [Tooltip("The name of the boolean parameter inside the animator that tracks whether the characted is moving down the stairs. " +
                 "If not provided, only the regular 'walk', 'run' and 'crouched' -movement types will be triggered.")]
        public string stairsDownParameter;
        private bool hasStairsDownParameter = false;

        [Space(12)]
        [Range(0.0f, 100.0f)]
        [Tooltip("The approximate maximum velocity that the character can achieve while moving. " +
                 "Use this to better fit the adjustment ranges of the below settings to the needs of your game.")]
        public float adjustMaximumVelocity = 10.0f;

        [Tooltip("The animator velocity threshold above which the character is considered to be moving. " +
                 "Controls the triggering of the 'sidle' - movement type.")]
        public float movingThresholdVelocity = 0.1f;
        private bool isMoving = false;

        public int minLimit = 0;
        public float maxLimit = 0.0f;

        // Good starting point for the walk of the third person character included in Unity's standard assets : 0.5f 
        // Good starting point for the run of the third person character included in Unity's standard assets : 3.0f 
        public float walkLimit = 0.5f;
        public float runLimit = 3.0f;

        [Tooltip("Lowers the animation velocity threshold for walk footsteps when the character is crouched.")]
        public float crouchedWalkAdjustment = 0;
        [Tooltip("Lowers the animation velocity threshold for run footsteps when the character is crouched.")]
        public float crouchedRunAdjustment = 0;

        private bool isAlive = true;
        private bool isGrounded = true;
        private bool cacheIsGrounded = true;
        private bool isCrouched = false;
        private bool isGoingUpTheStairs = false;
        private bool isGoingDownTheStairs = false;

        [Tooltip("The distance from the foot position game object to the reference point that must first be surpassed " +
                 "for the footstep triggering to be evaluated.")]
        public float thresholdHeight = 0.0f;
        [Tooltip("Lowers the 'Threshold Height' -value when the character is crouched.")]
        public float crouchedThresholdAdjustment = 0;

        [Tooltip("The distance of the foot position game object to the reference point at which the foot is again considered to be 'grounded' " +
                 "following the surpassing of the threshold height.")]
        public float distanceWhenGrounded = 0.0f;

        private float currentAnimatorVelocity = 0;

        private int previousLeftFootDirection = 0;
        private int previousRightFootDirection = 0;
        private float previousLeftFootHeight = 0;
        private float previousRightFootHeight = 0;
        private bool hasRaisedLeftFoot = false;
        private bool hasRaisedRightFoot = false;

        // Delay after player grounding before we check for regular footsteps again. 
        // <- Leave space for specific grounding sfxs.
        private float delay = 0.3f;
        private float timer = float.MaxValue;

        // Cooldown period before a sidle sfx can be triggered again.
        private float sidleCooldownDuration = 0.7f;
        private bool sidleCooldownInProgress = false;

        private bool initializationSuccesfull = false;

        // Inspector runtime feet height debugging:
        public string leftHeightDebug = "";
        public string rightHeightDebug = "";
        public string velocityDebug = "";

        #endregion

        #region Surface Check Variables

        public SurfaceType fallbackSurface = SurfaceType.Concrete;
        public SurfaceLayerType fallbackLayer = SurfaceLayerType.None;
        public LayerMask raycastLayerMask = 1<<0;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;
        [Min(0f)]
        public float raycastMaxDistance = 1.0f;
        public float adjustRaycastOriginY = 0.0f;

        #endregion

        #region Event Set Importing

        public ShoeOrFeetType shoeOrFeetType = ShoeOrFeetType.Barefeet;
        public FoleyType foleyType = FoleyType.CombatGear;
        public bool spatialAudioRoomAware = false;

        // Footsteps
        public FootstepEventSet footstepEventSet;
        public bool footstepEventSetImported = false;
        public GameObject footstepParentObject;
        public List<FootstepImport> footstepImports = new List<FootstepImport>();

        [Serializable]
        public class FootstepImport
        {
            public string key;
            public AudioObject audioObject;
        }

        // Foley
        public FoleyEventSet foleyEventSet;
        public bool foleyEventSetImported = false;
        public GameObject foleyParentObject;
        public List<FoleyImport> foleyImports = new List<FoleyImport>();

        [Serializable]
        public class FoleyImport
        {
            public string key;
            public AudioObject audioObject;
        }

        // Layer
        public LayerEventSet layerEventSet;
        public bool layerEventSetImported = false;
        public GameObject layerParentObject;
        public List<LayerImport> layerImports = new List<LayerImport>();

        [Serializable]
        public class LayerImport
        {
            public string key;
            public AudioObject audioObject;
        }
        #endregion

        #region Callbacks

        void Awake()
        {
            if (animator == null)
            {
                Debug.LogError("Animator is null for Footstep Playback Handler '" + gameObject.name + "'.");
                return;
            }

            if (string.IsNullOrEmpty(groundedParameter))
            {
                Debug.LogError("Grounded Parameter is null or empty for Footstep Playback Handler '" + gameObject.name + "'.");
                return;
            }

            if (referenceTransform == null)
            {
                Debug.LogError("Feet level transform is missin for Footstep Playback Handler '" + gameObject.name + "'.");
                return;
            }

            if (string.IsNullOrEmpty(crouchedParameter))
            {
                Debug.LogWarning("Crouched Parameter is null or empty for Footstep Playback Handler '" + gameObject.name + "'.");
            }
            else
            {
                hasCrouchParameter = true;
            }

            if (string.IsNullOrEmpty(stairsUpParameter))
            {
                Debug.LogWarning("Stairs Up Parameter is null or empty for Footstep Playback Handler '" + gameObject.name + "'.");
            }
            else
            {
                hasStairsUpParameter = true;
            }

            if (string.IsNullOrEmpty(stairsDownParameter))
            {
                Debug.LogWarning("Stairs Up Parameter is null or empty for Footstep Feet Position Listener '" + gameObject.name + "'.");
            }
            else
            {
                hasStairsDownParameter = true;
            }

            initializationSuccesfull = true;
        }
 
        void FixedUpdate()
        {
            if (initializationSuccesfull && animator != null && isAlive)
            {
                if (timer < delay)
                {
                    timer += Time.fixedDeltaTime;
                    return;
                }
                else
                {
                    timer = float.MaxValue;
                }

                isGrounded = animator.GetBool(groundedParameter);

                if (!isGrounded && cacheIsGrounded)
                {
                    cacheIsGrounded = false;
                }
                else if (isGrounded && !cacheIsGrounded)
                {
                    // Start a delay timer upon landing to temporarily prevent the triggering of regular footsteps.
                    // Grounding should have a seperate deterministic sfx tied to the landing animation / animator state / some other appropriate hook.
                    // <- Highly game & situation specific (with variation in falling heights & animations associated with them etc.).
                    // <- However, a custom system for landings can still of course call the Footstep Playback Handler to get info about the landing material.
                    cacheIsGrounded = true;
                    timer = 0;
                    return;
                }

                currentAnimatorVelocity = animator.velocity.magnitude;

                if (hasCrouchParameter)
                    isCrouched = animator.GetBool(crouchedParameter);

                if (hasStairsUpParameter)
                    isGoingUpTheStairs = animator.GetBool(stairsUpParameter);

                if (hasStairsDownParameter)
                    isGoingDownTheStairs = animator.GetBool(stairsDownParameter);

                FootstepUpdate();
            }
        }

        void OnValidate()
        {
            SanityCheck();   
        }

        #endregion

        #region Footstep Check

        private void FootstepUpdate()
        {
            // In addition to aborting the regular footstep update check when the player is not grounded one should add all the other applicable game specific checks here.
            // <- Could involve movement types such as climbing, vaulting or any kind of deterministic animation sequence with their own sfx triggering requirements.             
            if (!isGrounded)
            {
                previousLeftFootDirection = 0;
                previousRightFootDirection = 0;
                previousLeftFootHeight = 0;
                previousRightFootHeight = 0;
                hasRaisedLeftFoot = false;
                hasRaisedRightFoot = false;

                isMoving = false;
                StopAllCoroutines();
                sidleCooldownInProgress = false;
                return;
            }

            float leftHeight = GetFootHeight(true);
            float rightHeight = GetFootHeight(false);

            if (leftHeight == int.MaxValue || rightHeight == int.MaxValue || leftHeight < 0 || rightHeight < 0)
            {
                // Feet height checks failed.
                return;
            }

            #if UNITY_EDITOR
            var roundLeft = Math.Round(leftHeight, 2);
            leftHeightDebug = roundLeft.ToString();
            var roundRight = Math.Round(rightHeight, 2);
            rightHeightDebug = roundRight.ToString();
            var roundVelocity = Math.Round(currentAnimatorVelocity, 2);
            velocityDebug = roundVelocity.ToString();
            #endif

            bool footstepTriggered = false;

            int leftFootDirection;
            int rightFootDirection;

            if (leftHeight > previousLeftFootHeight)
            {
                leftFootDirection = 1;
            }
            else if (leftHeight < previousLeftFootHeight)
            {
                leftFootDirection = -1;
            }
            else
            {
                leftFootDirection = previousLeftFootDirection;
            }

            if (rightHeight > previousRightFootHeight)
            {
                rightFootDirection = 1;
            }
            else if (rightHeight < previousRightFootHeight)
            {
                rightFootDirection = -1;
            }
            else
            {
                rightFootDirection = previousRightFootDirection;
            }

            if (leftHeight > thresholdHeight && !hasRaisedLeftFoot)
            {
                hasRaisedLeftFoot = true;
            }
            else if (isCrouched && leftHeight > (thresholdHeight + crouchedThresholdAdjustment) && !hasRaisedLeftFoot)
            {
                hasRaisedLeftFoot = true;
            }
            else if (leftHeight < distanceWhenGrounded && hasRaisedLeftFoot)
            {
                hasRaisedLeftFoot = false;

                if (leftFootDirection < 0)
                {
                    if (currentAnimatorVelocity > walkLimit)
                    {
                        footstepTriggered = true;
                        FootstepSurfaceInfo surfaceInfo = CheckCurrentSurface(leftFootPosition);
                        TriggerMovementSounds(surfaceInfo, true);
                    }
                    else if (isCrouched && currentAnimatorVelocity >= (walkLimit + crouchedWalkAdjustment))
                    {
                        footstepTriggered = true;
                        FootstepSurfaceInfo surfaceInfo = CheckCurrentSurface(leftFootPosition);
                        TriggerMovementSounds(surfaceInfo, true);
                    }
                }
            }

            if (rightHeight > thresholdHeight && !hasRaisedRightFoot)
            {
                hasRaisedRightFoot = true;
            }
            else if (isCrouched && rightHeight > (thresholdHeight + crouchedThresholdAdjustment) && !hasRaisedRightFoot)
            {
                hasRaisedRightFoot = true;
            }
            else if (rightHeight < distanceWhenGrounded && hasRaisedRightFoot)
            {
                hasRaisedRightFoot = false;

                if (rightFootDirection < 0)
                {
                    if (currentAnimatorVelocity > walkLimit)
                    {
                        footstepTriggered = true;
                        FootstepSurfaceInfo surfaceInfo = CheckCurrentSurface(rightFootPosition);
                        TriggerMovementSounds(surfaceInfo, false);
                    }
                    else if (isCrouched && currentAnimatorVelocity >= (walkLimit + crouchedWalkAdjustment))
                    {
                        footstepTriggered = true;
                        FootstepSurfaceInfo surfaceInfo = CheckCurrentSurface(rightFootPosition);
                        TriggerMovementSounds(surfaceInfo, false);
                    }
                }
            }

            previousLeftFootDirection = leftFootDirection;
            previousRightFootDirection = rightFootDirection;

            previousLeftFootHeight = leftHeight;
            previousRightFootHeight = rightHeight;

            if (footstepTriggered)
                return;

            // SIDLE:
            // Animator velocity threshold for the character to be considered to be moving is adjustable.
            // <- The character needs to have stopped before the siddle sfx can be triggered again.
            // <- In addition, there is a cooldown period before the siddle sfx can be triggered again.
            //    <- This is because momentary hovering around the threshold value can lead to 'machine gun' -triggering of the sfx.

            // Since the sidle triggering is not foot-specific, as a compromise we will use the surface and position data of the left foot here.
            // <- Should not matter too much, unless the character is very large, in which case splitting the feet siddling sounds to foot-specific
            //    events could come into consideration.

            if (currentAnimatorVelocity > movingThresholdVelocity && !isMoving && !sidleCooldownInProgress)
            {
                FootstepSurfaceInfo surfaceInfo = CheckCurrentSurface(leftFootPosition);

                if (isCrouched)
                {   
                    if (foleyPosition != null)
                    {
                        PlayFoley(MovementType.SidleCrouched, foleyPosition);
                    }

                    PlayFootstep(MovementType.SidleCrouched, surfaceInfo.surfaceType, leftFootPosition);
                    PlayLayer(MovementType.SidleCrouched, surfaceInfo.layerType, leftFootPosition);              
                }
                else
                { 
                    if (foleyPosition != null)
                    {
                        PlayFoley(MovementType.Sidle, foleyPosition);
                    }

                    PlayFootstep(MovementType.Sidle, surfaceInfo.surfaceType, leftFootPosition);
                    PlayLayer(MovementType.Sidle, surfaceInfo.layerType, leftFootPosition);                 
                }

                isMoving = true;
                sidleCooldownInProgress = true;
                StartCoroutine(Cooldown(sidleCooldownDuration));
            }
            else if (currentAnimatorVelocity < movingThresholdVelocity && isMoving)
            {
                isMoving = false;
            }
        }

        private float GetFootHeight(bool getLeftFoot)
        {
            if (referenceTransform == null)
                return -1;

            Transform footTransform = null;

            if (getLeftFoot && leftFootPosition != null)
            {
                footTransform = leftFootPosition;
            }
            else if (!getLeftFoot && rightFootPosition != null)
            {
                footTransform = rightFootPosition;
            }

            if (footTransform == null)
                return -1;

            float posX = footTransform.position.x;
            float posZ = footTransform.position.z;
            float posY = referenceTransform.position.y;

            Vector3 measurementPosition = new Vector3(posX, posY, posZ);
            float distance = Vector3.Distance(measurementPosition, footTransform.position);

            return distance;
        }

        IEnumerator Cooldown(float time)
        {
            yield return new WaitForSeconds(time);

            sidleCooldownInProgress = false;
        }

        private void TriggerMovementSounds(FootstepSurfaceInfo surfaceInfo, bool isLeftFoot)
        {
            MovementType movementType;

            if (isCrouched)
            {
                if (currentAnimatorVelocity >= runLimit + crouchedRunAdjustment)
                {
                    if (isGoingUpTheStairs)
                        movementType = MovementType.RunCrouchedStairsUp;
                    else if (isGoingDownTheStairs)
                        movementType = MovementType.RunCrouchedStairsDown;
                    else
                        movementType = MovementType.RunCrouched;
                }
                else
                {
                    if (isGoingUpTheStairs)
                        movementType = MovementType.WalkCrouchedStairsUp;
                    else if (isGoingDownTheStairs)
                        movementType = MovementType.WalkCrouchedStairsDown;
                    else
                        movementType = MovementType.WalkCrouched;
                }
            }
            else if (currentAnimatorVelocity >= runLimit)
            {
                if (isGoingUpTheStairs)
                    movementType = MovementType.RunStairsUp;
                else if (isGoingDownTheStairs)
                    movementType = MovementType.RunStairsDown;
                else
                    movementType = MovementType.Run;
            }
            else
            {
                if (isGoingUpTheStairs)
                    movementType = MovementType.WalkStairsUp;
                else if (isGoingDownTheStairs)
                    movementType = MovementType.WalkStairsDown;
                else
                    movementType = MovementType.Walk;
            }

            if (isLeftFoot)
            {
                PlayFootstep(movementType, surfaceInfo.surfaceType, leftFootPosition);

                if (foleyPosition != null)
                {
                    PlayFoley(movementType, foleyPosition);
                }

                if (surfaceInfo.layerType != SurfaceLayerType.None)
                    PlayLayer(movementType, surfaceInfo.layerType, leftFootPosition);
            }
            else
            {
                PlayFootstep(movementType, surfaceInfo.surfaceType, rightFootPosition);

                if (foleyPosition != null)
                {
                    PlayFoley(movementType, foleyPosition);
                }

                if (surfaceInfo.layerType != SurfaceLayerType.None)
                    PlayLayer(movementType, surfaceInfo.layerType, rightFootPosition);
            }
        }
        #endregion

        #region Playback

        private void PlayFootstep(MovementType movementType, SurfaceType surfaceType, Transform footPosition = null)
        {
            foreach (FootstepImport import in footstepImports)
            {
                if (import.key == shoeOrFeetType.ToString() + "_" + surfaceType.ToString() + "_" + movementType.ToString())
                {
                    if (import.audioObject != null)
                    {
                        import.audioObject.TriggerDirectly(TriggeringAction.StartSound, footPosition);
                    }
                }
            }
        }

        private void PlayFoley(MovementType movementType, Transform foleyPosition = null)
        {
            foreach (FoleyImport import in foleyImports)
            {
                if (import.key == foleyType.ToString() + "_" + movementType.ToString())
                {
                    if (import.audioObject != null)
                    {
                        import.audioObject.TriggerDirectly(TriggeringAction.StartSound, foleyPosition);
                    }
                }
            }
        }

        private void PlayLayer(MovementType movementType, SurfaceLayerType surfaceLayerType, Transform layerPosition = null)
        {
            foreach (LayerImport import in layerImports)
            {
                if (surfaceLayerType != SurfaceLayerType.None && import.key == surfaceLayerType.ToString() + "_" + movementType.ToString())
                {
                    if (import.audioObject != null)
                    {
                        import.audioObject.TriggerDirectly(TriggeringAction.StartSound, layerPosition);
                    }
                }
            }
        }
        #endregion

        #region Public Methods

        public FootstepSurfaceInfo CheckCurrentSurface(Transform raycastOriginTransform)
        {
            FootstepSurfaceInfo surfaceInfo;

            Vector3 origin = raycastOriginTransform.position;

            origin.y += adjustRaycastOriginY;

            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, raycastMaxDistance, raycastLayerMask, queryTriggerInteraction);

            AudioSurfaceTag closestTag = null;
            float distanceToTag = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                var surfaceTag = hit.collider.gameObject.GetComponent<AudioSurfaceTag>();

                if (surfaceTag != null && hit.distance < distanceToTag)
                {
                    closestTag = surfaceTag;
                    distanceToTag = hit.distance;
                }
            }

            if (closestTag != null)
            {
                surfaceInfo.surfaceType = closestTag.surfaceType;
                surfaceInfo.layerType = closestTag.surfaceLayerType;

                return surfaceInfo;
            }
            else
            {
                surfaceInfo.surfaceType = fallbackSurface;
                surfaceInfo.layerType = fallbackLayer;
                return surfaceInfo;
            }
        }

        public void SanityCheck()
        {
            if (Application.isPlaying)
                return;

            if (thresholdHeight < 0)
                thresholdHeight = 0;

            if (distanceWhenGrounded < 0)
                distanceWhenGrounded = 0;

            if (thresholdHeight < distanceWhenGrounded)
                thresholdHeight = distanceWhenGrounded;

            maxLimit = adjustMaximumVelocity;

            if (walkLimit > maxLimit)
                walkLimit = maxLimit;

            if (runLimit > maxLimit)
                runLimit = maxLimit;

            if (walkLimit > runLimit)
                walkLimit = runLimit;

            if (runLimit < walkLimit)
                runLimit = walkLimit;

            if (movingThresholdVelocity < 0)
                movingThresholdVelocity = 0;

            if (movingThresholdVelocity > maxLimit)
                movingThresholdVelocity = maxLimit;

            if (movingThresholdVelocity > walkLimit)
                movingThresholdVelocity = walkLimit;

            if (crouchedWalkAdjustment > 0)
                crouchedWalkAdjustment = 0;

            if (-crouchedWalkAdjustment > walkLimit)
                crouchedWalkAdjustment = -walkLimit;

            if (crouchedRunAdjustment > 0)
                crouchedRunAdjustment = 0;

            float maxRunAdjustment = runLimit - walkLimit + -crouchedWalkAdjustment;

            if (-crouchedRunAdjustment > maxRunAdjustment)
                crouchedRunAdjustment = -maxRunAdjustment;

            if (crouchedThresholdAdjustment > 0)
                crouchedThresholdAdjustment = 0;

            if (-crouchedThresholdAdjustment > thresholdHeight)
                crouchedThresholdAdjustment = -thresholdHeight;
        }

        public void SetShoeOrFeetType(ShoeOrFeetType shoeOrFeetType)
        {
            this.shoeOrFeetType = shoeOrFeetType;
        }

        public void SetFoleyType(FoleyType foleyType)
        {
            this.foleyType = foleyType;
        }

        public void SetCharacterAliveStatus(bool isAlive)
        {
            this.isAlive = isAlive;
        }
        #endregion
    }
}