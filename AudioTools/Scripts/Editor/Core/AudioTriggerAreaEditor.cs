// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace FMODUnityTools
{
    [CustomEditor(typeof(AudioTriggerArea))]
    [CanEditMultipleObjects]
    public class AudioTriggerAreaEditor : Editor
    {
        //Change these to your project specific paths
        private const string DebugMaterialPath = "Assets/Scripts/FMOD-Audio-Tools/FMOD-Unity-Tools/AudioTools/Assets/Materials/DebugTriggerGreen.mat";
        private const string WedgeMeshPath = "Assets/Scripts/FMOD-Audio-Tools/FMOD-Unity-Tools/AudioTools/Assets/Meshes/Wedge.mesh";

        private bool toggleState = true;

        SerializedProperty layerMask;

        private void OnEnable()
        {
            layerMask = serializedObject.FindProperty("layerMask");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as AudioTriggerArea;
            serializedObject.Update();

            var triggererType = targetScript.Triggerer;

            if (triggererType == AudioTriggerArea.TriggererType.LayerMask)
            {
                EditorGUILayout.PropertyField(layerMask);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Box Collider"))
            {
                CreatePrimitiveCollider(PrimitiveType.Cube);
            }

            if (GUILayout.Button("Add Wedge Collider"))
            {
                CreateWedgeCollider();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Sphere Collider"))
            {
                CreatePrimitiveCollider(PrimitiveType.Sphere);
            }

            if (GUILayout.Button("Add Cylinder Collider"))
            {
                CreatePrimitiveCollider(PrimitiveType.Cylinder);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Toggle Debug Colors On/Off"))
            {
                toggleState = !toggleState;
                var meshRenderers = targetScript.gameObject.GetComponentsInChildren<MeshRenderer>();

                foreach (var meshRenderer in meshRenderers)
                {
                    meshRenderer.enabled = toggleState;
                }
            }

            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        private void CreatePrimitiveCollider(PrimitiveType primitiveType)
        {
            var targetScript = target as AudioTriggerArea;
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.transform.position = new Vector3(0, 0, 0);
            primitive.transform.parent = targetScript.transform;

            var collider = primitive.GetComponent<Collider>();

            if (collider != null)
                collider.isTrigger = true;

            var material = (Material)AssetDatabase.LoadAssetAtPath(DebugMaterialPath, typeof(Material));

            if (material != null)
            {
                var meshRenderer = primitive.GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    meshRenderer.material = material;
                }
            }
        }

        private void CreateWedgeCollider()
        {
            var wedgeMesh = (Mesh)AssetDatabase.LoadAssetAtPath(WedgeMeshPath, typeof(Mesh));

            if (wedgeMesh == null)
            {
                Debug.LogError("The wedge mesh could not be found at the path: " + WedgeMeshPath);
            }
            else
            {
                var targetScript = target as AudioTriggerArea;
                var wedge = new GameObject();
                wedge.transform.position = new Vector3(0, 0, 0);
                wedge.name = "Wedge";
                wedge.transform.SetParent(targetScript.transform);
                var meshRenderer = wedge.AddComponent<MeshRenderer>();
                var meshFilter = wedge.AddComponent<MeshFilter>();
                var meshCollider = wedge.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.isTrigger = true;
                meshCollider.sharedMesh = wedgeMesh;
                meshFilter.sharedMesh = wedgeMesh;

                var material = (Material)AssetDatabase.LoadAssetAtPath(DebugMaterialPath, typeof(Material));

                if (material != null)
                {
                    meshRenderer.sharedMaterial = material;
                }
            }
        }
    }
}