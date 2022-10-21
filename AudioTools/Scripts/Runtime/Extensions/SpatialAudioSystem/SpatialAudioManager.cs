﻿// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using System.Linq;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Spatial Audio System/Spatial Audio Manager")]
    public class SpatialAudioManager : MonoBehaviour
    {
        #region Declarations & Initializations

        public static SpatialAudioManager Instance { get; private set; }
        
        public ActiveModes activeModes;
        public DebugDrawModes debugDrawModes;
        public LayerMask obstructionLayerMask;
        public QueryTriggerInteraction obstructionQueryTriggers = QueryTriggerInteraction.Ignore;

        [SerializeField, Tooltip("Collider to ignore when checking for obstruction.")]
        private Collider ignoreSelfCollider;
        public Collider IgnoreSelfCollider { get => ignoreSelfCollider; set => ignoreSelfCollider = value; }
        public bool requireObstructionTag = true;

        [Tooltip("Controls the overall width of the raycast pattern used in the obstruction mode.")]
        [Range(0.0f, 10.0f)]
        public float obstructionRaycastSpread = 1.3f;

        [Range(0.1f, 30.0f)]
        public float maxCostDistance = 0.1f;

        // The maximum number of rooms that will be visited when searching for routes between two rooms.
        [SerializeField, Range(1, 8)]
        private int maxPropagationDepth = 8;
        public int MaxPropagationDepth { get { return maxPropagationDepth; } }

        public SpatialAudioRoom[] spatialAudioRooms = new SpatialAudioRoom[0];
        public SpatialAudioPortal[] spatialAudioPortals = new SpatialAudioPortal[0];
        private List<SpatialAudioRoom> validSpatialAudioRooms = new List<SpatialAudioRoom>();
        private List<RoomPair> roomPairs = new List<RoomPair>();
        private List<SpatialAudioRoom> currentPlayerRooms = new List<SpatialAudioRoom>();
        private SpatialAudioRoom currentPlayerRoom;
        private List<RoomAwareInstance> registeredInstances = new List<RoomAwareInstance>();
        private Vector3 playerPosition;
        /*
        When the level starts, each Spatial Audio Room will be set as a starting room and all the directly or indirectly reachable rooms from it are then searched for.
        <-This should encompass all the other rooms in the level, if the spatial audio geometry has been set up correctly.
        The connected rooms will be stored on a list as they are discovered.
        <- This means that the rooms closer to the starting room will be higher up on the list.
        When we know the previous room for a sound, we can then optimize the new room look-up by checking against the room connection list of the previous room. 
        <- In other words, the sound is now most likely located either in the same room or in one of the rooms nearby, rather than on the other side of the map.
        <- Unless, of course, your game includes teleporting or some other wild types of movement..)
        First-time room look-up for a sound still requires testing against all of the rooms, unless a starting room has been manually provided as the sound is registered.
        <- At least in the case of stationary sounds, it is a good practice to assign a starting room for this small performance boost.
        */
        private Dictionary<SpatialAudioRoom, List<SpatialAudioRoom>> orderedConnections = new Dictionary<SpatialAudioRoom, List<SpatialAudioRoom>>();

        // If the 'Require Obstruction Tag' -mode is active, the SpatialAudioObstructionTag instances populate this list with their associated colliders. 
        private HashSet<Collider> obstructingColliders = new HashSet<Collider>();

        private List<RoomAwareInstance> instancesWithKnownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesWithUnknownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> audibleInstances = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInPlayerRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInOtherRooms = new List<RoomAwareInstance>();
        private Dictionary<RoomAwareInstance, float> instanceToPlayerDistances = new Dictionary<RoomAwareInstance, float>();
        private List<Node> routeNodes = new List<Node>();
        private List<Node> debugDrawRouteNodes = new List<Node>();
        Dictionary<SpatialAudioPortal, float> distancesThroughArrivalPortals = new Dictionary<SpatialAudioPortal, float>();
        private Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints = new Dictionary<SpatialAudioPortal, Vector3>();

        // When calculating the diffraction angle for a given portal, the angle will be set to zero if the magnitude of either direction vector is below this value.
        // This is done to prevent the bugs/edge cases in angle calculations when the player or an emitter is crossing a room boundary. 
        // <- Using very short direction vectors may cause sudden unwanted jumps/artefact in the calculated diffraction values.
        private const float MinimumVectorMagnitude = 0.05f;

        private bool managerInitialized = false;

        [SerializeField]
        private bool debug = false;

        [SerializeField]
        private List<string> debugData = new List<string>();

        // In contrast to Wwise, FMOD does not currently include API functionality for multipositioning EventInstances dynamically.
        // Nevertheless, this setting is for visualizing from which direction / distance a sound should be heard emanating from when it arrives to
        // the listener through portal openingss.
        // If a multipositioning feature is added to FMOD sometime in the future, it is easy to modify this tool to take advantage of it.
        private bool visualizeEmitterVirtualPositions = false;

        [System.Flags]
        public enum ActiveModes
        {
            Nothing,
            PropagationCost,
            Obstruction
        }

        [System.Flags]
        public enum DebugDrawModes
        {
            Nothing,
            PropationCost,
            Obstruction
        }

        private class RoomAwareInstance
        {
            public EventInstance eventInstance;
            public Vector3 currentPosition; 
            public Vector3 previousPosition;
            public float maxDistance;
            public SpatialAudioRoom currentRoom;
            public SpatialAudioRoom fixedRoom;
            public PARAMETER_ID propagationID;
            public PARAMETER_ID obstructionID;
        }

        private class RoomPair
        {
            public SpatialAudioRoom roomA;
            public SpatialAudioRoom roomB;
            public List<List<SpatialAudioPortal>> routeAlternatives = new List<List<SpatialAudioPortal>>();

            public RoomPair() { }

            public RoomPair(SpatialAudioRoom roomA, SpatialAudioRoom roomB, List<List<SpatialAudioPortal>> routeAlternatives)
            {
                this.roomA = roomA;
                this.roomB = roomB;
                this.routeAlternatives = routeAlternatives;
            }

            public bool IsMatch(SpatialAudioRoom roomA, SpatialAudioRoom roomB)
            {
                if (this.roomA == null || this.roomB == null || roomA == null || roomB == null)
                    return false;

                if (this.roomA == roomA && this.roomB == roomB)
                    return true;
                else
                    return false;
            }
        }

        private class Route
        {
            public List<SpatialAudioRoom> visitedRooms = new List<SpatialAudioRoom>();
            public List<SpatialAudioPortal> routePortals = new List<SpatialAudioPortal>();
            public SpatialAudioRoom newestFoundRoom = null;

            public Route() { }

            public Route(Route route)
            {
                visitedRooms = new List<SpatialAudioRoom>(route.visitedRooms);
                routePortals = new List<SpatialAudioPortal>(route.routePortals);
                newestFoundRoom = route.newestFoundRoom;
            }
        }

        private struct Node
        {
            public enum NodeType { Emitter, Wall, Opening, Listener };

            public NodeType nodeType;
            public Vector3 position;

            // Only relevant if nodeType == NodeType.Opening
            public float portalClosednessCost;
            public float traversalMaxCost;

            // Only relevant if nodeType == NodeType.Wall
            public float wallOcclusion;

            public Node(NodeType nodeType, Vector3 position, float portalClosednessCost = 0, float traversalMaxCost = 0, float wallOcclusion = 0)
            {
                this.nodeType = nodeType;
                this.position = position;
                this.portalClosednessCost = portalClosednessCost;
                this.traversalMaxCost = traversalMaxCost;
                this.wallOcclusion = wallOcclusion;
            }
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (!activeModes.HasFlag(ActiveModes.Obstruction) && !activeModes.HasFlag(ActiveModes.PropagationCost)) 
                return;

            if (activeModes.HasFlag(ActiveModes.PropagationCost))
            {
                if (spatialAudioPortals == null)
                {
                    Debug.LogError("The 'spatialAudioPortals' array is null");
                    return;
                }

                foreach (var room in spatialAudioRooms)
                {
                    if (room == null)
                        continue;

                    if (room.InitializeRoom(this))
                    {
                        validSpatialAudioRooms.Add(room);
                    }
                }

                foreach (var room in validSpatialAudioRooms)
                {
                    var connectedRooms = FindAllConnectedRooms(room);

                    if (connectedRooms.Count > 0)
                    {
                        orderedConnections.Add(room, connectedRooms);
                    }
                }              

                InitializeRoomPairRouteAlternatives();
            }

            managerInitialized = true;
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

            var addedRooms = new HashSet<SpatialAudioRoom>();
            addedRooms.Add(startingRoom);

            for (int i = 0; i < startingRooms.Count; i++)
            {
                SpatialAudioRoom newStartingRoom = startingRooms[i];

                for (int j = 0; j < newStartingRoom.roomConnections.Count; j++)
                {
                    if (!addedRooms.Contains(newStartingRoom.roomConnections[j].connectedRoom))
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

        public void RegisterRoomAwareInstance(EventInstance eventInstance, SpatialAudioRoom fixedRoom = null)
        {
            if (!eventInstance.isValid())
                return;

            var roomAwareInstance = new RoomAwareInstance();
            roomAwareInstance.eventInstance = eventInstance;
            roomAwareInstance.currentPosition = HelperMethods.GetEventInstancePosition(eventInstance);
            roomAwareInstance.previousPosition = roomAwareInstance.currentPosition;
            roomAwareInstance.previousPosition = transform.position;
            eventInstance.getDescription(out EventDescription eventDescription);
            eventDescription.getMinMaxDistance(out float minDistance, out float maxDistance);
            roomAwareInstance.maxDistance = maxDistance;
            roomAwareInstance.currentRoom = fixedRoom;
            roomAwareInstance.fixedRoom = fixedRoom;

            // Set propagation cost and osbtruction (if applicable) to max value, before the first correct values are calculated on LateUpdate.
            if (activeModes.HasFlag(ActiveModes.PropagationCost))
            {
                if (HelperMethods.InitializeLocalParameterID(eventDescription, Parameters.PropagationCostParameter, ref roomAwareInstance.propagationID))
                {
                    SetParameterValue(roomAwareInstance.propagationID, Parameters.PropagationCostMaxValue, eventInstance);
                }
            }

            if (activeModes.HasFlag(ActiveModes.Obstruction))
            {
                if (HelperMethods.InitializeLocalParameterID(eventDescription, Parameters.ObstructionParameter, ref roomAwareInstance.obstructionID))
                {
                    SetParameterValue(roomAwareInstance.obstructionID, Parameters.ObstructionMaxValue, eventInstance);
                }
            }

            registeredInstances.Add(roomAwareInstance);
        }

        public void AddCurrentPlayerRoom(SpatialAudioRoom room)
        {
            if (currentPlayerRooms.Contains(room))
            {
                int oldIndex = currentPlayerRooms.IndexOf(room);
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

        public bool IsObstructingCollider(Collider collider)
        {
            if (obstructingColliders.Contains(collider))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Registered Instance Update

        void LateUpdate()
        {
            if (!managerInitialized)
                return;

            bool foundPlayerPosition = HelperMethods.TryGetListenerPosition(out playerPosition);

            if (!foundPlayerPosition)
                return;

            // Check that a registered instance is still playing and get its positional data.
            CheckRegisteredInstanceValidity();
            UpdateInstancePositionalData();

            // Find registered instances that are within hearing distance from the player.
            // <- In other words, we don't want to do spatial audio calculations for sounds that are inaudible anyway.
            // If the propagation cost mode is active then also store the calculated player-to-emitter distances, 
            // since they will be again needed later on during the check protocol.
            audibleInstances.Clear();
            instanceToPlayerDistances.Clear();
            CheckInstanceAudibility(ref audibleInstances, ref instanceToPlayerDistances);

            if (activeModes.HasFlag(ActiveModes.PropagationCost) && currentPlayerRooms != null && currentPlayerRooms.Count > 0)
            {
                currentPlayerRoom = GetHighestPriorityRoom();

                // Find the current room for the registree.
                // Put aside the registered instances for which the room look-up failed.
                // If the current room was known previously, a new room check is only performed if the position of the registred instance has changed.
                instancesWithKnownRoom.Clear();
                instancesWithUnknownRoom.Clear();
                UpdateRegisteredInstanceRoom(ref instancesWithKnownRoom, ref instancesWithUnknownRoom);

                // If the registree room cannot be determined, we will remove all propagation cost and obsruction from it.
                // <- Lesser evil than potentially missing hearing some vital audio, such as dialogue etc. 
                ResetPropagationCost(instancesWithUnknownRoom);
                if (activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    ResetObstruction(instancesWithUnknownRoom);
                }

                // Split the obtained relevant registered instances to those which are in the current player room and to those located in other rooms.
                instancesInPlayerRoom.Clear();
                instancesInOtherRooms.Clear();
                DivideInstancesByPlayerRelativePosition(ref instancesWithKnownRoom, ref instancesInPlayerRoom, ref instancesInOtherRooms, currentPlayerRoom);

                // Remove propagation cost from registered instances that are in the same room with the player, which may have been applied during the previous check cycle.
                ResetPropagationCost(instancesInPlayerRoom);

                // Calculate propagation cost for instances in other rooms.                  
                CalculatePropagationCosts(instancesInOtherRooms);
            }

            if (activeModes.HasFlag(ActiveModes.Obstruction))
            {
                CheckObstruction(audibleInstances);
            }

            if (debug)
            {
                UpdateDebugData();
            }
        }

        private void UpdateDebugData()
        {
#if UNITY_EDITOR

            debugData.Clear();

            foreach (var item in registeredInstances)
            {
                if (item != null &&  item.eventInstance.isValid())
                {
                    float propagationCost = 0;
                    float obstruction = 0;
                    item.eventInstance.getDescription(out EventDescription eventDescription);
                    eventDescription.getPath(out string path);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);

                    if (activeModes.HasFlag(ActiveModes.PropagationCost))
                    {
                        item.eventInstance.getParameterByID(item.propagationID, out propagationCost);
                    }   
                    
                    if (activeModes.HasFlag(ActiveModes.Obstruction))
                    {
                        item.eventInstance.getParameterByID(item.obstructionID, out obstruction);
                    }

                    string info = name + ", PropagationCost: " + propagationCost + ", Obstruction: " + obstruction;
                    debugData.Add(info);
                }
            }
#endif
        }

        private void CheckRegisteredInstanceValidity()
        {
            for (int i = registeredInstances.Count - 1; i > -1; i--)
            {
                RoomAwareInstance instance = registeredInstances[i];

                if (instance == null)
                    continue;

                if (!instance.eventInstance.isValid())
                {
                    registeredInstances.RemoveAt(i);
                    continue;
                }

                instance.eventInstance.getPlaybackState(out PLAYBACK_STATE playbackState);

                if (playbackState == PLAYBACK_STATE.STOPPED)
                {
                    registeredInstances.RemoveAt(i);
                }
            }
        }

        private void UpdateInstancePositionalData()
        {
            foreach (var instance in registeredInstances)
            {
                instance.previousPosition = instance.currentPosition;
                instance.currentPosition = HelperMethods.GetEventInstancePosition(instance.eventInstance);
            }
        }

        private SpatialAudioRoom GetHighestPriorityRoom()
        {
            SpatialAudioRoom prioritizedRoom = null;
            bool firstEntryProcessed = false;

            for (int i = 0; i < currentPlayerRooms.Count; i++)
            {
                var room = currentPlayerRooms[i];

                if (room == null)
                    continue;

                if (!firstEntryProcessed)
                {
                    prioritizedRoom = room;
                    firstEntryProcessed = true;
                }
                else if (room.Priority > prioritizedRoom.Priority)
                {
                    prioritizedRoom = room;
                }
            }

            return prioritizedRoom;
        }

        private void UpdateRegisteredInstanceRoom(ref List<RoomAwareInstance> instancesWithKnownRoom, ref List<RoomAwareInstance> instancesWithUnknownRoom)
        {
            foreach (var instance in registeredInstances)
            {
                if (instance.fixedRoom != null)
                {
                    instance.currentRoom = instance.fixedRoom;
                    continue;
                }

                if (instance.currentRoom == null)
                {
                    var initialRoom = FindInitialRoom(instance.currentPosition);

                    if (initialRoom != null)
                    {
                        instance.currentRoom = initialRoom;
                        instancesWithKnownRoom.Add(instance);
                    }
                    else
                    {
                        instancesWithUnknownRoom.Add(instance);
                    }
                }
                else
                {
                    bool isIdentical = CheckIfPositionIdentical(instance.currentPosition, instance.previousPosition);

                    if (isIdentical)
                    {
                        instancesWithKnownRoom.Add(instance);
                    }
                    else
                    {
                        var currentRoom = CheckForRoomChange(instance.currentRoom, instance.currentPosition);

                        if (currentRoom != null)
                        {
                            instance.currentRoom = currentRoom;
                            instancesWithKnownRoom.Add(instance);
                        }
                        else
                        {
                            // Remove the previously known room, since we cannot know what has happened when the instance has moved.
                            instance.currentRoom = null;
                            instancesWithUnknownRoom.Add(instance);
                        }
                    }
                }
            }
        }

        private SpatialAudioRoom FindInitialRoom(Vector3 position)
        {
            SpatialAudioRoom room = null;
            bool firstEntryHandled = false;

            for (int i = 0; i < validSpatialAudioRooms.Count; i++)
            {
                // If the spatial audio room geometry has been set correctly the trigger collider areas of different rooms should only minimally overlap.
                // <- i.e. early out, since the system does not currently support nested rooms.
                for (int j = 0; j < validSpatialAudioRooms[i].colliders.Count; j++)
                {
                    if (CheckIfPositionInsideCollider(validSpatialAudioRooms[i].colliders[j], position))
                    {
                        if (!firstEntryHandled)
                        {
                            firstEntryHandled = true;
                            room = validSpatialAudioRooms[i];
                            break;
                        }
                        else if (validSpatialAudioRooms[i].Priority > room.Priority)
                        {
                            room = validSpatialAudioRooms[i];
                            break;
                        }    
                    }
                }
            }

            return room;
        }

        private SpatialAudioRoom CheckForRoomChange(SpatialAudioRoom previouslyKnownRoom, Vector3 currentPosition)
        {
            SpatialAudioRoom currentRoom = null;
            bool firstEntryHandled = false;
            var connectionOrderedRoomList = orderedConnections[previouslyKnownRoom];

            foreach (var room in connectionOrderedRoomList)
            {
                if (room == null)
                    continue;

                foreach (var collider in room.colliders)
                {
                    if (collider == null)
                        continue;

                    if (CheckIfPositionInsideCollider(collider, currentPosition))
                    {
                        if (!firstEntryHandled)
                        {
                            currentRoom = room;
                            firstEntryHandled = true;
                            break;
                        }
                        else if (room.Priority > currentRoom.Priority)
                        {
                            currentRoom = room;
                            break;
                        }
                    }
                }
            }

            if (currentRoom == null)
            {
                currentRoom = FindInitialRoom(currentPosition);
            }

            return currentRoom;
        }

        private bool CheckIfPositionInsideCollider(Collider collider, Vector3 instancePosition)
        {
            bool isInside = (collider.ClosestPoint(instancePosition) - instancePosition).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
            return isInside;
        }

        private bool CheckIfPositionIdentical(Vector3 posA, Vector3 posB) 
        {
            return (posA - posB).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon;
        }

        private void CheckInstanceAudibility(ref List<RoomAwareInstance> listForAudibleInstances, ref Dictionary<RoomAwareInstance, float> playerToEmitterDistances)
        {
            foreach (var instance in registeredInstances)
            {
                float playerToEmitterDistance = Vector3.Distance(playerPosition, instance.currentPosition);

                if (playerToEmitterDistance <= instance.maxDistance)
                {
                    listForAudibleInstances.Add(instance);

                    if (activeModes.HasFlag(ActiveModes.PropagationCost))
                    {
                        playerToEmitterDistances.Add(instance, playerToEmitterDistance);
                    }
                }
            }
        }

        // Divides instances based on whether they are located in the same room with the player or inside some other room.
        private void DivideInstancesByPlayerRelativePosition(ref List<RoomAwareInstance> allRelevantInstances, 
                                                             ref List<RoomAwareInstance> instancesInPlayerRoom, 
                                                             ref List<RoomAwareInstance> instancesInOtherRooms, 
                                                             SpatialAudioRoom currentPlayerRoom)
        {
            foreach (var instance in allRelevantInstances)
            {
                if (instance.currentRoom == currentPlayerRoom)
                {
                    instancesInPlayerRoom.Add(instance);
                }
                else
                {
                    instancesInOtherRooms.Add(instance);
                }
            }
        }

        private void SetParameterValue(PARAMETER_ID parameterID, float parameterValue, EventInstance eventInstance) 
        {
            var result = eventInstance.setParameterByID(parameterID, parameterValue);

            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning(result); 
            }
        }

        #endregion

        #region Obstruction

        private void CheckObstruction(in List<RoomAwareInstance> instances)
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                CalculateAndSetObstruction(instance.eventInstance, instance.currentPosition, instance.obstructionID);
            }
        }

        private void CalculateAndSetObstruction(EventInstance eventInstance, Vector3 emitterPostion, PARAMETER_ID obstructionID)
        {
            bool debug = false;

#if UNITY_EDITOR
            if (debugDrawModes.HasFlag(DebugDrawModes.Obstruction))
            {
                debug = true;
            }
#endif
            float obstruction = SpatialAudioObstructionChecker.ObstructionCheck(playerPosition, emitterPostion,requireObstructionTag, 
                                                                                obstructionLayerMask, obstructionQueryTriggers,
                                                                                obstructionRaycastSpread, debug, ignoreSelfCollider);
            SetParameterValue(obstructionID, obstruction, eventInstance);
        }

        private void ResetObstruction(List<RoomAwareInstance> instances) 
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                SetParameterValue(instance.obstructionID, Parameters.ObstructionMinValue, instance.eventInstance);
            }
        }

        #endregion

        #region Propagation Cost

        private void InitializeRoomPairRouteAlternatives()
        {
            for (int i = 0; i < validSpatialAudioRooms.Count; i++)
            {
                var roomA = validSpatialAudioRooms[i];

                for (int j = 0; j < validSpatialAudioRooms.Count; j++)
                {
                    var roomB = validSpatialAudioRooms[j];

                    if (roomA != roomB)
                    {
                        var routes = new List<Route>();

                        foreach (var connection in roomA.roomConnections)
                        {
                            if (connection == null || connection.connectedRoom == null || connection.connectedRoom == roomA)
                                continue;

                            foreach (var portal in connection.connectingPortals)
                            {
                                if (portal == null)
                                    continue;

                                var route = new Route();
                                route.visitedRooms.Add(roomA);
                                route.routePortals.Add(portal);
                                route.newestFoundRoom = connection.connectedRoom;
                                routes.Add(route);
                            }
                        }

                        for (int k = 0; k < routes.Count; k++)
                        {
                            var route = routes[k];

                            while (route.newestFoundRoom != roomB && route.visitedRooms.Last() != route.newestFoundRoom && route.visitedRooms.Count <= MaxPropagationDepth)
                            {
                                bool existingRouteExtended = false;
                                var roomToVisit = route.newestFoundRoom;
                                route.visitedRooms.Add(roomToVisit);
                                var deepCopyTemplate = new Route(route);

                                foreach (var connection in roomToVisit.roomConnections)
                                {
                                    if (connection == null || connection.connectedRoom == null || route.visitedRooms.Contains(connection.connectedRoom))
                                        continue;

                                    foreach (var portal in connection.connectingPortals)
                                    {
                                        if (portal == null)
                                            continue;

                                        if (!existingRouteExtended)
                                        {
                                            route.routePortals.Add(portal);
                                            route.newestFoundRoom = connection.connectedRoom;
                                            existingRouteExtended = true;
                                        }
                                        else
                                        {
                                            var newRoute = new Route(deepCopyTemplate);
                                            newRoute.routePortals.Add(portal);
                                            newRoute.newestFoundRoom = connection.connectedRoom;
                                            routes.Add(newRoute);
                                        }
                                    }
                                }
                            }
                        }

                        var routeAlternatives = new List<List<SpatialAudioPortal>>();

                        foreach (var route in routes)
                        {
                            if (route.newestFoundRoom == roomB)
                            {
                                routeAlternatives.Add(route.routePortals);
                            }
                        }

                        // Don't create a room pair if no valid routes were found
                        if (routeAlternatives.Count == 0)
                            continue;

                        var roomPair = new RoomPair(roomA, roomB, routeAlternatives);
                        roomPairs.Add(roomPair);
                    }
                }
            }
        }

        private void CalculatePropagationCosts(List<RoomAwareInstance> instances)
        {
            foreach (var instance in instances)
            {
                Vector3 instancePosition = instance.currentPosition;
                float directRouteLength = instanceToPlayerDistances[instance];
                bool foundPair = TryGetMatchingRoomPair(instance.currentRoom, currentPlayerRoom, out RoomPair roomPair);

                if (!foundPair || roomPair.routeAlternatives.Count == 0)
                {
                    SetParameterValue(instance.propagationID, Parameters.PropagationCostMaxValue, instance.eventInstance); 
                    continue;
                }

                portalClosestPoints.Clear();
                GetClosestPointsOnPortals(instancePosition, playerPosition, ref portalClosestPoints);

                float lowestCost = float.MaxValue;
                float routeLength = float.MaxValue;
                distancesThroughArrivalPortals.Clear();

                for (int i = 0; i < roomPair.routeAlternatives.Count; i++)
                {
                    var routeAlternative = roomPair.routeAlternatives[i];
                    routeNodes.Clear();
                    GetRouteNodes(instancePosition, playerPosition, in routeAlternative, in portalClosestPoints, ref routeNodes);
                    GetRouteCostAndDistance(in routeNodes, out float cost, out float distance);

                    int lastIndex = routeAlternative.Count - 1;

                    if (lastIndex >= 0 && !distancesThroughArrivalPortals.ContainsKey(routeAlternative[lastIndex]))
                    {
                        distancesThroughArrivalPortals.Add(routeAlternative[lastIndex], distance);
                    }

                    if (i == 0 || cost < lowestCost || (cost == lowestCost && distance < routeLength))
                    {
                        lowestCost = cost;
                        routeLength = distance;
#if UNITY_EDITOR
                        if (debugDrawModes.HasFlag(DebugDrawModes.PropationCost))
                        {
                            debugDrawRouteNodes.Clear();
                            debugDrawRouteNodes.AddRange(routeNodes);
                        }
#endif
                    }          
                }

#if UNITY_EDITOR

                if (visualizeEmitterVirtualPositions)
                {
                    foreach (var route in roomPair.routeAlternatives)
                    {
                        if (route.Count == 0)
                            continue;

                        var arrivalPortal = route[route.Count - 1];

                        if (arrivalPortal.portalType == SpatialAudioPortal.PortalType.Wall)
                            continue;

                        if (!portalClosestPoints.ContainsKey(arrivalPortal))
                            continue;

                        Vector3 arrivalPoint = portalClosestPoints[arrivalPortal];
                        Vector3 direction = (arrivalPoint - playerPosition).normalized;
                        float distance = distancesThroughArrivalPortals[arrivalPortal];
                        Vector3 virtualPosition = playerPosition + direction * distance;
                        Debug.DrawLine(playerPosition, virtualPosition, Color.cyan);
                    }
                }

                if (debugDrawModes.HasFlag(DebugDrawModes.PropationCost))
                {
                    DrawSoundPath(debugDrawRouteNodes);
                }
#endif

                SetParameterValue(instance.propagationID, lowestCost, instance.eventInstance);
                ScaleRolloffDistance(directRouteLength, routeLength, instance);
            }
        }

        private bool TryGetMatchingRoomPair(SpatialAudioRoom roomA, SpatialAudioRoom roomB, out RoomPair roomPair)
        {
            roomPair = null;

            foreach (var pair in roomPairs)
            {
                if (pair == null)
                    continue;
                
                if (pair.IsMatch(roomA, roomB))
                {
                    roomPair = pair;
                    return true;
                }
            }

            return false;
        }

        private void GetClosestPointsOnPortals(Vector3 instancePosition, Vector3 playerPosition, ref Dictionary<SpatialAudioPortal, Vector3> closestPoints)
        {
            Vector3 direction = (playerPosition - instancePosition).normalized;
            var ray = new Ray(instancePosition, direction);

            foreach (var portal in spatialAudioPortals)
            {
                var collider = portal.portalCollider;

                if (collider != null && portal != null && !closestPoints.ContainsKey(portal))
                {
                    if (collider.bounds.IntersectRay(ray, out float distance))
                    {
                        Vector3 hitPoint = instancePosition + (direction * distance);
                        closestPoints.Add(portal, hitPoint);
                    }
                    else
                    {
                        Vector3 closestFromInstance = portal.portalCollider.ClosestPoint(instancePosition);
                        Vector3 closestFromPlayer = portal.portalCollider.ClosestPoint(playerPosition);

                        float distanceThroughInstanceCp = GetIndirectDistance(instancePosition, closestFromInstance, playerPosition);
                        float distanceThroughPlayerCp = GetIndirectDistance(instancePosition, closestFromPlayer, playerPosition);

                        if (distanceThroughInstanceCp <= distanceThroughPlayerCp)
                        {
                            portalClosestPoints.Add(portal, closestFromInstance);
                        }
                        else
                        {
                            portalClosestPoints.Add(portal, closestFromPlayer);
                        }
                    }
                }
            }
        }

        private void GetRouteNodes(Vector3 instancePosition, Vector3 playerPosition, in List<SpatialAudioPortal> routePortals, in Dictionary<SpatialAudioPortal, Vector3> closestPoints, ref List<Node> routeNodes)
        {
            routeNodes.Add(new Node(Node.NodeType.Emitter, instancePosition));

            for (int i = 0; i < routePortals.Count; i++)
            {
                var portal = routePortals[i];

                if (portal != null && closestPoints.ContainsKey(portal))
                {
                    var nodeType = portal.portalType == SpatialAudioPortal.PortalType.Opening ? Node.NodeType.Opening : Node.NodeType.Wall;
                    Vector3 position = closestPoints[portal];
                    float portalClosednessCost = nodeType == Node.NodeType.Opening ? portal.PortalStatus : 0f;
                    float traversalMaxCost = nodeType == Node.NodeType.Opening ? portal.traversalMaxCost : 0f;
                    float wallOcclusion = nodeType == Node.NodeType.Wall ? portal.wallOcclusion : 0f;
                    routeNodes.Add(new Node(nodeType, position, portalClosednessCost, traversalMaxCost, wallOcclusion));
                }
            }

            routeNodes.Add(new Node(Node.NodeType.Listener, playerPosition));
        }

        private void ResetPropagationCost(List<RoomAwareInstance> instances)
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                SetParameterValue(instance.propagationID, Parameters.PropagationCostMinValue, instance.eventInstance);

                instance.eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, instance.maxDistance);
            }
        }

        private float GetIndirectDistance(Vector3 instancePosition, Vector3 portalPosition, Vector3 playerPosition)
        {
            float instanceToPortal = Vector3.Distance(instancePosition, portalPosition);
            float portalToPlayer = Vector3.Distance(portalPosition, playerPosition);
            return instanceToPortal + portalToPlayer;
        }

        private void ScaleRolloffDistance(float directRouteLength, float routeLength, RoomAwareInstance instance)
        {
            float difference = routeLength - directRouteLength;
            float scaledRolloff = instance.maxDistance - difference;

            if (instance.eventInstance.isValid())
            {
                var result = instance.eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, scaledRolloff);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError(result);
                }
            }
        }

        private void DrawSoundPath(List<Node> routeNodes)
        {
            for (int i = 0; i < routeNodes.Count - 1; i++)
            {
                Node nodeA = routeNodes[i];
                Node nodeB = routeNodes[i + 1];
                Debug.DrawLine(nodeA.position, nodeB.position, Color.magenta);
            }
        }

        private void GetRouteCostAndDistance(in List<Node> routeNodes, out float cost, out float distance)
        {
            cost = 0f;
            distance = 0f;

            for (int i = 0; i < routeNodes.Count - 2; i++)
            {
                Node nodeA = routeNodes[i];
                Node nodeB = routeNodes[i + 1];
                Node nodeC = routeNodes[i + 2];

                Vector3 posA = nodeA.position;
                Vector3 posB = nodeB.position;
                Vector3 posC = nodeC.position;

                Vector3 dirAB = posB - posA;
                Vector3 dirBC = posC - posB;

                float magnitudeAB = dirAB.magnitude;
                float magnitudeBC = dirBC.magnitude;
                float meanMagnitude = (magnitudeAB + magnitudeBC) / 2;

                if (i == routeNodes.Count - 3)
                {
                    distance += magnitudeAB + magnitudeBC;
                }
                else
                {
                    distance += magnitudeAB;
                }

                float angle;

                if (magnitudeAB < MinimumVectorMagnitude || magnitudeBC < MinimumVectorMagnitude)
                {
                    angle = 0;
                }
                else
                {
                    angle = Vector3.Angle(dirAB, dirBC);
                }

                float portalDiffraction = angle / 180;
                float magnitudeScaling = Mathf.Clamp01(meanMagnitude / maxCostDistance);
                float traversalCost = Mathf.Clamp01(nodeB.traversalMaxCost / maxCostDistance);
                
                if (nodeB.nodeType == Node.NodeType.Opening)
                {
                    cost += (magnitudeScaling * (portalDiffraction + traversalCost) + nodeB.portalClosednessCost);
                }
                else
                {
                    cost += nodeB.wallOcclusion;
                }
            }

            cost = Mathf.Clamp01(cost);
        }

        #endregion

        #region Cleanup

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion
    }
}