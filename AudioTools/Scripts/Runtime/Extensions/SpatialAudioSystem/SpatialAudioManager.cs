// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using UnityEditor;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Spatial Audio System/Spatial Audio Manager")]
    public class SpatialAudioManager : MonoBehaviour
    {
        #region Declarations & Initializations

        // Uses the singleton pattern.
        public static SpatialAudioManager instance { get; private set; }
        
        private const string ObstructionParameter = "Obstruction"; // A local parameter with exactly this name needs to created inside the FMOD Studio project.
        private const int ObstructionMinValue = 0; // <- The local parameter min value needs to be set as '0' inside the FMOD Studio project. 
        private const int ObstructionMaxValue = 1; //  <- The local parameter max value needs to be set as '1' inside the FMOD Studio project.

        private const string PropagationCostParameter = "PropagationCost";
        private const int CostMinValue = 0; // <- The local parameter min value needs to be set as '0' inside the FMOD Studio. 
        private const int CostMaxValue = 1; //  <- The local parameter max value needs to be set as '1' inside the FMOD Studio.

        public ActiveModes activeModes;
        public DrawDebugLines drawDebugLines;

        [Tooltip("It is advisable to assign a new custom layer to portal game objects and set this layer mask to include only that layer. " +
                 "If a layer named 'Portal' exists, the Spatial Audio Manager sets portal game objects to that layer automatically")]
        public LayerMask portalLayerMask = (1 << 0);
        public LayerMask obstructionLayerMask = (1 << 0);
        public QueryTriggerInteraction obstructionQueryTriggers = QueryTriggerInteraction.Ignore;

        [Tooltip("Collider to ignore when checking for obstruction.")]
        public Collider ignoreSelfCollider;

        public bool requireObstructionTag = true;

        [Range(0.0f, 1.0f), Tooltip("The maximum amount of propagation cost at which obstruction check is still performed when the emitter and listener are in different rooms.")]
        public float obstructionCheckThreshold = 0.0f;

        [Tooltip("Controls the overall width of the raycast pattern used in the obstruction mode.")]
        [Range(0.0f, 10.0f)]
        public float obstructionRaycastSpread = 1.3f;

        [Tooltip("The distance at which the full amount of calculated diffraction or traversal cost will be applied to the total portal propagation cost.")]
        [Range(0.1f, 30.0f)]
        public float maxCostDistance = 8.0f;

        public SpatialAudioRoom[] spatialAudioRooms = new SpatialAudioRoom[0];
        public SpatialAudioPortal[] spatialAudioPortals = new SpatialAudioPortal[0];

        private AudioActorTag playerPosition = null;
        private Vector3 _playerPosition;
        private SpatialAudioNode playerNode = new SpatialAudioNode();

        private List<SpatialAudioRoom> currentPlayerRooms = new List<SpatialAudioRoom>();
        private SpatialAudioRoom currentPlayerRoom = null;

        private List<RoomAwareInstance> registeredInstances = new List<RoomAwareInstance>();

        private List<SpatialAudioRoom> validSpatialAudioRooms = new List<SpatialAudioRoom>();
       
        // When the level starts, each Spatial Audio Room will be set as a starting room and all the directly or indirectly reachable rooms from it are then searched for.
        //(<-This should encompass all the other rooms in the level, if the spatial audio geometry has been set corretly.) 
        // The connected rooms will be stored on a list as they are discovered.
        // <- This means that the rooms closer to the starting room will be higher up on the list.
        // When we know the previous room for a sound we can optimize the new room look-up by checking against the room connection list of the previous room. 
        // <- i.e. the sound is now most likely located either in the same room or in one of the rooms nearby, rather than on the other side of the map.
        //(<- unless, of course, your game includes teleporting or some other wild types of movement..)
        // First-time room look-up for a sound still requires testing against all of the rooms, unless a starting room has been manually provided when the sound is registered.
        // <- At least in the case of stationary sounds, it is a good practice to assign a starting room for a small performance boost.
        private Dictionary<SpatialAudioRoom, List<SpatialAudioRoom>> orderedConnections = new Dictionary<SpatialAudioRoom, List<SpatialAudioRoom>>();

        // During the propagation cost calculations having these relations already stored allows for faster sorting of raycast hit data.
        // <- i.e. not having to do a bunch of 'GetComponent' calls etc.
        private Dictionary<BoxCollider, SpatialAudioPortal> colliderToPortalData = new Dictionary<BoxCollider, SpatialAudioPortal>();

        // Used for a fast node lookup during the propagation cost calculations. 
        private Dictionary<SpatialAudioPortal, SpatialAudioNode> portalToNodeData = new Dictionary<SpatialAudioPortal, SpatialAudioNode>();

        // If the 'Require Obstruction Tag' -mode is active, the Spatial Audio Obstruction Tags populate this list with their associated colliders. 
        private HashSet<Collider> obstructingColliders = new HashSet<Collider>();

        // The maximum number of rooms that a sound route can pass through before it is automatically terminated. 
        private int checkLevelLimit = 8;

        private List<RoomAwareInstance> instancesWithKnownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesWithUnknownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> audibleInstances = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInPlayerRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInOtherRooms = new List<RoomAwareInstance>();
        private Dictionary<RoomAwareInstance, float> instanceToPlayerDistances = new Dictionary<RoomAwareInstance, float>();

        private bool managerInitialized = false;

        // When calculating the diffraction angle for a given portal, the angle will be set to zero if the magnitude of either direction vector is below this value.
        // This is done to prevent the bugs/edge cases in angle calculations when the player or an emitter is crossing a room boundary. 
        // <- Using very short direction vectors may cause sudden unwanted jumps/artefact in the calculated diffraction values.
        private float minimumVectorMagnitude = 0.05f;

        public float CheckInterval { get; private set; } = 0.05f;
        private float timer = 0.0f;

        [System.Flags]
        public enum ActiveModes
        {
            PropagationCost = 1,
            Obstruction = 2
        }

        [System.Flags]
        public enum DrawDebugLines
        {
            PropationCost = 1,
            Obstruction = 2
        }

        private class RoomAwareInstance
        {
            public FMOD.Studio.EventInstance eventInstance;
            public Transform attachedTransform;
            public Vector3 cachePosition;
            public float maxDistance;
            public SpatialAudioRoom spatialAudioRoom = null;
            public bool usesResonanceAudioSource = false;
        }

        private class SpatialAudioNode
        {
            public bool isInstance = false;
            public RoomAwareInstance roomAwareInstance;
            public SpatialAudioPortal nodePortal = null;
            public bool isPlayer = false;
        }

        private class SpatialAudioRoute
        {
            public List<SpatialAudioNode> routePoints = new List<SpatialAudioNode>();
            public HashSet<SpatialAudioRoom> visitedRooms = new HashSet<SpatialAudioRoom>();
            public SpatialAudioNode cacheLastNode = null;
            public float routeLength;
        }

        void Reset()
        {
            var otherManagers = FindObjectsOfType<SpatialAudioManager>();

            for (int i = 0; i < otherManagers.Length; i++)
            {
                if (otherManagers[i] != this)
                {
                    EditorUtility.DisplayDialog("Error", "Multiple Spatial Audio Managers detected. " +
                                                "Make sure there is just one instance of Spatial Audio Manager in any given active scene", "Ok");
                    DestroyImmediate(this);
                    return;
                }
            }

            gameObject.name = "SpatialAudioManager";

            // Check if a layer with the name "AudioToolsPortal" has been created.
            // If found, automatically assign the LayerMask of propagation cost -related raycasting to this layer.
            int portalLayer = LayerMask.NameToLayer("AudioToolsPortal");

            if(portalLayer > -1)
            {
                portalLayerMask = (1 << portalLayer);
            }

            // Reset possible transform offsets for the Spatial Audio Manager, since there will be trigger colliders areas in its children.
            var copyPosition = transform.position;
            copyPosition.x = 0.0f;
            copyPosition.y = 0.0f;
            copyPosition.z = 0.0f;
            transform.position = copyPosition;

            var copyScale = transform.localScale;
            copyScale.x = 1.0f;
            copyScale.y = 1.0f;
            copyScale.z = 1.0f;
            transform.localScale = copyScale;

            var copyRotation = transform.rotation;
            copyRotation.x = 0.0f;
            copyRotation.y = 0.0f;
            copyRotation.z = 0.0f;
            copyRotation.w = 0.0f;
            transform.rotation = copyRotation;
        }

        void Awake()
        {
            // Enforce singleton pattern.
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }

            if (!activeModes.HasFlag(ActiveModes.Obstruction) && !activeModes.HasFlag(ActiveModes.PropagationCost)) return;

            var audioActorTags = FindObjectsOfType<AudioActorTag>();

            for (int i = 0; i < audioActorTags.Length; i++)
            {
                AudioActorTag audioActorTag = audioActorTags[i];

                if(audioActorTag.triggererType == TriggererType.Player)
                {
                    playerPosition = audioActorTag;
                }
            }
           
            if (playerPosition == null)
            {
                Debug.LogError("Initialization of Spatial Audio Manager failed. Audio Actor Tag for player position could not be found");
                return;
            }
            
            if (activeModes.HasFlag(ActiveModes.PropagationCost))
            {
                for (int i = 0; i < spatialAudioRooms.Length; i++)
                {
                    SpatialAudioRoom room = spatialAudioRooms[i];

                    if (room != null)
                    {
                        bool roomIsValid = room.InitializeRoom(this);

                        if (roomIsValid)
                        {
                            validSpatialAudioRooms.Add(room);
                        }
                    }
                }

                for (int i = 0; i < validSpatialAudioRooms.Count; i++)
                {
                    var connections = FindAllConnectedRooms(validSpatialAudioRooms[i]);
                    if (connections != null && connections.Count > 0)
                    {
                        orderedConnections.Add(validSpatialAudioRooms[i], connections);
                    }
                }

                for (int i = 0; i < spatialAudioPortals.Length; i++)
                {
                    SpatialAudioPortal portal = spatialAudioPortals[i];

                    if (portal != null)
                    {
                        if (portal.portalCollider != null)
                        {
                            colliderToPortalData.Add(portal.portalCollider, portal);

                            if(!portalToNodeData.ContainsKey(portal))
                            {
                                var portalNode = CreatePortalNode(portal);
                                portalToNodeData.Add(portal, portalNode);
                            }                
                        }
                        else
                        {
                            portal.portalCollider = portal.gameObject.GetComponent<BoxCollider>();

                            if (portal.portalCollider != null)
                            {
                                colliderToPortalData.Add(portal.portalCollider, portal);

                                if (!portalToNodeData.ContainsKey(portal))
                                {
                                    var portalNode = CreatePortalNode(portal);
                                    portalToNodeData.Add(portal, portalNode);
                                }
                            }
                        }
                    }
                }
            }

            playerNode.isPlayer = true;

            managerInitialized = true;
        }

        private SpatialAudioNode CreatePortalNode(SpatialAudioPortal portal)
        {
            SpatialAudioNode portalNode = new SpatialAudioNode();
            portalNode.nodePortal = portal;

            return portalNode;
        }

        private List<SpatialAudioRoom> FindAllConnectedRooms(SpatialAudioRoom startingRoom)
        {
            if (startingRoom == null)
            {
                return null;
            }

            var connectionList = new List<SpatialAudioRoom>();
            connectionList.Add(startingRoom);

            var startingRooms = new List<SpatialAudioRoom>();
            startingRooms.Add(startingRoom);

            // For a quicker look-up than iterating over the list above.
            HashSet<SpatialAudioRoom> addedRooms = new HashSet<SpatialAudioRoom>();
            addedRooms.Add(startingRoom);

            for (int i = 0; i < startingRooms.Count; i++)
            {
                SpatialAudioRoom newStartingRoom = startingRooms[i];

                for (int j = 0; j < newStartingRoom.roomConnections.Count; j++)
                {
                    if (addedRooms.Contains(newStartingRoom.roomConnections[j].connectedRoom) == false)
                    {
                        startingRooms.Add(newStartingRoom.roomConnections[j].connectedRoom);
                        addedRooms.Add(newStartingRoom.roomConnections[j].connectedRoom);
                        connectionList.Add(newStartingRoom.roomConnections[j].connectedRoom);
                    }
                }
            }
            return connectionList;
        }
        #endregion

        #region Public Methods
        public void RegisterRoomAwareInstance(FMOD.Studio.EventInstance instance, Transform transform, float maxDistance, SpatialAudioRoom initialRoom = null, bool isResonanceAudioSource = false)
        {
            if (!instance.isValid() || transform == null) { return; }

            RoomAwareInstance roomAwareInstance = new RoomAwareInstance();
            roomAwareInstance.eventInstance = instance;
            roomAwareInstance.attachedTransform = transform;
            roomAwareInstance.cachePosition = transform.position;
            roomAwareInstance.maxDistance = maxDistance;
            roomAwareInstance.usesResonanceAudioSource = isResonanceAudioSource;

            if (initialRoom != null)
            {
                roomAwareInstance.spatialAudioRoom = initialRoom;
            }

            // Set propagation cost and osbtruction (if applicable) to max value, before the first correct values are calculated on LateUpdate.
            if (SpatialAudioManager.instance != null)
            {
                if (SpatialAudioManager.instance.activeModes.HasFlag(ActiveModes.PropagationCost))
                {
                    SetParameterValue(PropagationCostParameter, CostMaxValue, instance);
                }

                if (SpatialAudioManager.instance.activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    SetParameterValue(ObstructionParameter, ObstructionMaxValue, instance);
                }
            }

            registeredInstances.Add(roomAwareInstance);
        }

        public void AddCurrentPlayerRoom(SpatialAudioRoom room)
        {
            if (currentPlayerRooms.Contains(room))
            {
                int oldIndex = currentPlayerRooms.IndexOf(room);
                SpatialAudioRoom r = currentPlayerRooms[oldIndex];
                currentPlayerRooms.RemoveAt(oldIndex);
                currentPlayerRooms.Insert(0, room);
            }
            else
            {
                currentPlayerRooms.Insert(0, room);
            }
        }

        public void RemoveCurrentPlayerRoom(SpatialAudioRoom room)
        {
            if (currentPlayerRooms.Contains(room))
            {
                currentPlayerRooms.Remove(room);
            }
        }

        public void AddObstructingCollider(Collider collider)
        {
            if (!obstructingColliders.Contains(collider))
            {
                obstructingColliders.Add(collider);
            }
        }

        public void RemoveObstructingCollider(Collider collider)
        {
            if (obstructingColliders.Contains(collider))
            {
                obstructingColliders.Remove(collider);
            }
        }

        public bool CheckIfColliderObstructing(Collider collider)
        {
            if (obstructingColliders.Contains(collider))
                return true;
            else
                return false;
        }
        #endregion

        #region Registered Instance Update
        void LateUpdate()
        {
            if (!managerInitialized || playerPosition == null) 
              return;

            timer += Time.deltaTime;

            if (timer > CheckInterval)
            {
                timer = 0.0f;

                // Check that a registered instance is still playing and its position is known.
                UpdateRegisteredInstanceValidity();
            
                _playerPosition = playerPosition.transform.position;

                // Find registered instances that are within hearing distance from the player.
                // <- In other words, we don't want to do spatial audio calculations for sounds that are inaudible anyway.
                // If the propagation cost mode is active also store the calculated player-to-emitter distances, 
                // since they will be again needed later on in the check protocol.
                audibleInstances.Clear();
                instanceToPlayerDistances.Clear();
                CheckInstanceAudibility(ref audibleInstances, ref instanceToPlayerDistances);

                // If only the obstruction mode is active, check for obstruction for all audible registered instances.
                if (!activeModes.HasFlag(ActiveModes.PropagationCost) && activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    CalculateObstruction(audibleInstances);
                }

                if (activeModes.HasFlag(ActiveModes.PropagationCost) && currentPlayerRooms != null && currentPlayerRooms.Count > 0)
                {
                    // The first player room on the list will locked as the current player room for the duration of the check round.
                    currentPlayerRoom = currentPlayerRooms[0];

                    // Find the current room of the registered instance.
                    // Put aside the registered instances for which the room look-up failed.
                    // If the current room was known previously, a new room check is only performed if the position of the instance has changed.
                    instancesWithKnownRoom.Clear();
                    instancesWithUnknownRoom.Clear();
                    UpdateRegisteredInstanceRoom(ref instancesWithKnownRoom, ref instancesWithUnknownRoom);

                    // If the instance room cannot be determined we will remove all propagation cost abd obsruction from it.
                    // <- Lesser evil than potentially missing hearing some vital audio, such as dialogue etc. 
                    ResetPropagationCost(instancesWithUnknownRoom);
                    if (activeModes.HasFlag(ActiveModes.Obstruction))
                        ResetObstruction(instancesWithUnknownRoom);

                    // Split the obtained relevant registered instances to those which are in the current player room and to those located in other rooms.
                    instancesInPlayerRoom.Clear();
                    instancesInOtherRooms.Clear();
                    DivideInstances(ref instancesWithKnownRoom, ref instancesInPlayerRoom, ref instancesInOtherRooms, currentPlayerRoom);

                    // Remove any propagation cost from instances that are in the same room with the player that may have been applied during previous check cycles.
                    ResetPropagationCost(instancesInPlayerRoom);

                    // Calculate obstruction for instances located in the player room if the obstruction mode is active.
                    if (activeModes.HasFlag(ActiveModes.Obstruction))
                    {
                        CalculateObstruction(instancesInPlayerRoom);
                    }

                    // Calculate propagation cost for instances in other rooms.
                    // For these instances, obstruction will be calculated after the propagation cost check - provided that the obstruction mode is active 
                    // and the obtained propagation cost is under the user-set threshold level controlled by the variable "obstructionMaxPropagationCost".
                    // <- This gives the user some control over reducing possibly unnecessary raycasting.
                    // <- A high propagation cost probably means that the room walls already block the sound at a level that makes the added obstruction calculations redundant.            
                    CalculatePropagationCost(instancesInOtherRooms);
                }
            }
        }

        private void UpdateRegisteredInstanceValidity()
        {
            for (int i = registeredInstances.Count - 1; i > -1; i--)
            {
                RoomAwareInstance roomAwareInstance = registeredInstances[i];

                if (roomAwareInstance != null)
                {
                    if (!roomAwareInstance.eventInstance.isValid())
                    {
                        registeredInstances.RemoveAt(i);
                        continue;
                    }
                    else 
                    {
                        FMOD.Studio.PLAYBACK_STATE playbackState;

                        roomAwareInstance.eventInstance.getPlaybackState(out playbackState);

                        if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                        {
                            registeredInstances.RemoveAt(i);
                            continue;
                        }
                    }

                    if (roomAwareInstance.attachedTransform == null)
                    {
                        registeredInstances.RemoveAt(i);

                        // If the followed transform has been lost, remove possibly added propagation cost / obstruction, since the situation is now unclear.
                        if (activeModes.HasFlag(ActiveModes.Obstruction))
                            ResetObstruction(null, roomAwareInstance);

                        if (activeModes.HasFlag(ActiveModes.PropagationCost))
                            ResetPropagationCost(null, roomAwareInstance);
                    }
                }
            }
        }

        private void UpdateRegisteredInstanceRoom(ref List<RoomAwareInstance> instancesWithKnownRoom, ref List<RoomAwareInstance> instancesWithUnknownRoom)
        {
            for (int i = 0; i < registeredInstances.Count; i++)
            {
                if (registeredInstances[i].spatialAudioRoom == null)
                {
                    bool isIdentical = CheckIfPositionIdentical(registeredInstances[i].attachedTransform.position, registeredInstances[i].cachePosition);

                    if (!isIdentical)
                    {
                        registeredInstances[i].cachePosition = registeredInstances[i].attachedTransform.position; 
                    }

                    var initialRoom = FindInitialRoom(registeredInstances[i].attachedTransform.position);

                    if (initialRoom != null)
                    {
                        registeredInstances[i].spatialAudioRoom = initialRoom;
                        instancesWithKnownRoom.Add(registeredInstances[i]);
                    }
                    else
                    {
                        instancesWithUnknownRoom.Add(registeredInstances[i]);
                    }
                }
                else
                {
                    bool isIdentical = CheckIfPositionIdentical(registeredInstances[i].attachedTransform.position, registeredInstances[i].cachePosition);

                    if (!isIdentical)
                    {
                        registeredInstances[i].cachePosition = registeredInstances[i].attachedTransform.position;

                        var currentRoom = CheckForRoomChange(registeredInstances[i].spatialAudioRoom, registeredInstances[i].attachedTransform.position);

                        if (currentRoom != null)
                        {
                            registeredInstances[i].spatialAudioRoom = currentRoom;
                            instancesWithKnownRoom.Add(registeredInstances[i]);
                        }
                        else
                        {
                            // Remove the previously known room, since we cannot know what has happened when the instance has moved.
                            registeredInstances[i].spatialAudioRoom = null;
                            instancesWithUnknownRoom.Add(registeredInstances[i]);
                        }
                    }
                    else
                    {
                        instancesWithKnownRoom.Add(registeredInstances[i]);
                    }
                }
            }
        }

        // This method is used when the current room of the Fmod event instance was not known initially or during the previous frame.
        private SpatialAudioRoom FindInitialRoom(Vector3 position)
        {
            for (int i = 0; i < validSpatialAudioRooms.Count; i++)
            {
                // If the spatial audio room geometry has been set correctly the trigger collider areas of different rooms should only minimally overlap.
                // <- This tool does not support nested rooms.
                // <- In other words, we will pick the first found room to be the current room for the event instance. 
                for (int j = 0; j < validSpatialAudioRooms[i].colliders.Length; j++)
                {
                    if (CheckIfPositionInsideCollider(validSpatialAudioRooms[i].colliders[j], position))
                    {
                        return validSpatialAudioRooms[i];
                    }
                }
            }

            return null;
        }

        private SpatialAudioRoom CheckForRoomChange(SpatialAudioRoom previouslyKnownRoom, Vector3 currentPosition)
        {
            SpatialAudioRoom currentRoom;

            var orderedConnectionsList = orderedConnections[previouslyKnownRoom];

            if (orderedConnectionsList != null && orderedConnectionsList.Count > 0)
            {
                for (int i = 0; i < orderedConnectionsList.Count; i++)
                {
                    for (int j = 0; j < orderedConnectionsList[i].colliders.Length; j++)
                    {
                        var collider = orderedConnectionsList[i].colliders[j];

                        if (collider != null)
                        {
                            if (CheckIfPositionInsideCollider(collider, currentPosition))
                            {
                                currentRoom = orderedConnectionsList[i];
                                return currentRoom;
                            }
                        }
                    }
                }
            }

            currentRoom = FindInitialRoom(currentPosition);

            return currentRoom;
        }

        private bool CheckIfPositionInsideCollider(Collider collider, Vector3 instancePosition)
        {
            bool isInside = (collider.ClosestPoint(instancePosition) - instancePosition).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
            return isInside;
        }

        private bool CheckIfPositionIdentical(Vector3 posA, Vector3 posB) 
        {
            bool isIdentical = (posA - posB).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
            return isIdentical;
        }

        private void CheckInstanceAudibility(ref List<RoomAwareInstance> listForAudibleInstances, ref Dictionary<RoomAwareInstance, float> playerToEmitterDistances)
        {
            for (int i = 0; i < registeredInstances.Count; i++)
            {
                float playerToEmitterDistance = Vector3.Distance(_playerPosition, registeredInstances[i].attachedTransform.position);

                if (playerToEmitterDistance < registeredInstances[i].maxDistance)
                {
                    listForAudibleInstances.Add(registeredInstances[i]);

                    if (activeModes.HasFlag(ActiveModes.PropagationCost))
                    {
                        playerToEmitterDistances.Add(registeredInstances[i], playerToEmitterDistance);
                    }
                }
            }
        }

        // Divides instances based on whether they are located in the same room with the player or some other room.
        private void DivideInstances(ref List<RoomAwareInstance> allRelevantInstances, 
                                     ref List<RoomAwareInstance> instancesInPlayerRoom, 
                                     ref List<RoomAwareInstance> instancesInOtherRooms, 
                                     SpatialAudioRoom currentPlayerRoom)
        {
            for (int i = 0; i < allRelevantInstances.Count; i++)
            {
                if (allRelevantInstances[i].spatialAudioRoom == currentPlayerRoom)
                {
                    instancesInPlayerRoom.Add(allRelevantInstances[i]);
                }
                else
                {
                    instancesInOtherRooms.Add(allRelevantInstances[i]);
                }
            }
        }

        private void SetParameterValue(string parameterName, float parameterValue, FMOD.Studio.EventInstance eventInstance) 
        {
            if (!string.IsNullOrEmpty(parameterName))
            {
                FMOD.RESULT result = eventInstance.setParameterByName(parameterName, parameterValue);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError("Spatial Audio Manager failed to set a parameter. Fmod error: " + result);
                }
            }
            else
            {
                Debug.LogError("Spatial Audio Manager failed to set a parameter. The parameter name was null or empty.");
            }
        }
        #endregion

        #region Obstruction
        private void CalculateObstruction(List<RoomAwareInstance> instances, RoomAwareInstance instance = null)
        {
            bool debug = false;

#if UNITY_EDITOR           
            if (drawDebugLines.HasFlag(DrawDebugLines.Obstruction))
            {
                debug = true;
            }
#endif
            if (instances != null)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    float obstruction = SpatialAudioObstructionChecker.ObstructionCheck(_playerPosition, instances[i].attachedTransform.position,
                                                                                        requireObstructionTag, obstructionLayerMask, obstructionQueryTriggers, 
                                                                                        obstructionRaycastSpread, debug, ignoreSelfCollider);

                    SetParameterValue(ObstructionParameter, obstruction, instances[i].eventInstance);
                }
            }
            else if (instance != null)
            {
                float obstruction = SpatialAudioObstructionChecker.ObstructionCheck(_playerPosition, instance.attachedTransform.position,
                                                                                    requireObstructionTag, obstructionLayerMask,obstructionQueryTriggers, 
                                                                                    obstructionRaycastSpread, debug, ignoreSelfCollider);

                SetParameterValue(ObstructionParameter, obstruction, instance.eventInstance);

            }
        }
       
        private void ResetObstruction(List<RoomAwareInstance> instances, RoomAwareInstance instance = null)
        {
            if (instances != null)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    SetParameterValue(ObstructionParameter, ObstructionMinValue, instances[i].eventInstance);
                }
            }
            else if (instance != null)
            {
                SetParameterValue(ObstructionParameter, ObstructionMinValue, instance.eventInstance);
            }
        }
        #endregion

        #region Propagation Cost
        private void CalculatePropagationCost(List<RoomAwareInstance> instances)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                RoomAwareInstance instance = instances[i];
                SpatialAudioRoom instanceRoom = instance.spatialAudioRoom;

                // Raycast from the instance position towards the player.
                // The obtained collider data will utilized by the 'FindTraversalRoutes' -method (see the method for more detailed walkthrough).
                float instanceToPlayerDistance;
                instanceToPlayerDistances.TryGetValue(instance, out instanceToPlayerDistance);

                var portalHits = RaycastPortals(instance.attachedTransform.position, _playerPosition, instanceToPlayerDistance);
                Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints = new Dictionary<SpatialAudioPortal, Vector3>();

                // Find and store routes from the instance towards player through portals.
                var routesToPlayer = FindTraversalRoutes(instanceRoom, ref portalHits, instance.maxDistance, ref portalClosestPoints, instance);

                // Filter out any routes that did not reach the player.
                for (int j = routesToPlayer.Count - 1; j > -1; j--)
                {
                    SpatialAudioRoute route = routesToPlayer[j];

                    SpatialAudioNode lastNode = route.routePoints[route.routePoints.Count - 1];

                    if (!lastNode.isPlayer)
                    {
                        routesToPlayer.RemoveAt(j);
                    }
                }

                // If no route to the player was found, apply full propagation cost.
                if (routesToPlayer.Count < 1)
                {
                    if (activeModes.HasFlag(ActiveModes.Obstruction))
                    {
                        if (obstructionCheckThreshold >= CostMaxValue)
                        {
                            CalculateObstruction(null, instance);
                        }
                        else
                        {
                            ResetObstruction(null, instance);
                        }
                    }
                    SetParameterValue(PropagationCostParameter, CostMaxValue, instance.eventInstance);
                    continue;
                }

                // Calculate the propagation cost to the player for each route and pick the one with the lowest value.
                // If multiple routes have the same total propagation cost, pick the one with the shortest traversal distance.
                SpatialAudioRoute lowestPropagationCostRoute = null;
                float lowestPropagationCost = float.MaxValue;

                for (int j = 0; j < routesToPlayer.Count; j++)
                {
                    SpatialAudioRoute route = routesToPlayer[j];
                    float totalPropagationCost = CalculatePathPropagationCost(route, portalClosestPoints);

                    if (totalPropagationCost < lowestPropagationCost)
                    {
                        lowestPropagationCost = totalPropagationCost;
                        lowestPropagationCostRoute = route;
                    }
                    else if (totalPropagationCost == lowestPropagationCost)
                    {
                        if (lowestPropagationCostRoute != null)
                        {
                            if(route.routeLength < lowestPropagationCostRoute.routeLength)
                            {
                                lowestPropagationCost = totalPropagationCost;
                                lowestPropagationCostRoute = route;
                            }
                        }
                        else
                        {
                            lowestPropagationCost = totalPropagationCost;
                            lowestPropagationCostRoute = route;
                        }
                    }
                }

                if (drawDebugLines.HasFlag(DrawDebugLines.PropationCost))
                {
                    DrawSoundPath(lowestPropagationCostRoute, portalClosestPoints);
                }
                
                if (activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    if (obstructionCheckThreshold >= lowestPropagationCost)
                    {
                        CalculateObstruction(null, instance);
                    }
                    else
                    {
                        ResetObstruction(null, instance);
                    }
                }

                // Assign the calculated propagation cost to the FMOD Event Instance.
                SetParameterValue(PropagationCostParameter, lowestPropagationCost, instance.eventInstance);

                // Modify the fall-off distance of the instance to take into account the (potentially) longer route traversed. 
                float directDistance;
                instanceToPlayerDistances.TryGetValue(instance, out directDistance);
                ScaleFalloffDistance(directDistance, lowestPropagationCostRoute.routeLength, instance);
            }
        }

        private void ResetPropagationCost(List<RoomAwareInstance> instances, RoomAwareInstance instance = null)
        {
            if (instances != null)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    SetParameterValue(PropagationCostParameter, CostMinValue, instances[i].eventInstance);

                    if (instance.usesResonanceAudioSource)
                    {
                        bool succeeded = ResonanceAudioSourceUtility.SetResonanceAudioSourceMaxDistance(instance.eventInstance, instances[i].maxDistance);

                        if (!succeeded)
                        {
                            Debug.LogError("Scaling of the rolloff distance failed for a Resonance Audio Source");
                        }
                    }
                    else
                    {
                        instances[i].eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, instances[i].maxDistance);
                    }
                }
            }
            else if (instance != null)
            {
                SetParameterValue(PropagationCostParameter, CostMinValue, instance.eventInstance);

                if (instance.usesResonanceAudioSource)
                {
                    bool succeeded = ResonanceAudioSourceUtility.SetResonanceAudioSourceMaxDistance(instance.eventInstance, instance.maxDistance);

                    if (!succeeded)
                    {
                        Debug.LogError("Scaling of the rolloff distance failed for a Resonance Audio Source");
                    }
                }
                else
                {
                    instance.eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, instance.maxDistance);
                }
            }
        }

        private RaycastHit[] RaycastPortals(Vector3 instancePosition, Vector3 playerPosition, float raycastDistance)
        {
            Vector3 direction = playerPosition - instancePosition;

            RaycastHit[] hits = Physics.RaycastAll(instancePosition, direction, raycastDistance, portalLayerMask, QueryTriggerInteraction.Collide);

            return hits;
        }
  
        private List<SpatialAudioRoute> FindTraversalRoutes(SpatialAudioRoom startingRoom, ref RaycastHit[] hitData, float routeMaxLength, 
                                                            ref Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints, RoomAwareInstance instance)
        {
            List<SpatialAudioRoute> traversalRoutes = new List<SpatialAudioRoute>();
            SpatialAudioNode instanceNode = new SpatialAudioNode();
            instanceNode.isInstance = true;
            instanceNode.roomAwareInstance = instance;

            // Get all the portals in the instance's room.
            var startingRoomPortals = GetStartingRoomPortals(startingRoom);

            if (startingRoomPortals.Count < 1)
                return null;

            // For each portal, find the point traversing through which gives the shortest distance from the sound to the player.
            // First, check the raycast data obtained earlier: if the portal was hit, store the hit point on the portal.
            // If a portal was not hit, calculate the closest point on the portal from both the player's and the emitter's position.
            // Of the two points, select and store the one which results in the lowest overall distance when we pass through it on route from the sound to the player.

            for (int i = 0; i < startingRoomPortals.Count; i++)
            {
                SpatialAudioPortal portal = startingRoomPortals[i];

                bool raycastHitFound = CheckIfPortalWasHit(portal, ref hitData, ref portalClosestPoints);

                if (raycastHitFound == false)
                {
                    CalculatePortalClosestPoint(portal, instance.attachedTransform.position, ref portalClosestPoints);
                }

                // Check that the closest point on portal is within the maximum rolloff distance of the sound.
                // If true, start a new route from that portal towards the player.
                // Store the route distance.
                float instanceToPortalDistance = Vector3.Distance(instance.attachedTransform.position, portalClosestPoints[portal]);

                if (instanceToPortalDistance <= routeMaxLength) 
                {
                    var newRoute = new SpatialAudioRoute();
                    newRoute.routePoints.Add(instanceNode);
                    newRoute.cacheLastNode = instanceNode;
                    SpatialAudioNode newNode = portalToNodeData[portal];
                    newRoute.routePoints.Add(newNode);
                    newRoute.routeLength = instanceToPortalDistance;
                    newRoute.visitedRooms.Add(startingRoom);
                    traversalRoutes.Add(newRoute);                 
                }
            }

            // Start extending routes until:
            // 1. They reach the player OR
            // 2. Their length exceeds the maximum rolloff distance of the sound OR
            // 3. They cannot find any new rooms to visit OR
            // 4. The number of rooms visited exceeds the set max number (checkLevelLimit);

            for (int i = 0; i < traversalRoutes.Count; i++) 
            {
                SpatialAudioRoute route = traversalRoutes[i];

                while (route.routeLength < routeMaxLength && !CheckIfLastNodeIsPlayer(route) && 
                       route.visitedRooms.Count <= checkLevelLimit && NewNodeHasBeenAdded(route))
                {
                    SpatialAudioNode lastNode = route.routePoints[route.routePoints.Count - 1];

                    // There should always be just two, process the one that has not been yet visited ->
                    List<SpatialAudioRoom> connectedRooms = lastNode.nodePortal.GetConnectedRooms();

                    bool unvisitedRoomFound = false;
                    SpatialAudioRoom unvisitedRoom = null;

                    for (int j = 0; j < connectedRooms.Count; j++)
                    {
                        if (!route.visitedRooms.Contains(connectedRooms[j]))
                        {
                            unvisitedRoomFound = true;
                            unvisitedRoom = connectedRooms[j];
                        }                    
                    }

                    if (unvisitedRoomFound == true)
                    {
                        SpatialAudioRoom currentUnvisitedRoom = unvisitedRoom;

                        if (currentUnvisitedRoom == currentPlayerRoom)
                        {
                            Vector3 portalPosition = portalClosestPoints[lastNode.nodePortal];
                            float distanceToPlayer = Vector3.Distance(portalPosition, _playerPosition);

                            float totalDistance = route.routeLength + distanceToPlayer;

                            if (totalDistance <= routeMaxLength)
                            {
                                route.routePoints.Add(playerNode);
                                route.cacheLastNode = lastNode;
                                route.routeLength = totalDistance;
                                break;
                            }
                            else
                            {
                                route.cacheLastNode = lastNode;
                                break;
                            }
                        }

                        // From the current 'unvisited' room find the portals that lead to yet another unvisited rooms. 
                        var portalsToNewUnvisitedRooms = GetPortalsToUnvisitedRooms(currentUnvisitedRoom, ref route.visitedRooms);
                        route.visitedRooms.Add(currentUnvisitedRoom);

                        // Temporarily store the new valid portals together with their total route length. 
                        Dictionary<SpatialAudioPortal, float> newValidPortals = new Dictionary<SpatialAudioPortal, float>();

                        for (int k = 0; k < portalsToNewUnvisitedRooms.Count; k++)
                        {
                            SpatialAudioPortal newPortal = portalsToNewUnvisitedRooms[k];

                            // Similarly to what was done earlier, find the closest traversal points on portals and check that the max traversal distance.
                            // <- Only do the check if the traversal point for that portal has not yet been determined
                            if (!portalClosestPoints.ContainsKey(newPortal))
                            {
                                bool raycastHitFound = CheckIfPortalWasHit(newPortal, ref hitData, ref portalClosestPoints);

                                if (!raycastHitFound)
                                {
                                    CalculatePortalClosestPoint(newPortal, instance.attachedTransform.position, ref portalClosestPoints);
                                }
                            }

                            // Calculate the distance from previous route node portal to the new portal
                            SpatialAudioNode previousNode = route.routePoints[route.routePoints.Count - 1];
                            Vector3 previousNodePortalClosestPoint = portalClosestPoints[previousNode.nodePortal];

                            Vector3 newPortalClosestPoint = portalClosestPoints[newPortal];
                                
                            float distance = Vector3.Distance(previousNodePortalClosestPoint, newPortalClosestPoint);
                            float totalRouteLength = route.routeLength + distance;

                            // Add the new portal to the route extension list if traversing to it does not result 
                            // in exceeding the maximum rolloff distance of the instance.
                            if (totalRouteLength <= routeMaxLength)
                            {
                                newValidPortals.Add(newPortal, totalRouteLength);
                            }
                        }
                            
                        // If the number of new valid portals for route extension is more than one, 
                        // create deep copies of the original route and add those to the 'travelsalRoutes' list.
                        //  <- One of the portals always extends the current existing route.
                        // If no new valid portals were found, the last node and the cached last node of the route 
                        // are set as same in order to terminate the 'while' -loop.

                        if (newValidPortals.Count == 0)
                        {
                            route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                        }

                        else if (newValidPortals.Count == 1)
                        {
                            foreach (var portal in newValidPortals)
                            {
                                SpatialAudioNode newNode = portalToNodeData[portal.Key];
                                route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                                route.routePoints.Add(newNode);
                                route.routeLength = portal.Value;
                            }
                        }
                        else if (newValidPortals.Count > 1)
                        {                                
                            int deepCopyNumber = newValidPortals.Count - 1;

                            foreach (var portal in newValidPortals)
                            {
                                if (deepCopyNumber > 0)
                                {
                                    SpatialAudioRoute routeCopy = new SpatialAudioRoute();
                                    List<SpatialAudioNode> routePointsCopy = new List<SpatialAudioNode>(route.routePoints);
                                    HashSet<SpatialAudioRoom> visitedRoomsCopy = new HashSet<SpatialAudioRoom>(route.visitedRooms);
                                    routeCopy.routePoints = routePointsCopy;
                                    routeCopy.visitedRooms = visitedRoomsCopy;
                                    routeCopy.cacheLastNode = routeCopy.routePoints[routeCopy.routePoints.Count - 1];

                                    SpatialAudioNode newNode = portalToNodeData[portal.Key];
                                    routeCopy.routePoints.Add(newNode);
                                    routeCopy.routeLength = portal.Value;
                                    traversalRoutes.Add(routeCopy);
                                    deepCopyNumber--;                                          
                                }
                                else
                                {
                                    SpatialAudioNode newNode = portalToNodeData[portal.Key];
                                    route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                                    route.routePoints.Add(newNode);
                                    route.routeLength = portal.Value;
                                }
                            }
                        }                      
                    }
                    else
                    {
                        // If no new unvisited were found, the last node and the cached last node 
                        // of the route are set as same in order to terminate the 'while' -loop.
                        route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                    }
                }
            }

            return traversalRoutes;
        }

        public List<SpatialAudioPortal> GetStartingRoomPortals (SpatialAudioRoom startingRoom)
        {
            List<SpatialAudioPortal> portals = new List<SpatialAudioPortal>();

            for (int i = 0; i < startingRoom.roomConnections.Count; i++)
            {
                var roomConnection = startingRoom.roomConnections[i];

                for (int j = 0; j < roomConnection.connectingPortals.Length; j++)
                {
                    SpatialAudioPortal portal = roomConnection.connectingPortals[j];

                    if (!portals.Contains(portal))
                    {
                        portals.Add(portal);
                    }
                }                
            }
            return portals;
        }

        private bool CheckIfPortalWasHit(SpatialAudioPortal portalToCheck, ref RaycastHit[] hitData, ref Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints)
        {
            for (int i = 0; i < hitData.Length; i++)
            {
                RaycastHit hit = hitData[i];

                if (hit.collider is BoxCollider)
                {
                    BoxCollider boxColliderCast = (BoxCollider)hit.collider;

                    if (colliderToPortalData.ContainsKey(boxColliderCast))
                    {
                        var hitPortal = colliderToPortalData[boxColliderCast];

                        if (hitPortal == portalToCheck)
                        {
                            if (!portalClosestPoints.ContainsKey(hitPortal))
                            {
                                portalClosestPoints.Add(hitPortal, hit.point);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void CalculatePortalClosestPoint(SpatialAudioPortal portal, Vector3 instancePosition, ref Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints)
        {
            Vector3 closestFromInstance = portal.portalCollider.ClosestPoint(instancePosition);
            Vector3 closestFromPlayer = portal.portalCollider.ClosestPoint(_playerPosition);

            float distanceThroughInstanceCp = GetIndirectDistance(instancePosition, closestFromInstance, _playerPosition);
            float distanceThroughPlayerCp = GetIndirectDistance(instancePosition, closestFromPlayer, _playerPosition);

            if (distanceThroughInstanceCp <= distanceThroughPlayerCp)
            {
                portalClosestPoints.Add(portal, closestFromInstance);             
            }
            else
            {
                portalClosestPoints.Add(portal, closestFromPlayer);
            }
        }

        private float GetIndirectDistance(Vector3 instancePosition, Vector3 portalPosition, Vector3 playerPosition)
        {
            float instanceToPortal = Vector3.Distance(instancePosition, portalPosition);
            float portalToPlayer = Vector3.Distance(portalPosition, playerPosition);

            float totalDistance = instanceToPortal + portalToPlayer;
            return totalDistance;
        }

        private bool CheckIfLastNodeIsPlayer (SpatialAudioRoute route)
        {
            if (route != null && route.routePoints != null && route.routePoints.Count > 0)
            {
                var lastNode = route.routePoints[route.routePoints.Count - 1];

                if (lastNode.isPlayer)
                {
                    return true;
                }
            }

            return false;
        }

        private bool NewNodeHasBeenAdded(SpatialAudioRoute route)
        {
            if (route != null && route.routePoints != null && route.routePoints.Count > 0)
            {
                var currentLastNode = route.routePoints[route.routePoints.Count - 1];
                
                if (currentLastNode != route.cacheLastNode)
                {
                    return true;
                }
            }

            return false;
        }

        private List<SpatialAudioPortal> GetPortalsToUnvisitedRooms(SpatialAudioRoom room, ref HashSet<SpatialAudioRoom> visitedRooms)
        {
            List<SpatialAudioPortal> newPortals = new List<SpatialAudioPortal>();

            for (int i = 0; i < room.roomConnections.Count; i++)
            {
                var roomConnection = room.roomConnections[i];

                if (!visitedRooms.Contains(roomConnection.connectedRoom))
                {
                    for (int j = 0; j < roomConnection.connectingPortals.Length; j++)
                    {
                        var newPortal = roomConnection.connectingPortals[j];

                        if (newPortal != null && !newPortals.Contains(newPortal))
                        {
                            newPortals.Add(newPortal);
                        }                      
                    }
                }
            }

            return newPortals;
        }

        private float CalculatePathPropagationCost(SpatialAudioRoute route, Dictionary<SpatialAudioPortal, Vector3> portalPositions)
        {
                 float totalPropagationCost = 0;

            for (int i = 0; i < route.routePoints.Count - 2; i++)
            {
                SpatialAudioNode nodeA = route.routePoints[i];
                SpatialAudioNode nodeB = route.routePoints[i + 1];
                SpatialAudioNode nodeC = route.routePoints[i + 2];

                Vector3 posA;
                Vector3 posB;
                Vector3 posC;

                // Node A can be either an emitter or a portal - never the player.  
                if (nodeA.isInstance) 
                {
                    posA = nodeA.roomAwareInstance.attachedTransform.position;
                }
                else { portalPositions.TryGetValue(nodeA.nodePortal, out posA); }

                // Node B is always a portal
                portalPositions.TryGetValue(nodeB.nodePortal, out posB);

                // Node C can be either a portal or the player
                if (nodeC.isInstance)
                {
                    posC = nodeC.roomAwareInstance.attachedTransform.position;
                }
                else 
                {
                    posC = _playerPosition;   
                }

                // Different procedures for 'opening' and 'wall' -types of portals. 
                // <- Diffraction obviously not relevant with walls.
                if (nodeB.nodePortal != null && nodeB.nodePortal.portalType == SpatialAudioPortal.PortalType.Opening)
                {
                    Vector3 dirAB = posB - posA;
                    Vector3 dirBC = posC - posB;

                    float angle;

                    if (dirAB.magnitude < minimumVectorMagnitude || dirBC.magnitude < minimumVectorMagnitude)
                    {
                        angle = 0;
                    }
                    else
                    {
                        angle = Vector3.Angle(dirAB, dirBC);
                    }

                    float portalDiffraction = angle / 180;
                    float portalClosednessCost = nodeB.nodePortal.PortalStatus;
                    float magnitudeScaling;

                    if (dirAB.magnitude < dirBC.magnitude)
                    {
                        magnitudeScaling = dirAB.magnitude / maxCostDistance;
                        magnitudeScaling = Mathf.Clamp01(magnitudeScaling);
                    }
                    else
                    {
                        magnitudeScaling = dirBC.magnitude / maxCostDistance;
                        magnitudeScaling = Mathf.Clamp01(magnitudeScaling);
                    }

                    totalPropagationCost += (magnitudeScaling * (portalDiffraction + nodeB.nodePortal.traversalMaxCost) + portalClosednessCost); 
                }
                else
                {
                    totalPropagationCost += nodeB.nodePortal.wallOcclusion;
                }

                if (totalPropagationCost >= 1)
                {
                    totalPropagationCost = 1;
                    return totalPropagationCost;
                }
            }

            return totalPropagationCost;     
        }

        private void ScaleFalloffDistance(float directRouteLength, float indirectRouteLength, RoomAwareInstance instance)
        {
            float difference = indirectRouteLength - directRouteLength;
            float scaledRolloff = instance.maxDistance - difference;
          
            if (instance.eventInstance.isValid())
            {
                if (instance.usesResonanceAudioSource)
                {
                    bool succeeded = ResonanceAudioSourceUtility.SetResonanceAudioSourceMaxDistance(instance.eventInstance, scaledRolloff);

                    if (!succeeded)
                    {
                        Debug.LogError("Scaling of the rolloff distance failed for a Resonance Audio Source");
                    }
                }
                else
                {
                    FMOD.RESULT result = instance.eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, scaledRolloff);

                    if (result != FMOD.RESULT.OK)
                        Debug.LogError("Scaling of the rolloff distance failed. Fmod error: " + result);

                }
            }
        }
        
        private void DrawSoundPath(SpatialAudioRoute route, Dictionary<SpatialAudioPortal, Vector3> portalPositions)
        {
            for (int i = 0; i < route.routePoints.Count - 1; i++)
            {
                SpatialAudioNode n1 = route.routePoints[i];
                SpatialAudioNode n2 = route.routePoints[i + 1];

                Vector3 pos1;
                Vector3 pos2;

                if (n1.isInstance) { pos1 = n1.roomAwareInstance.attachedTransform.transform.position; }
                else if (n1.isPlayer) { pos1 = _playerPosition; }
                else { portalPositions.TryGetValue(n1.nodePortal, out pos1); }

                if (n2.isInstance) { pos2 = n2.roomAwareInstance.attachedTransform.transform.position; }
                else if (n2.isPlayer) { pos2 = _playerPosition; }
                else { portalPositions.TryGetValue(n2.nodePortal, out pos2); }

                Debug.DrawLine(pos1, pos2, Color.magenta, CheckInterval);
            }
        }
        #endregion

        #region Cleanup
        void OnDestroy()
        {
            // Free the singleton instance.
            if (instance == this)
            {
                instance = null;
            }
        }
        #endregion
    }
}
