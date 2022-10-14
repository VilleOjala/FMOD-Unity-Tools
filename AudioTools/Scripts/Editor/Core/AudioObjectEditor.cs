// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;
using System.IO;

namespace FMODUnityTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioObject))]
    public class AudioObjectEditor : Editor
    {
        SerializedProperty initialRoom;

        public void OnEnable()
        {
            initialRoom = serializedObject.FindProperty("initialRoom");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var audioObject = target as AudioObject;
            serializedObject.Update();

            if (audioObject.spatialAudioRoomAware)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(initialRoom, new GUIContent("Initial Room (optional)"));
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Path.GetFileNameWithoutExtension(audioObject.currentAttenuationToDraw.Path), 
                            GUILayout.Width(EditorGUIUtility.labelWidth));

            if (GUILayout.Button("Update/change the event for attenuation gizmo"))
            {
                audioObject.IncrementClickCounter();
            }

            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }
        
        public void OnSceneGUI()
        {
            var audioObject = target as AudioObject;
            var eventReference = audioObject.currentAttenuationToDraw;

            if (eventReference.IsNull)
                return;

            Vector3 gizmoPosition;

            if (audioObject.FollowTarget != null)
            {
                gizmoPosition = audioObject.FollowTarget.position;
            }
            else
            {
                gizmoPosition = audioObject.transform.position;
            }

            var editorEvent = FMODUnity.EventManager.EventFromPath(eventReference.Path);
            if (editorEvent != null && editorEvent.Is3D)
            {
                float minDistance = editorEvent.MinDistance;
                float maxDistance = editorEvent.MaxDistance;
                Handles.RadiusHandle(Quaternion.identity, gizmoPosition, minDistance); 
                Handles.RadiusHandle(Quaternion.identity, gizmoPosition, maxDistance); 
            }
        }
    }
}