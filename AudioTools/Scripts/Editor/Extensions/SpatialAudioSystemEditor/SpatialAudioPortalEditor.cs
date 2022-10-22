// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEditor;

namespace FMODUnityTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpatialAudioPortal))]
    public class SpatialAudioPortalEditor : Editor
    {
        SerializedProperty openEnvelope;
        SerializedProperty closeEnvelope;
        SerializedProperty openFadeTime;
        SerializedProperty closeFadeTime;
        SerializedProperty initialState;
        SerializedProperty traversalCost;

        void OnEnable()
        {
            openEnvelope = serializedObject.FindProperty("openEnvelope");
            closeEnvelope = serializedObject.FindProperty("closeEnvelope");
            openFadeTime = serializedObject.FindProperty("openFadeTime");
            closeFadeTime = serializedObject.FindProperty("closeFadeTime");
            initialState = serializedObject.FindProperty("initialState");
            traversalCost = serializedObject.FindProperty("traversalCost");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as SpatialAudioPortal;
            serializedObject.Update();
            EditorGUILayout.PropertyField(initialState);
            EditorGUILayout.PropertyField(openEnvelope);
            EditorGUILayout.PropertyField(closeEnvelope);
            EditorGUILayout.PropertyField(openFadeTime);
            EditorGUILayout.PropertyField(closeFadeTime);
            EditorGUILayout.PropertyField(traversalCost);
            string debug = string.Copy(targetScript.debugPortalStatus);
            EditorGUILayout.LabelField(debug);
            serializedObject.ApplyModifiedProperties();
        }
    }
}