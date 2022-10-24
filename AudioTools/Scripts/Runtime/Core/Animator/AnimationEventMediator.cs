// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using FMODUnity;
using UnityEngine;

namespace FMODUnityTools
{
    [RequireComponent(typeof(Animator)), AddComponentMenu("FMOD Unity Tools/Core/Animation Event Mediator")]
    public class AnimationEventMediator : MonoBehaviour
    {
        public ControlAction controlAction;
        public ParamRef[] parameters;

        // Use this method name when assigning events to animation keyframes 
        // and pass an EventTag as an argument to target specific AudioObjects. 
        public void OnAnimationEvent(Object unityObject)
        {
            if (unityObject == null)
                return;

            if (unityObject is EventTag)
            {
                var eventTag = (EventTag)unityObject;
                EventManager.PostEvent(new ControlActionEventArguments(eventTag, controlAction, parameters));
            }
        }
    }
}