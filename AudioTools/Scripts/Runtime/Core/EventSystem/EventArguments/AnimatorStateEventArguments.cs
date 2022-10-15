// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using FMODUnity;

namespace FMODUnityTools
{
    public class AnimatorStateEventArguments : ControlActionEventArguments
    {
        public Animator animator;
        public AnimatorStateInfo stateInfo;
        public int layerIndex;

        public AnimatorStateEventArguments(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, EventTag eventTag, ControlAction controlAction, ParamRef[] parameters)
               : base(eventTag, controlAction, parameters)
        {
            this.animator = animator;
            this.stateInfo = stateInfo;
            this.layerIndex = layerIndex;
        }
    }
}