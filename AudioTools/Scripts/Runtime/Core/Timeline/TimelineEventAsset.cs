// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEngine.Playables;
using FMODUnity;

namespace FMODUnityTools
{
    public class TimelineEventAsset : PlayableAsset
    {
        public EventTag eventTag;
        public ControlAction controlAction;
        public ParamRef[] parameters;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimelineEventBehaviour>.Create(graph);
            var audioEventBehaviour = playable.GetBehaviour();
            audioEventBehaviour.eventTag = eventTag;
            audioEventBehaviour.controlAction = controlAction;
            audioEventBehaviour.parameters = parameters;
            return playable;
        }
    }
}