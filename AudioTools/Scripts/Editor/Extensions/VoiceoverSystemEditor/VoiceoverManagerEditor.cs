// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace FMODUnityTools
{
    [CustomEditor(typeof(VoiceoverManager))]
    public class VoiceoverManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as VoiceoverManager;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Voiceover Playback Handler"))
            {
                var newGameObj = new GameObject("VoiceoverPlaybackHandler");
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