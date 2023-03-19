// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Footstep System/Third Person Footsteps")]
    public class ThirdPersonFootsteps : MonoBehaviour, IEventListener
    {
        public Transform referenceHeight;
        private Animator animator;
        public List<Transform> footTransforms = new List<Transform>();

        [SerializeField, Tooltip("Visible in Inspector for foot height threshold debugging purposes only. Don't set anything manually here.")]
        private List<Foot> feet = new List<Foot>();

        public string verticalParameter;
        public string horizontalParameter;
        private float vertical;
        private float horizontal;

        public string groundedParameter; 
        private bool isGrounded = true;

        public string crouchParameter;
        private bool isCrouched = false;

        public AudioObject footstepAudioObject;
        public AudioObject clothAudioObject;

        public Vector3 rayOffset;
        public LayerMask surfaceCheckLayerMask;
        public float rayLength;
        public WaterDepthThresholds waterDepthThresholds;

        public EventTag jumpEventTag;
        private bool justJumped = false;
        public EventTag landEventTag;
        private bool justLanded = false;

        [Min(0)]
        public float footCooldown = 0.3f;
        private List<Coroutine> cooldowns = new List<Coroutine>();

        [Min(0)]
        public float crouchThreshold;
        [Min(0)]
        public float walkThreshold;
        [Min(0)]
        public float runThreshold;

        [Serializable]
        private class Foot
        {
            public Transform footTransform;
            public bool isRaised;
            public bool isCoolingDown;
            public float debugRelativeHeight;

            public Foot(Transform footTransform)
            {
                this.footTransform = footTransform;
                this.isRaised = false;
                this.isCoolingDown = false;
            }
        }

        public enum MovementMode
        {
            None = 0,
            Walk = 1,
            Run = 2,
            Crouch = 3,
            Jump = 4,
            Land = 5
        }

        [Serializable]
        public struct FootstepHeightThresholds
        {
            [Min(0)] public float raisedHeight;
            [Min(0)] public float triggerHeight;
        }

        public FootstepHeightThresholds crouchHeightThresholds;
        public FootstepHeightThresholds walkHeightThresholds;
        public FootstepHeightThresholds runHeightThresholds;

        //Editor debugging->
        [Serializable]
        private struct DebugInfo
        {
            public float vertical;
            public float horizontal;
            public float speed;
            public bool isGrounded;
            public bool isCrouched;
            public string locomotionType;
        }

        [SerializeField]
        private DebugInfo debugInfo;

        [SerializeField]
        private bool debugDrawRaycast = false;

        [SerializeField]
        private bool debugPrintTriggered = false;
        //

        private void Start()
        {
            if (referenceHeight == null)
            {
                referenceHeight = transform;
            }

            animator = GetComponentInParent<Animator>();

            if (animator == null)
            {
                Debug.LogError("Animator reference is null.");
                return;
            }

            if (animator.isHuman)
            {
                footTransforms.Clear();
                footTransforms.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
                footTransforms.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));
            }

            foreach (var footTransform in footTransforms)
            {
                if (footTransform != null)
                {
                    feet.Add(new Foot(footTransform));
                }
            }
        }

        private void OnEnable()
        {
            EventManager.RegisterListener(this);
        }

        private void OnDisable()
        {
            EventManager.UnregisterListener(this);
        }

        private void FixedUpdate()
        {
            if (animator == null)
                return;

            if (referenceHeight == null)
                return;

            if (!string.IsNullOrEmpty(groundedParameter))
            {
                isGrounded = animator.GetBool(groundedParameter);
            }
            else
            {
                isGrounded = true;
            }

            if (!isGrounded)
            {
                ResetAllRaisedFeet();
            }

            bool jumpedOrLanded = false;

            if (justJumped)
            {
                var parameters = GetParameters(MovementMode.Jump, referenceHeight.position + rayOffset);
                PlayMovementAudio(footstepAudioObject, referenceHeight, parameters);
                PlayMovementAudio(clothAudioObject, referenceHeight, parameters);
                jumpedOrLanded = true;
            }

            if (justLanded)
            {
                var parameters = GetParameters(MovementMode.Land, referenceHeight.position + rayOffset);
                PlayMovementAudio(footstepAudioObject, referenceHeight, parameters);
                PlayMovementAudio(clothAudioObject, referenceHeight, parameters);
                jumpedOrLanded = true;
            }

            if (jumpedOrLanded)
            {
                ResetAllRaisedFeet();
                StopCurrentCooldownsAndSetAllFeetToCooldown();
            }

            ResetJumpingAndLanding();

            // Don't try to play a regular step sound immediately after jumping / landing.
            if (jumpedOrLanded)
                return;

            vertical = !string.IsNullOrEmpty(verticalParameter) ? animator.GetFloat(verticalParameter) : 0f;
            horizontal = !string.IsNullOrEmpty(horizontalParameter) ? animator.GetFloat(horizontalParameter) : 0f;
            float speed = GetDistanceFromBlendTreeOrigo(vertical, horizontal);

            isCrouched = false;

            if (!string.IsNullOrEmpty(crouchParameter))
            {
                isCrouched = animator.GetBool(crouchParameter);
            }

            var movementMode = MovementMode.None;

            if (isCrouched && speed >= crouchThreshold)
            {
                movementMode = MovementMode.Crouch;
            }
            else if (speed >= runThreshold)
            {
                movementMode |= MovementMode.Run;
            }
            else if (speed >= walkThreshold)
            {
                movementMode = MovementMode.Walk;
            }

#if UNITY_EDITOR
            debugInfo = new DebugInfo { horizontal = horizontal, vertical = vertical, speed = speed, isCrouched = isCrouched, isGrounded = isGrounded, locomotionType = movementMode.ToString() };
#endif
            if (movementMode == MovementMode.None)
            {
                ResetAllRaisedFeet();
                return;
            }

            foreach (var foot in feet)
            {
                if (foot.isCoolingDown || foot.footTransform == null)
                    continue;

                float relativeHeight = Mathf.Abs(foot.footTransform.position.y - referenceHeight.position.y);
                foot.debugRelativeHeight = relativeHeight;
                var thresholds = GetFootstepHeightThresholds(movementMode);

                if (relativeHeight >= thresholds.raisedHeight && !foot.isRaised)
                {
                    foot.isRaised = true;
                }
                else if (relativeHeight <= thresholds.triggerHeight && foot.isRaised)
                {
                    foot.isRaised = false;
                    var parameters = GetParameters(movementMode, foot.footTransform.position + rayOffset);
                    PlayMovementAudio(footstepAudioObject, foot.footTransform, parameters);
                    PlayMovementAudio(clothAudioObject, referenceHeight, parameters);
                    StartFootCooldown(foot);
                }
            }
        }

        private FootstepHeightThresholds GetFootstepHeightThresholds(MovementMode movementMode)
        {
            FootstepHeightThresholds heightThresholds = default;

            switch (movementMode)
            {
                case MovementMode.Walk:
                    heightThresholds = walkHeightThresholds;
                    break;
                case MovementMode.Run:
                    heightThresholds = runHeightThresholds;
                    break;
                case MovementMode.Crouch:
                    heightThresholds = crouchHeightThresholds;
                    break;
            }

            return heightThresholds; 
        }

        private ParamRef[] GetParameters(MovementMode movementMode, Vector3 rayOrigin)
        {
            SurfaceInfo surfaceInfo = default;

            if (SurfaceChecker.Instance != null)
            {
                if (SurfaceChecker.Instance.TryGetSurfaceType(rayOrigin, Vector3.down, surfaceCheckLayerMask, rayLength, waterDepthThresholds, out surfaceInfo))
                {
#if UNITY_EDITOR
                    if (debugDrawRaycast)
                    {
                        Debug.DrawLine(rayOrigin, surfaceInfo.position, Color.red, 5.0f);
                    }
#endif
                }
            }
            else
            {
                Debug.LogWarning("SurfaceChecker singleton instance is missing.");
            }

            var parameters = new ParamRef[]
            {
                new ParamRef{ Name = Parameters.ThirdPersonMovementParameter, Value = (float)movementMode },
                new ParamRef{ Name = Parameters.SurfaceParameter, Value = (float)surfaceInfo.surfaceType },
                new ParamRef{ Name = Parameters.WaterDepthParameter, Value = (float)surfaceInfo.waterDepth }
            };

            return parameters;
        }

        private void PlayMovementAudio(AudioObject audioObject, Transform followTarget, ParamRef[] parameters)
        {
            if (audioObject == null)
                return;

            audioObject.FollowTarget = followTarget;
            audioObject.Control(ControlAction.Start, parameters);

#if UNITY_EDITOR

            if (debugPrintTriggered)
            {
                var message = string.Empty;

                foreach (var parameter in parameters)
                {
                    if (parameter.Name == Parameters.ThirdPersonMovementParameter)
                    {
                        message += Parameters.ThirdPersonMovementParameter + ": " + (Enum.GetName(typeof(MovementMode), (int)parameter.Value)) + ", ";
                    }
                    else if (parameter.Name == Parameters.SurfaceParameter)
                    {
                        message += Parameters.SurfaceParameter + ": " + (Enum.GetName(typeof(SurfaceType), (int)parameter.Value)) + ", ";
                    }
                    else if (parameter.Name == Parameters.WaterDepthParameter)
                    {
                        message += Parameters.WaterDepthParameter + ": " + (Enum.GetName(typeof(WaterDepth), (int)parameter.Value)) + ", ";
                    }
                }

                message += "AudioObject: " + audioObject.gameObject.name + ", " + "Follow target: " + followTarget.name;
                Debug.Log(message);
            }
#endif
        }

        private void StartFootCooldown(Foot foot)
        {
            if (foot == null || foot.isCoolingDown)
                return;

            cooldowns.Add(StartCoroutine(FootCoolingDown(foot)));
        }

        private IEnumerator FootCoolingDown(Foot foot)
        {
            foot.isCoolingDown = true;
            yield return new WaitForSeconds(footCooldown);

            if (foot != null)
            {
                foot.isCoolingDown = false;
            }
        }

        private void StopAllFootCooldowns()
        {
            RemoveNullCooldowns();

            foreach (var cooldown in cooldowns)
            {
                StopCoroutine(cooldown);
            }

            foreach (var foot in feet)
            {
                if (foot != null)
                {
                    foot.isCoolingDown = false;
                }
            }
        }

        private void StopCurrentCooldownsAndSetAllFeetToCooldown()
        {
            StopAllFootCooldowns();

            foreach (var foot in feet)
            {
                StartFootCooldown(foot);
            }
        }

        private void RemoveNullCooldowns()
        {
            for (int i = cooldowns.Count - 1; i >= 0; i--)
            {
                var cooldown = cooldowns[i];

                if (cooldown == null)
                {
                    cooldowns.RemoveAt(i);
                }
            }
        }

        private void ResetAllRaisedFeet()
        {
            foreach (var foot in feet)
            {
                if (foot != null)
                {
                    foot.isRaised = false;
                    foot.debugRelativeHeight = 0;
                }
            }
        }

        private void ResetJumpingAndLanding()
        {
            justJumped = false;
            justLanded = false;
        }

        protected float GetDistanceFromBlendTreeOrigo(float y, float z)
        {
            return Mathf.Sqrt(((Mathf.Pow(y, 2)) + ((Mathf.Pow(z, 2)))));
        }

        public void EventReceived(EventArguments eventArgs)
        {
            if (eventArgs.eventTag == null)
                return;

            if (eventArgs is AnimatorStateEventArguments)
            {
                var args = (AnimatorStateEventArguments)eventArgs;

                if (args.animator == null || args.animator != this.animator)
                    return;

                if (args.eventTag == jumpEventTag)
                {
                    justJumped = true;
                }

                if (args.eventTag == landEventTag)
                {
                    justLanded = true;
                }
            }
        }
    }
}