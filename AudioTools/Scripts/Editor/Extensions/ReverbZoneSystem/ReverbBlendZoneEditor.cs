// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using UnityEngine;
using UnityEditor;

namespace FMODUnityTools
{
    [CustomEditor(typeof(ReverbBlendZone))]
    public class ReverbBlendZoneEditor : Editor
    {
        //Change this to your project specific path
        private const string DebugMaterialPath = "Assets/Scripts/FMOD-Audio-Tools/FMOD-Unity-Tools/AudioTools/Assets/Materials/DebugTriggerRed.mat";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as ReverbBlendZone;

            if (GUILayout.Button("Create the Blend Area"))
            {
                if (targetScript.blendArea == null)
                {
                    CreateBlendArea();
                }
                else
                {
                    EditorUtility.DisplayDialog(string.Empty, "Blend Area already created.", "Ok");
                }
            }
        }

        public void CreateBlendArea()
        {
            var targetScript = target as ReverbBlendZone;
            var reverBlendZoneGameObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reverBlendZoneGameObj.transform.parent = targetScript.transform;
            targetScript.blendArea = reverBlendZoneGameObj;
            var zoneBoxCollider = reverBlendZoneGameObj.GetComponent<BoxCollider>();

            if (zoneBoxCollider != null)
            {
                zoneBoxCollider.isTrigger = true;
                zoneBoxCollider.hideFlags = HideFlags.NotEditable;
            }

            targetScript.blendAreaCollider = zoneBoxCollider;
            var zoneMeshRenderer = reverBlendZoneGameObj.GetComponent<MeshRenderer>();
            var material = (Material)AssetDatabase.LoadAssetAtPath(DebugMaterialPath, typeof(Material));

            if (zoneMeshRenderer != null)
            {
                if (material != null)
                    zoneMeshRenderer.material = material;
            }

            // Reset any possible transform position, rotation and scale offsets.
            var copyTransformPosition = reverBlendZoneGameObj.transform.position;
            copyTransformPosition.x = 0.0f;
            copyTransformPosition.y = 0.0f;
            copyTransformPosition.z = 0.0f;
            reverBlendZoneGameObj.transform.position = copyTransformPosition;

            var copyTransformRotation = reverBlendZoneGameObj.transform.rotation;
            copyTransformRotation.x = 0.0f;
            copyTransformRotation.y = 0.0f;
            copyTransformRotation.z = 0.0f;
            copyTransformRotation.w = 0.0f;
            reverBlendZoneGameObj.transform.rotation = copyTransformRotation;

            var copyTransformScale = reverBlendZoneGameObj.transform.localScale;

            copyTransformScale.x = 1.0f;
            copyTransformScale.y = 1.0f;
            copyTransformScale.z = 1.0f;
            reverBlendZoneGameObj.transform.localScale = copyTransformScale;

            // Create zone back wall

            targetScript.backWall = new GameObject();
            targetScript.backWall.name = "BackWall";
            targetScript.backWall.transform.SetParent(reverBlendZoneGameObj.transform);
            targetScript.backWall.hideFlags = HideFlags.NotEditable;

            var copyBackWallTransformPosition = targetScript.backWall.transform.position;
            copyBackWallTransformPosition.x = 0.0f;
            copyBackWallTransformPosition.y = 0.0f;
            copyBackWallTransformPosition.z = -0.5f;
            targetScript.backWall.transform.position = copyBackWallTransformPosition;

            var copyBackWallTransformScale = targetScript.backWall.transform.localScale;

            copyBackWallTransformScale.x = 1.0f;
            copyBackWallTransformScale.y = 1.0f;
            copyBackWallTransformScale.z = 0.0f;
            targetScript.backWall.transform.localScale = copyBackWallTransformScale;

            targetScript.backWallCollider = targetScript.backWall.AddComponent<BoxCollider>();
            targetScript.backWallCollider.isTrigger = true;

            // Create zone front wall

            targetScript.frontWall = new GameObject();
            targetScript.frontWall.name = "FrontWall";
            targetScript.frontWall.transform.SetParent(reverBlendZoneGameObj.transform);
            targetScript.frontWall.hideFlags = HideFlags.NotEditable;

            var copyFrontWallTransformPosition = targetScript.frontWall.transform.position;
            copyFrontWallTransformPosition.x = 0.0f;
            copyFrontWallTransformPosition.y = 0.0f;
            copyFrontWallTransformPosition.z = 0.5f;
            targetScript.frontWall.transform.position = copyFrontWallTransformPosition;

            var copyFrontWallTransformScale = targetScript.frontWall.transform.localScale;

            copyFrontWallTransformScale.x = 1.0f;
            copyFrontWallTransformScale.y = 1.0f;
            copyFrontWallTransformScale.z = 0.0f;
            targetScript.frontWall.transform.localScale = copyFrontWallTransformScale;

            targetScript.frontWallCollider = targetScript.frontWall.AddComponent<BoxCollider>();
            targetScript.frontWallCollider.isTrigger = true;

            // Create zone left wall

            targetScript.leftWall = new GameObject();
            targetScript.leftWall.name = "LeftWall";
            targetScript.leftWall.transform.SetParent(reverBlendZoneGameObj.transform);
            targetScript.leftWall.hideFlags = HideFlags.NotEditable;

            var copyLeftWallTransformPosition = targetScript.leftWall.transform.position;
            copyLeftWallTransformPosition.x = -0.5f;
            copyLeftWallTransformPosition.y = 0.0f;
            copyLeftWallTransformPosition.z = 0.0f;
            targetScript.leftWall.transform.position = copyLeftWallTransformPosition;

            var copyLeftWallTransformScale = targetScript.leftWall.transform.localScale;

            copyLeftWallTransformScale.x = 0.0f;
            copyLeftWallTransformScale.y = 1.0f;
            copyLeftWallTransformScale.z = 1.0f;
            targetScript.leftWall.transform.localScale = copyLeftWallTransformScale;

            targetScript.leftWallCollider = targetScript.leftWall.AddComponent<BoxCollider>();
            targetScript.leftWallCollider.isTrigger = true;

            // Create zone right wall

            targetScript.rightWall = new GameObject();
            targetScript.rightWall.name = "RightWall";
            targetScript.rightWall.transform.SetParent(reverBlendZoneGameObj.transform);
            targetScript.rightWall.hideFlags = HideFlags.NotEditable;

            var copyRightWallTransformPosition = targetScript.rightWall.transform.position;
            copyRightWallTransformPosition.x = 0.5f;
            copyRightWallTransformPosition.y = 0.0f;
            copyRightWallTransformPosition.z = 0.0f;
            targetScript.rightWall.transform.position = copyRightWallTransformPosition;

            var copyRightWallTransformScale = targetScript.rightWall.transform.localScale;

            copyRightWallTransformScale.x = 0.0f;
            copyRightWallTransformScale.y = 1.0f;
            copyRightWallTransformScale.z = 1.0f;
            targetScript.rightWall.transform.localScale = copyRightWallTransformScale;

            targetScript.rightWallCollider = targetScript.rightWall.AddComponent<BoxCollider>();
            targetScript.rightWallCollider.isTrigger = true;

            // Create zone down wall

            targetScript.downWall = new GameObject();
            targetScript.downWall.name = "DownWall";
            targetScript.downWall.transform.SetParent(reverBlendZoneGameObj.transform);
            targetScript.downWall.hideFlags = HideFlags.NotEditable;

            var copyDownWallTransformPosition = targetScript.downWall.transform.position;
            copyDownWallTransformPosition.x = 0.0f;
            copyDownWallTransformPosition.y = -0.5f;
            copyDownWallTransformPosition.z = 0.0f;
            targetScript.downWall.transform.position = copyDownWallTransformPosition;

            var copyDownWallTransformScale = targetScript.downWall.transform.localScale;

            copyDownWallTransformScale.x = 1.0f;
            copyDownWallTransformScale.y = 0.0f;
            copyDownWallTransformScale.z = 1.0f;
            targetScript.downWall.transform.localScale = copyDownWallTransformScale;

            targetScript.downWallCollider = targetScript.downWall.AddComponent<BoxCollider>();
            targetScript.downWallCollider.isTrigger = true;

            // Create zone up wall

            targetScript.upWall = new GameObject();
            targetScript.upWall.name = "UpWall";
            targetScript.upWall.transform.SetParent(reverBlendZoneGameObj.transform);
            targetScript.upWall.hideFlags = HideFlags.NotEditable;

            var copyUpWallTransformPosition = targetScript.upWall.transform.position;
            copyUpWallTransformPosition.x = 0.0f;
            copyUpWallTransformPosition.y = 0.5f;
            copyUpWallTransformPosition.z = 0.0f;
            targetScript.upWall.transform.position = copyUpWallTransformPosition;

            var copyUpWallTransformScale = targetScript.upWall.transform.localScale;

            copyUpWallTransformScale.x = 1.0f;
            copyUpWallTransformScale.y = 0.0f;
            copyUpWallTransformScale.z = 1.0f;
            targetScript.upWall.transform.localScale = copyUpWallTransformScale;

            targetScript.upWallCollider = targetScript.upWall.AddComponent<BoxCollider>();
            targetScript.upWallCollider.isTrigger = true;
        }
    }
}