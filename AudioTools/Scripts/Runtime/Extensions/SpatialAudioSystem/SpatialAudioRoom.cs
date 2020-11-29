// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    // Only instantiate Spatial Audio Rooms with the 'Add Spatial Audio Room" -button of the Spatial Audio Manager.

    [AddComponentMenu("Audio Tools/Extensions/Spatial Audio System/Spatial Audio Room")]
    public class SpatialAudioRoom : MonoBehaviour
    {
        private SpatialAudioManager spatialAudioManager = null;

        [Tooltip("Optionally give the room a unique name for more informative debug messages.")]
        public string roomName;

        public AudioTriggerArea area;

        [HideInInspector]
        public Collider[] colliders;

        public List<RoomConnection> roomConnections = new List<RoomConnection>();

        [System.Serializable]
        public class RoomConnection
        {
            public SpatialAudioRoom connectedRoom;
            public SpatialAudioPortal[] connectingPortals;
        }

        private bool roomAlreadyInitalized = false;

        public bool InitializeRoom(SpatialAudioManager caller)
        {
            if (caller == null)
            {
                string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                Debug.LogError("Initialization of Spatial Audio Room '" + offendingRoom + "' failed. Spatial Audio Manager was null.");
                return false;
            }
            else
            {
                spatialAudioManager = caller;
            }

            if (roomAlreadyInitalized)
            {
                string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                Debug.LogError("Spatial Audio Room '" + offendingRoom + "' has already been initialized. Check for duplicates.");
                return false;
            }

            if (area == null)
            {
                string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                Debug.LogError("Audio Trigger Area is null for Spatial Audio Room '" + offendingRoom + "'.");
                return false;
            }
            else
            {
                if (area.requireTag != RequiredTags.Player)
                {
                    Debug.LogError("Audio Trigger Areas associated with Spatial Audio Rooms need to have 'Player' as the required tag.");
                    return false;                  
                }

                colliders = area.GetColliders();

                if (colliders == null || colliders.Length < 1)
                {
                    string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                    Debug.LogError("Spatial Audio Room '" + offendingRoom + "' has no valid trigger colliders.");
                    return false;
                }               
            }

            for (int i = 0; i < roomConnections.Count; i++)
            {
                RoomConnection roomConnection = roomConnections[i];

                if (roomConnection == null)
                {
                    string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                    Debug.LogError("Room connection is null for Spatial Audio Room '" + offendingRoom + "'.");
                    return false;
                }

                if (roomConnection.connectedRoom == null)
                {
                    string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                    Debug.LogError("Room connection's connected room is null for Spatial Audio Room '" + offendingRoom + "'.");
                    return false;
                }

                if (roomConnection.connectingPortals == null)
                {
                    string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                    Debug.LogError("Room connection's connecting portals is null for Spatial Audio Room '" + offendingRoom + "'.");
                    return false;
                }

                for (int j = 0; j < roomConnection.connectingPortals.Length; j++)
                {
                    if (roomConnection.connectingPortals[j] == null)
                    {
                        string offendingRoom = (string.IsNullOrEmpty(roomName) ? gameObject.name : roomName);
                        Debug.LogError("Room connection's connecting portal is null for Spatial Audio Room '" + offendingRoom + "'.");
                        return false;
                    }
                }
            }

            // Sanity checks completed. Next, we will set programatically set this room be one of the two rooms that any portal found in room connections connects.
            // <- This reduces the need for manual set-up in editor. 
            // <- In addition, the availability of "other-way-around" references simplifies the propagation cost code.
            // In a proper spatial audio geometry setup, each portal can only connect two rooms. 
            // <- An error is thrown if more than two rooms try to assign themselves to a single portal.

            for (int i = 0; i < roomConnections.Count; i++)
            {
                var roomConnection = roomConnections[i];

                for (int j = 0; j < roomConnection.connectingPortals.Length; j++)
                {
                    roomConnection.connectingPortals[j].SetConnectedRoom(this);
                }
            }
 
            area.OnTriggerAreaEvent += OnPlayerEnterAndExit;

            return true;
        }
        public void OnPlayerEnterAndExit(object sender, AudioTriggerAreaEventArgs eventArgs)
        {
            if (eventArgs.triggerEventType == AudioTriggerAreaEventArgs.TriggerEventType.TriggerEnter)
            {
                if (spatialAudioManager != null)
                {
                    spatialAudioManager.AddCurrentPlayerRoom(this);
                }
            }

            if (eventArgs.triggerEventType == AudioTriggerAreaEventArgs.TriggerEventType.TriggerExit)
            {
                if (spatialAudioManager != null)
                {
                    spatialAudioManager.RemoveCurrentPlayerRoom(this);
                }
            }
        }

        void OnDestroy()
        {
            CleanUp();   
        }

        void CleanUp()
        {
            area.OnTriggerAreaEvent -= OnPlayerEnterAndExit;
        }

        void OnValidate()
        {
            if (!string.IsNullOrEmpty(roomName))
                gameObject.name = roomName;
            else
                gameObject.name = "SpatialAudioRoom";
        }
    }  
}