// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(VoiceoverManager))]
    public class VoiceoverManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var targetScript = target as VoiceoverManager;

            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Voiceover Playback Handler"))
            {
                GameObject newGameObj = new GameObject("VoiceoverPlaybackHandler");
                newGameObj.transform.SetParent(targetScript.transform);
                var playbackHandler = newGameObj.AddComponent<VoiceoverPlaybackHandler>();
                targetScript.voiceoverPlaybackHandlers.Add(playbackHandler);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear & Retrieve All"))
            {
                targetScript.voiceoverPlaybackHandlers.Clear();

                var playbackHandlers = targetScript.GetComponentsInChildren<VoiceoverPlaybackHandler>();

                for (int i = 0; i < playbackHandlers.Length; i++)
                {
                    targetScript.voiceoverPlaybackHandlers.Add(playbackHandlers[i]);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}