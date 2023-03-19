// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FMODUnityTools
{
    [CustomEditor(typeof(SpatialAudioManager))]
    public class SpatialAudioManagerEditor : Editor
    {
        //Change this to your project specific path.
        private const string DebugMaterialPath = "Assets/Scripts/FMOD-Audio-Tools/FMOD-Unity-Tools/AudioTools/Assets/Materials/DebugTriggerRed.mat";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var targetScript = target as SpatialAudioManager;
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));

            if (GUILayout.Button("Add Spatial Audio Room"))
            {
                var newRoomGameObj = new GameObject();
                newRoomGameObj.name = "SpatialAudioRoom";
                newRoomGameObj.transform.SetParent(targetScript.transform);

                var roomComponent = newRoomGameObj.AddComponent<SpatialAudioRoom>();

                SpatialAudioRoom[] roomsCopy = new SpatialAudioRoom[targetScript.spatialAudioRooms.Length + 1];
                targetScript.spatialAudioRooms.CopyTo(roomsCopy, 0);
                roomsCopy[roomsCopy.Length - 1] = roomComponent;
                targetScript.spatialAudioRooms = roomsCopy;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button("Add Spatial Audio Portal"))
            {
                var portalGameObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                portalGameObj.name = "SpatialAudioPortal";
                portalGameObj.transform.SetParent(targetScript.transform);
                portalGameObj.transform.position = new Vector3(0, 0.5f, 0);

                var meshCollider = portalGameObj.GetComponent<MeshCollider>();

                if (meshCollider != null)
                    DestroyImmediate(meshCollider);

                var boxCollider = portalGameObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1.0f, 1.0f, 0.0f);
                boxCollider.isTrigger = true;
                boxCollider.hideFlags = HideFlags.NotEditable;

                var portalComponent = portalGameObj.AddComponent<SpatialAudioPortal>();
                portalComponent.portalCollider = boxCollider;

                var material = (Material)AssetDatabase.LoadAssetAtPath(DebugMaterialPath, typeof(Material));

                if (material != null)
                {
                    var meshRenderer = portalGameObj.GetComponent<MeshRenderer>();
                    meshRenderer.material = material;
                    portalComponent.meshRenderer = meshRenderer;
                }

                SpatialAudioPortal[] portalsCopy = new SpatialAudioPortal[targetScript.spatialAudioPortals.Length + 1];
                targetScript.spatialAudioPortals.CopyTo(portalsCopy, 0);
                portalsCopy[portalsCopy.Length - 1] = portalComponent;
                targetScript.spatialAudioPortals = portalsCopy;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button("Clear & Retrieve All"))
            {
                var portals = targetScript.GetComponentsInChildren<SpatialAudioPortal>();
                targetScript.spatialAudioPortals = portals;
                var rooms = targetScript.GetComponentsInChildren<SpatialAudioRoom>();
                targetScript.spatialAudioRooms = rooms;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            if (GUILayout.Button("Room Network Validity Test"))
            {
                TestGridValidity(targetScript.spatialAudioRooms);
            }

            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        /* Check that each room is reachable from every other room. Note that this does not enforce 
         * that for each connected room pair the connection has been set in both room GameObjects. */
        private void TestGridValidity(SpatialAudioRoom[] allRooms)
        {
            for (int i = 0; i < allRooms.Length; i++)
            {
                for (int j = 0; j < allRooms.Length; j++)
                {
                    if (allRooms[i] == allRooms[j])
                        continue;
                    else
                    {
                        bool isConnected = IsRoomPairConnected(allRooms[i], allRooms[j]);

                        if (isConnected)
                            Debug.Log("Route from '" + allRooms[i].gameObject.name + " to " + allRooms[j].gameObject.name + "' is valid!");
                        else
                        {
                            Debug.LogError("Route from '" + allRooms[i].gameObject.name + " to " + allRooms[j].gameObject.name + "' does not exist!");
                        }
                    }
                }
            }
        }

        private bool IsRoomPairConnected(SpatialAudioRoom startingRoom, SpatialAudioRoom targetRoom)
        {
            var visitedRooms = new List<SpatialAudioRoom>();
            var startingRooms = new List<SpatialAudioRoom>();
            startingRooms.Add(startingRoom);
            visitedRooms.Add(startingRoom);

            for (int i = 0; i < startingRooms.Count; i++)
            {
                for (int j = 0; j < startingRooms[i].roomConnections.Count; j++)
                {
                    var roomConnection = startingRooms[i].roomConnections[j];

                    if (roomConnection.connectedRoom != null && roomConnection.connectingPortals != null && !visitedRooms.Contains(roomConnection.connectedRoom))
                    {
                        bool hasValidConnectingPortal = false;

                        for (int k = 0; k < roomConnection.connectingPortals.Length; k++)
                        {
                            var portal = roomConnection.connectingPortals[k];

                            if (portal != null)
                            {
                                hasValidConnectingPortal = true;
                            }
                        }

                        if (!hasValidConnectingPortal)
                        {
                            Debug.LogError("No valid connecting portal exists for the connection between rooms '" + startingRooms[i].gameObject.name +
                                           " and " + roomConnection.connectedRoom.gameObject.name + "'.");
                        }

                        if (roomConnection.connectedRoom == targetRoom && hasValidConnectingPortal)
                        {
                            return true;
                        }
                        else if (hasValidConnectingPortal)
                        {
                            startingRooms.Add(roomConnection.connectedRoom);
                            visitedRooms.Add(roomConnection.connectedRoom);
                        }
                    }
                }
            }

            return false;
        }
    }
}