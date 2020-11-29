// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using UnityEngine;
using UnityEngine.Playables;

namespace AudioTools
{
    public class AudioTimelineEventAsset : PlayableAsset
    {
        public AudioObjectTag audioObjectTag;
        public TriggeringAction triggeringAction = TriggeringAction.StartSound;
        public ExposedReference<Transform> optionalFollowTransform;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AudioTimelineEventBehaviour>.Create(graph);

            var audioEventBehaviour = playable.GetBehaviour();
            audioEventBehaviour.audioObjectTag = audioObjectTag;
            audioEventBehaviour.triggeringAction = triggeringAction;
            audioEventBehaviour.followTransform = optionalFollowTransform.Resolve(graph.GetResolver());

            return playable;
        }
    }
}