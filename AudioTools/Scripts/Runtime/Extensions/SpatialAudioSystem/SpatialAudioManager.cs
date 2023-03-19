// FMOD-Unity-Tools by Ville Ojala
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

        [Tooltip("Controls the overall width of the raycast pattern used in the obstruction mode.")]
        [Range(0.0f, 10.0f)]
        public float obstructionRaycastSpread = 1.3f;

        [SerializeField, Range(1, 8), Tooltip("The maximum number of rooms that will be visited when searching for routes between two rooms.")]
        private int maxPropagationDepth = 8;
        public int MaxPropagationDepth { get { return maxPropagationDepth; } }

        public SpatialAudioRoom[] spatialAudioRooms = new SpatialAudioRoom[0];
        public SpatialAudioPortal[] spatialAudioPortals = new SpatialAudioPortal[0];
        private List<SpatialAudioRoom> validSpatialAudioRooms = new List<SpatialAudioRoom>();

        private Dictionary<SpatialAudioRoom, uint> roomUniqueIDs = new Dictionary<SpatialAudioRoom, uint>();
        private uint ID_Incrementer = 1;
        private Dictionary<SpatialAudioRoom, List<string>> relevantRoomPairsByRoom = new Dictionary<SpatialAudioRoom, List<string>>();
        private Dictionary<string, RoomPair> roomPairs = new Dictionary<string, RoomPair>();

        private List<SpatialAudioRoom> currentListenerRooms = new List<SpatialAudioRoom>();
        private SpatialAudioRoom currentListenerRoom;
        private List<RoomAwareInstance> registeredInstances = new List<RoomAwareInstance>();
        private Vector3 listenerPosition;

        /* On Awake, each SpatialAudioRoom is treated as a starting room and all the directly or indirectly reachable rooms from it are then searched for.
         * This search should encompass all the other rooms in the level if the spatial audio geometry has been set up correctly. The connected rooms 
         * will be added to a list as they are discovered, which means that the rooms closer to the starting room will be higher up on the list.
         * Consequently, if we know the previous room for a sound, we can optimize the new room look-up by checking against the room connection list of the previous room, 
         * as the sound is now most likely located either in the same room or in one of the rooms nearby. First-time room look-up for a sound still requires 
         * testing against all of the rooms, unless a permanent room has been manually provided upon sound registration. */
        private Dictionary<SpatialAudioRoom, List<SpatialAudioRoom>> orderedConnections = new Dictionary<SpatialAudioRoom, List<SpatialAudioRoom>>();

        private List<RoomAwareInstance> instancesWithKnownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesWithUnknownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> audibleInstances = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInListenerRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInOtherRooms = new List<RoomAwareInstance>();
        private Dictionary<RoomAwareInstance, float> instanceToListenerDistances = new Dictionary<RoomAwareInstance, float>();
        private List<Node> routeNodes = new List<Node>();
        private List<Node> debugDrawRouteNodes = new List<Node>();
        Dictionary<SpatialAudioPortal, float> distancesThroughArrivalPortals = new Dictionary<SpatialAudioPortal, float>();
        private Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints = new Dictionary<SpatialAudioPortal, Vector3>();

        /* When calculating a diffraction angle for a portal, the angle will be set to zero if the magnitude of either vector is below this constant value.
         * This is done to prevent undesired results in angle calculations when the listener or an emitter is crossing a room boundary. 
         * Using very short vectors for calculations may cause sudden immersion-breaking jumps/artefacts in the obtained diffraction values. */
        private const float MinimumVectorMagnitude = 0.05f;
        private bool managerInitialized = false;

        [SerializeField]
        private bool debug = false;

        [SerializeField]
        private List<string> debugData = new List<string>();

        /* In contrast to Wwise, FMOD does not currently include API functionality for multipositioning EventInstances dynamically at runtime.
         * Nevertheless, this setting is for visualizing from which direction / distance a sound should be heard emanating from as it arrives to
         * the listener through portal openings. */
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
            public enum NodeType { Emitter, Portal, Listener };

            public NodeType nodeType;
            public Vector3 position;

            // Only relevant if nodeType == NodeType.Opening
            public float portalClosednessStatus;
            public float maxClosednessCost;

            public Node(NodeType nodeType, Vector3 position, float portalClosednessStatus = 0, float maxClosednessCost = 0)
            {
                this.nodeType = nodeType;
                this.position = position;
                this.portalClosednessStatus = portalClosednessStatus;
                this.maxClosednessCost = maxClosednessCost;
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

                        if (!roomUniqueIDs.ContainsKey(room))
                        {
                            roomUniqueIDs.Add(room, ID_Incrementer);
                            ID_Incrementer++; 
                        }
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

            // Set propagation cost and osbtruction (if applicable) to max value before the first correct values are calculated on LateUpdate.
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

        public void AddCurrentListenerRoom(SpatialAudioRoom room)
        {
            if (currentListenerRooms.Contains(room))
            {
                int oldIndex = currentListenerRooms.IndexOf(room);
                currentListenerRooms.RemoveAt(oldIndex);
                currentListenerRooms.Insert(0, room);
            }
            else
            {
                currentListenerRooms.Insert(0, room);
            }
        }

        public void RemoveCurrentListenerRoom(SpatialAudioRoom room)
        {
            if (currentListenerRooms.Contains(room))
            {
                currentListenerRooms.Remove(room);
            }
        }

        #endregion

        #region Registered Instance Update

        void LateUpdate()
        {
            if (!managerInitialized)
                return;

            bool foundListenerPosition = HelperMethods.TryGetListenerPosition(out listenerPosition);

            if (!foundListenerPosition)
                return;

            // Check that a registered instance is still playing and get its positional data.
            CheckRegisteredInstanceValidity();
            UpdateInstancePositionalData();

            /* Find registered instances that are within hearing distance from the listener, as we don't want to run spatial audio calculations 
             * for sounds that are inaudible anyway. If the propagation cost mode is active, then also store the calculated listener-emitter distances, 
             * since they will be again needed later on during the check protocol. */
            audibleInstances.Clear();
            instanceToListenerDistances.Clear();
            CheckInstanceAudibility(audibleInstances, instanceToListenerDistances);

            if (activeModes.HasFlag(ActiveModes.PropagationCost) && currentListenerRooms != null && currentListenerRooms.Count > 0)
            {
                currentListenerRoom = GetHighestPriorityRoom();

                /* Try to find the current room of each registree an put aside the instances for which the lookup failed.
                 * If the occupied room was known last frame, a new room lookup is only performed when the position of the registree has changed. */
                instancesWithKnownRoom.Clear();
                instancesWithUnknownRoom.Clear();
                UpdateRegisteredInstanceRoom(instancesWithKnownRoom, instancesWithUnknownRoom);

                /* If the registree room cannot be determined, we will remove all propagation cost and obsruction from it.
                 * This is a lesser evil than potentially missing hearing some crucial audio, such as dialogue that should be audible */
                ResetPropagationCost(instancesWithUnknownRoom);
                if (activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    ResetObstruction(instancesWithUnknownRoom);
                }

                // Split the obtained relevant registered instances to those which are in the current listener room and to those located in some other rooms.
                instancesInListenerRoom.Clear();
                instancesInOtherRooms.Clear();
                DivideInstancesByListenerRelativePosition(instancesWithKnownRoom, instancesInListenerRoom, instancesInOtherRooms, currentListenerRoom);

                // Remove any previously added propagation cost from registered instances that are now in the same room with the listener.
                ResetPropagationCost(instancesInListenerRoom);

                // Calculate propagation cost for instances in non-listener rooms.                  
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

            for (int i = 0; i < currentListenerRooms.Count; i++)
            {
                var room = currentListenerRooms[i];

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

        private void UpdateRegisteredInstanceRoom(List<RoomAwareInstance> instancesWithKnownRoom, List<RoomAwareInstance> instancesWithUnknownRoom)
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
                            // Remove the previously known room, as we cannot know what has happened when the instance has moved.
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

        private void CheckInstanceAudibility(List<RoomAwareInstance> listForAudibleInstances, Dictionary<RoomAwareInstance, float> listenerToEmitterDistances)
        {
            foreach (var instance in registeredInstances)
            {
                float listenerToEmitterDistance = Vector3.Distance(listenerPosition, instance.currentPosition);

                if (listenerToEmitterDistance <= instance.maxDistance)
                {
                    listForAudibleInstances.Add(instance);

                    if (activeModes.HasFlag(ActiveModes.PropagationCost))
                    {
                        listenerToEmitterDistances.Add(instance, listenerToEmitterDistance);
                    }
                }
            }
        }

        private void DivideInstancesByListenerRelativePosition(List<RoomAwareInstance> allRelevantInstances, 
                                                               List<RoomAwareInstance> instancesInListenerRoom, 
                                                               List<RoomAwareInstance> instancesInOtherRooms, 
                                                               SpatialAudioRoom currentListenerRoom)
        {
            foreach (var instance in allRelevantInstances)
            {
                if (instance.currentRoom == currentListenerRoom)
                {
                    instancesInListenerRoom.Add(instance);
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

        private void CheckObstruction(List<RoomAwareInstance> instances)
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
            float obstruction = SpatialAudioObstructionChecker.ObstructionCheck(listenerPosition, emitterPostion, obstructionLayerMask, 
                                                                                obstructionQueryTriggers, obstructionRaycastSpread, 
                                                                                debug);
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
            if (validSpatialAudioRooms.Count > 1000)
            {
                Debug.LogError("The maximum number of rooms is 1000.");
                return;
            }

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

                        // Don't create a room pair if no valid routes were found.
                        if (routeAlternatives.Count == 0)
                            continue;

                        uint roomA_ID = roomUniqueIDs[roomA];
                        uint roomB_ID = roomUniqueIDs[roomB];
                        string roomPairID = roomA_ID.ToString("D3") + roomB_ID.ToString("D3");

                        if (relevantRoomPairsByRoom.ContainsKey(roomA))
                        {
                            var keyList = relevantRoomPairsByRoom[roomA];
                            keyList.Add(roomPairID);
                        }
                        else
                        {
                            var keyList = new List<string>();
                            keyList.Add(roomPairID);
                            relevantRoomPairsByRoom.Add(roomA, keyList);
                        }

                        var roomPair = new RoomPair(roomA, roomB, routeAlternatives);
                        roomPairs.Add(roomPairID, roomPair);
                    }
                }
            }
        }

        private void CalculatePropagationCosts(List<RoomAwareInstance> instances)
        {
            foreach (var instance in instances)
            {
                Vector3 instancePosition = instance.currentPosition;
                float directRouteLength = instanceToListenerDistances[instance];
                bool foundPair = TryGetMatchingRoomPair(instance.currentRoom, currentListenerRoom, out RoomPair roomPair);

                if (!foundPair || roomPair.routeAlternatives.Count == 0)
                {
                    SetParameterValue(instance.propagationID, Parameters.PropagationCostMaxValue, instance.eventInstance); 
                    continue;
                }

                portalClosestPoints.Clear();
                GetClosestPointsOnPortals(instancePosition, listenerPosition, portalClosestPoints);

                float lowestCost = float.MaxValue;
                float routeLength = float.MaxValue;
                distancesThroughArrivalPortals.Clear();

                for (int i = 0; i < roomPair.routeAlternatives.Count; i++)
                {
                    var routeAlternative = roomPair.routeAlternatives[i];
                    routeNodes.Clear();
                    GetRouteNodes(instancePosition, listenerPosition, routeAlternative, portalClosestPoints, routeNodes);
                    GetRouteCostAndDistance(routeNodes, out float cost, out float distance);

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

                        if (!portalClosestPoints.ContainsKey(arrivalPortal))
                            continue;

                        Vector3 arrivalPoint = portalClosestPoints[arrivalPortal];
                        Vector3 direction = (arrivalPoint - listenerPosition).normalized;
                        float distance = distancesThroughArrivalPortals[arrivalPortal];
                        Vector3 virtualPosition = listenerPosition + direction * distance;
                        Debug.DrawLine(listenerPosition, virtualPosition, Color.cyan);
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

        private bool TryGetMatchingRoomPair(SpatialAudioRoom emitterRoom, SpatialAudioRoom listenerRoom, out RoomPair roomPair)
        {
            roomPair = null;

            if (!relevantRoomPairsByRoom.TryGetValue(emitterRoom, out List<string> keyList))
                return false;
           
            foreach (var key in keyList)
            {
                if (roomPairs.TryGetValue(key, out RoomPair pair))
                {
                    if (pair.IsMatch(emitterRoom, listenerRoom))
                    {
                        roomPair = pair;
                        return true;
                    }
                }
            }

            return false;
        }

        private void GetClosestPointsOnPortals(Vector3 instancePosition, Vector3 listenerPosition, Dictionary<SpatialAudioPortal, Vector3> closestPoints)
        {
            Vector3 direction = (listenerPosition - instancePosition).normalized;
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
                        Vector3 closestFromListener = portal.portalCollider.ClosestPoint(listenerPosition);

                        float distanceThroughInstanceCp = GetIndirectDistance(instancePosition, closestFromInstance, listenerPosition);
                        float distanceThroughListenerCp = GetIndirectDistance(instancePosition, closestFromListener, listenerPosition);

                        if (distanceThroughInstanceCp <= distanceThroughListenerCp)
                        {
                            portalClosestPoints.Add(portal, closestFromInstance);
                        }
                        else
                        {
                            portalClosestPoints.Add(portal, closestFromListener);
                        }
                    }
                }
            }
        }

        private void GetRouteNodes(Vector3 instancePosition, Vector3 listenerPosition, List<SpatialAudioPortal> routePortals, in Dictionary<SpatialAudioPortal, Vector3> closestPoints, List<Node> routeNodes)
        {
            routeNodes.Add(new Node(Node.NodeType.Emitter, instancePosition));

            for (int i = 0; i < routePortals.Count; i++)
            {
                var portal = routePortals[i];

                if (portal != null && closestPoints.ContainsKey(portal))
                {
                    var nodeType = Node.NodeType.Portal;
                    Vector3 position = closestPoints[portal];
                    float portalClosednessCost = nodeType == Node.NodeType.Portal ? portal.PortalStatus : 0f;
                    float traversalMaxCost = nodeType == Node.NodeType.Portal ? portal.maxClosednessCost : 0f;
                    routeNodes.Add(new Node(nodeType, position, portalClosednessCost, traversalMaxCost));
                }
            }

            routeNodes.Add(new Node(Node.NodeType.Listener, listenerPosition));
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

        private float GetIndirectDistance(Vector3 instancePosition, Vector3 portalPosition, Vector3 listenerPosition)
        {
            float instanceToPortal = Vector3.Distance(instancePosition, portalPosition);
            float portalToListener = Vector3.Distance(portalPosition, listenerPosition);
            return instanceToPortal + portalToListener;
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

        private void GetRouteCostAndDistance(List<Node> routeNodes, out float cost, out float distance)
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
                cost += portalDiffraction + nodeB.portalClosednessStatus * nodeB.maxClosednessCost;
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