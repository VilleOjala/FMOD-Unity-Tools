// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace FMODUnityTools
{
    [CustomEditor(typeof(VoiceoverPlaybackHandler))]
    public class VoiceoverPlaybackHandlerEditor : Editor
    {
        SerializedProperty initialRoom;

        void OnEnable()
        {
            initialRoom = serializedObject.FindProperty("initialRoom");  
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as VoiceoverPlaybackHandler;
            serializedObject.Update();

            if (targetScript.spatialAudioRoomAware)
            {
                EditorGUILayout.PropertyField(initialRoom);
            }

            serializedObject.ApplyModifiedProperties();            
        }
    }
}