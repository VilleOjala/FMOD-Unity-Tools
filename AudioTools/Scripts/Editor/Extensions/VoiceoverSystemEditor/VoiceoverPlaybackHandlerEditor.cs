// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace FMODUnityTools
{
    [CustomEditor(typeof(VoiceoverPlaybackHandler))]
    public class VoiceoverPlaybackHandlerEditor : Editor
    {
        SerializedProperty fixedRoom;

        void OnEnable()
        {
            fixedRoom = serializedObject.FindProperty("fixedRoom");  
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as VoiceoverPlaybackHandler;
            serializedObject.Update();

            if (targetScript.spatialAudioRoomAware)
            {
                EditorGUILayout.PropertyField(fixedRoom);
            }

            serializedObject.ApplyModifiedProperties();            
        }
    }
}