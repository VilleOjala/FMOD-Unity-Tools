// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace AudioTools
{
    [CustomEditor(typeof(AudioTriggerArea))]
    [CanEditMultipleObjects]
    public class AudioTriggerAreaEditor : Editor
    {
        SerializedProperty customRequiredTag;
        SerializedProperty requireTag;

        private bool toggleState = true;

        void OnEnable()
        {
            customRequiredTag = serializedObject.FindProperty("customRequiredTag");
            requireTag = serializedObject.FindProperty("requireTag");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetScript = target as AudioTriggerArea;

            serializedObject.Update();

            if (requireTag.enumValueIndex == 3)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Custom Tag Name:", GUILayout.Width(EditorGUIUtility.labelWidth));
                customRequiredTag.stringValue = EditorGUILayout.TextArea(customRequiredTag.stringValue, 
                                                                         GUILayout.MaxHeight(22), GUILayout.MaxWidth(600));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Box Collider", GUILayout.MaxHeight(22), GUILayout.MaxWidth(600)))
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "TriggerBoxCollider";
                cube.transform.position = new Vector3(0, 0, 0); 
                cube.transform.parent = targetScript.transform;

                var boxCollider = cube.GetComponent<BoxCollider>();

                if (boxCollider != null)
                    boxCollider.isTrigger = true;

                var material = (Material)AssetDatabase.LoadAssetAtPath("Assets/AudioTools/Assets/Materials/DebugTriggerGreen.mat", typeof(Material));

                if (material != null)
                {
                    var meshRenderer = cube.GetComponent<MeshRenderer>();

                    if (meshRenderer != null)
                    {
                        meshRenderer.material = material;
                    }
                }

                // Check if a layer with the name "AudioToolsGeneral" has been created.
                // If found, automatically assign this layer to trigger collider game objects.
                int layerIndex = LayerMask.NameToLayer("AudioToolsGeneral");

                if (layerIndex > -1)
                {
                   cube.layer = layerIndex;
                }
            }

            if (GUILayout.Button("Add Wedge Collider", GUILayout.MaxHeight(22), GUILayout.MaxWidth(600)))
            {
                var wedgeMesh = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/AudioTools/Assets/Meshes/Wedge.mesh", typeof(Mesh));

                if (wedgeMesh == null)
                {
                    Debug.LogError("The wedge collider mesh could not be found at the path: 'Assets/AudioTools/Assets/Meshes/Wedge.mesh'");
                }
                else
                {
                    GameObject wedge = new GameObject();
                    wedge.transform.position = new Vector3(0, 0, 0); 
                    wedge.name = "TriggerWedgeCollider";
                    wedge.transform.SetParent(targetScript.transform);

                    var meshRenderer = wedge.AddComponent<MeshRenderer>();
                    var meshFilter = wedge.AddComponent<MeshFilter>();
                    var meshCollider = wedge.AddComponent<MeshCollider>();

                    meshCollider.convex = true;
                    meshCollider.isTrigger = true;
                    meshCollider.sharedMesh = wedgeMesh;

                    meshFilter.sharedMesh = wedgeMesh;

                    var material = (Material)AssetDatabase.LoadAssetAtPath("Assets/AudioTools/Assets/Materials/DebugTriggerGreen.mat", typeof(Material));

                    if (material == null)
                    {
                        Debug.LogError("The trigger collider material could not be found at the path: 'Assets/AudioTools/Assets/Materials/DebugTriggerGreen.mat'");
                    }
                    else
                    {
                        meshRenderer.sharedMaterial = material;
                    }

                    // Check if a layer with the name "AudioToolsGeneral" has been created.
                    // If found, automatically assign this layer to trigger collider game objects.
                    int layerIndex = LayerMask.NameToLayer("AudioToolsGeneral");

                    if (layerIndex > -1)
                    {
                        wedge.layer = layerIndex;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Sphere Collider", GUILayout.MaxHeight(22), GUILayout.MaxWidth(600)))
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = new Vector3(0, 0, 0); 
                sphere.name = "TriggerSphereCollider";
                sphere.transform.SetParent(targetScript.transform);

                var sphereCollider = sphere.GetComponent<SphereCollider>();

                if (sphereCollider != null)
                    sphereCollider.isTrigger = true;

                var material = (Material)AssetDatabase.LoadAssetAtPath("Assets/AudioTools/Assets/Materials/DebugTriggerGreen.mat", typeof(Material));

                if (material != null)
                {
                    var meshRenderer = sphere.GetComponent<MeshRenderer>();

                    if (meshRenderer != null)
                        meshRenderer.material = material;
                }

                // Check if a layer with the name "AudioToolsGeneral" has been created.
                // If found, automatically assign this layer to trigger collider game objects.
                int layerIndex = LayerMask.NameToLayer("AudioToolsGeneral");

                if (layerIndex > -1)
                {
                    sphere.layer = layerIndex;
                }
            }

            if (GUILayout.Button("Add Cylinder Collider", GUILayout.MaxHeight(22), GUILayout.MaxWidth(600)))
            {
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = "TriggerCylinderCollider";
                cylinder.transform.position = new Vector3(0, 0, 0); 
                cylinder.transform.parent = targetScript.transform;
            
                var capsuleCollider = cylinder.GetComponent<CapsuleCollider>();

                if (capsuleCollider != null)
                    capsuleCollider.isTrigger = true;

                var material = (Material)AssetDatabase.LoadAssetAtPath("Assets/AudioTools/Assets/Materials/DebugTriggerGreen.mat", typeof(Material));

                if (material != null)
                {
                    var meshRenderer = cylinder.GetComponent<MeshRenderer>();

                    if (meshRenderer != null)
                    {
                        meshRenderer.material = material;
                    }
                }

                // Check if a layer with the name "AudioToolsGeneral" has been created.
                // If found, automatically assign this layer to trigger collider game objects.
                int layerIndex = LayerMask.NameToLayer("AudioToolsGeneral");

                if (layerIndex > -1)
                {
                    cylinder.layer = layerIndex;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Debug Colors On/Off"))
            {
                // Kind of an ugly way of doing this (i.e. without references).

                var meshRenderers = targetScript.gameObject.GetComponentsInChildren<MeshRenderer>();

                if (toggleState == true)
                {
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        meshRenderers[i].enabled = false;
                    }

                    toggleState = false;
                }
                else
                {
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        meshRenderers[i].enabled = true;
                    }

                    toggleState = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}