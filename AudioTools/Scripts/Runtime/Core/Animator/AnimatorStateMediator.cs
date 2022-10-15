// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using FMODUnity;
using UnityEngine;

namespace FMODUnityTools
{
    public class AnimatorStateMediator : StateMachineBehaviour
    {
        public enum AnimatorStateEvent
        {
            None,
            OnStateEnter,
            OnStateUpdate,
            OnStateExit
        }

        public AnimatorStateEvent eventType;
        public EventTag eventTag;
        public ControlAction controlAction;
        public ParamRef[] parameters;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (eventType == AnimatorStateEvent.OnStateEnter)
            {
                EventManager.PostEvent(new AnimatorStateEventArguments(animator, stateInfo, layerIndex, eventTag, controlAction, parameters));
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (eventType == AnimatorStateEvent.OnStateUpdate)
            {
                EventManager.PostEvent(new AnimatorStateEventArguments(animator, stateInfo, layerIndex, eventTag, controlAction, parameters));
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (eventType == AnimatorStateEvent.OnStateExit)
            {
                EventManager.PostEvent(new AnimatorStateEventArguments(animator, stateInfo, layerIndex, eventTag, controlAction, parameters));
            }
        }
    }
}