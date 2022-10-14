// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Toolsls

using FMODUnity;
using UnityEngine.Playables;

namespace FMODUnityTools
{
    public class TimelineEventBehaviour : PlayableBehaviour
    {
        public EventTag eventTag;
        public ControlAction controlAction;
        public ParamRef[] parameters;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (eventTag != null)
            {
                EventManager.PostEvent(new ControlActionEventArguments(eventTag, controlAction, parameters));
            }
        }
    }
}