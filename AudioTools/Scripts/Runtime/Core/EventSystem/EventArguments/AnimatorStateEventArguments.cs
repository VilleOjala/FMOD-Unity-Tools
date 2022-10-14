// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;

namespace FMODUnityTools
{
    public class AnimatorStateEventArguments : EventArguments
    {
        public Animator animator;
        public AnimatorStateInfo stateInfo;
        public int layerIndex;

        public AnimatorStateEventArguments(EventTag eventTag, Animator animator, AnimatorStateInfo stateInfo, int layerIndex) : base(eventTag)
        {
            this.animator = animator;
            this.stateInfo = stateInfo;
            this.layerIndex = layerIndex;
        }
    }
}