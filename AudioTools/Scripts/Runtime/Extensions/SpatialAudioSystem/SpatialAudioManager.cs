// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Spatial Audio System/Spatial Audio Manager")]
    public class SpatialAudioManager : MonoBehaviour
    {
        #region Declarations & Initializations

        public static SpatialAudioManager Instance { get; private set; }
        
        private const string ObstructionParameter = "Obstruction"; // A local parameter with exactly this name needs to created inside the FMOD Studio project.
        PARAMETER_ID obstructionID;
        private const int ObstructionMinValue = 0; // <- The local parameter min value needs to be set as '0' inside the FMOD Studio project. 
        private const int ObstructionMaxValue = 1; //  <- The local parameter max value needs to be set as '1' inside the FMOD Studio project.

        private const string PropagationCostParameter = "PropagationCost";        
        PARAMETER_ID propagationID;    
        private const int CostMinValue = 0; // <- The local parameter min value needs to be set as '0' inside the FMOD Studio. 
        private const int CostMaxValue = 1; //  <- The local parameter max value needs to be set as '1' inside the FMOD Studio.

        public ActiveModes activeModes;
        public DebugDrawModes debugDrawModes;

        public LayerMask obstructionLayerMask;
        public QueryTriggerInteraction obstructionQueryTriggers = QueryTriggerInteraction.Ignore;

        [SerializeField, Tooltip("Collider to ignore when checking for obstruction.")]
        private Collider ignoreSelfCollider;
        public Collider IgnoreSelfCollider { get => ignoreSelfCollider; set => ignoreSelfCollider = value; }

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
        private List<SpatialAudioRoom> validSpatialAudioRooms = new List<SpatialAudioRoom>();
        private List<SpatialAudioRoom> currentPlayerRooms = new List<SpatialAudioRoom>();
        private SpatialAudioRoom currentPlayerRoom;
        private List<RoomAwareInstance> registeredInstances = new List<RoomAwareInstance>();
        private Vector3 playerPosition;
        private SpatialAudioNode playerNode = new SpatialAudioNode() { isPlayer = true };

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

        //  During the propagation cost calculations having these relations already pre-stored allows for faster sorting of raycast hit data.
        // <- i.e. not having to do a bunch of 'GetComponent' calls etc.
        private Dictionary<BoxCollider, SpatialAudioPortal> colliderToPortalData = new Dictionary<BoxCollider, SpatialAudioPortal>();

        // Used for a fast node lookup during the propagation cost calculations. 
        private Dictionary<SpatialAudioPortal, SpatialAudioNode> portalToNodeData = new Dictionary<SpatialAudioPortal, SpatialAudioNode>();

        // If the 'Require Obstruction Tag' -mode is active, the Spatial Audio Obstruction Tags populate this list with their associated colliders. 
        private HashSet<Collider> obstructingColliders = new HashSet<Collider>();

        // The maximum number of rooms that a sound route can pass through before it is automatically terminated. 
        private const int CheckLevelLimit = 6;

        private List<RoomAwareInstance> instancesWithKnownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesWithUnknownRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> audibleInstances = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInPlayerRoom = new List<RoomAwareInstance>();
        private List<RoomAwareInstance> instancesInOtherRooms = new List<RoomAwareInstance>();
        private Dictionary<RoomAwareInstance, float> instanceToPlayerDistances = new Dictionary<RoomAwareInstance, float>();

        // When calculating the diffraction angle for a given portal, the angle will be set to zero if the magnitude of either direction vector is below this value.
        // This is done to prevent the bugs/edge cases in angle calculations when the player or an emitter is crossing a room boundary. 
        // <- Using very short direction vectors may cause sudden unwanted jumps/artefact in the calculated diffraction values.
        private float minimumVectorMagnitude = 0.05f;

        public float CheckInterval { get; set; } = 0.1f;
        private float timer = 0.0f;
        private bool forceCheckNextLateUpdate = false;
        private bool managerInitialized = false;

        // Currently, the tool only works properly when this is set as false.
        // The experimental mode is for emitter virtual position calculation testing to make sound appear to be eminating from the directions of openings
        // <- FMOD does not currently support dynamic multipositioning of event instances (the transceiver plugin is not really suitable)
        // Emitter max distance scaling is not used in the experimental mode
        private bool experimentalMode = false; 

        [System.Flags]
        public enum ActiveModes
        {
            PropagationCost,
            Obstruction
        }

        [System.Flags]
        public enum DebugDrawModes
        {
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
        }

        private class SpatialAudioNode
        {
            public bool isInstance = false;
            public RoomAwareInstance roomAwareInstance;
            public SpatialAudioPortal nodePortal;
            public bool isPlayer = false;
        }

        private class SpatialAudioRoute
        {
            public List<SpatialAudioNode> routePoints = new List<SpatialAudioNode>();
            public HashSet<SpatialAudioRoom> visitedRooms = new HashSet<SpatialAudioRoom>();
            public SpatialAudioNode cacheLastNode;
            public float routeLength;

            //Icluded only for the "experimental mode" testing purposes
            public bool arrivedThroughOpening = false;
            public Vector3 arrivalPoint;
            public Vector3 virtualPosition;
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
                    var connectedRooms = FindAllConnectedRooms(validSpatialAudioRooms[i]);

                    if (connectedRooms != null && connectedRooms.Count > 0)
                    {
                        orderedConnections.Add(validSpatialAudioRooms[i], connectedRooms);
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

                            if (!portalToNodeData.ContainsKey(portal))
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
             
                var result = RuntimeManager.StudioSystem.getParameterDescriptionByName(PropagationCostParameter, out PARAMETER_DESCRIPTION desc);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError(result);
                }

                propagationID = desc.id;          
            }

            if (activeModes.HasFlag(ActiveModes.Obstruction))
            {
                var result = RuntimeManager.StudioSystem.getParameterDescriptionByName(ObstructionParameter, out PARAMETER_DESCRIPTION desc);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError(result);
                }

                obstructionID = desc.id;
            }

            managerInitialized = true;
        }

        private SpatialAudioNode CreatePortalNode(SpatialAudioPortal portal)
        {
            var portalNode = new SpatialAudioNode();
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
            if (Instance != null)
            {
                if (Instance.activeModes.HasFlag(ActiveModes.PropagationCost))
                {
                    SetParameterValue(propagationID, CostMaxValue, eventInstance);
                }

                if (Instance.activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    SetParameterValue(obstructionID, ObstructionMaxValue, eventInstance);
                }
            }

            registeredInstances.Add(roomAwareInstance);
            forceCheckNextLateUpdate = true;
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

            timer += Time.deltaTime;

            if (timer > CheckInterval || forceCheckNextLateUpdate)
            {
                timer = 0.0f;
                forceCheckNextLateUpdate = false;

                bool foundPlayerPosition = HelperMethods.TryGetListenerPosition(out playerPosition);

                if (!foundPlayerPosition)
                {
                    //TODO: Handle somehow?
                    return;
                }

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

                // If only the obstruction mode is active, check for obstruction for all audible registred instances.
                if (!activeModes.HasFlag(ActiveModes.PropagationCost) && activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    CheckObstruction(audibleInstances);
                }

                if (activeModes.HasFlag(ActiveModes.PropagationCost) && currentPlayerRooms != null && currentPlayerRooms.Count > 0)
                {
                    currentPlayerRoom = currentPlayerRooms[0];

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

                    // Calculate obstruction for instances located in the player room if the obstruction mode is active.
                    if (activeModes.HasFlag(ActiveModes.Obstruction))
                    {
                        CheckObstruction(instancesInPlayerRoom);
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
            for (int i = 0; i < validSpatialAudioRooms.Count; i++)
            {
                // If the spatial audio room geometry has been set correctly the trigger collider areas of different rooms should only minimally overlap.
                // <- i.e. early out, since the system does not currently support nested rooms.
                for (int j = 0; j < validSpatialAudioRooms[i].colliders.Count; j++)
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
                        currentRoom = room;
                        return currentRoom;
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
                Debug.LogError(result); 
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

                CalculateAndSetObstruction(instance.eventInstance, instance.currentPosition);
            }
        }

        private void CheckObstruction(params RoomAwareInstance[] instances)
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                CalculateAndSetObstruction(instance.eventInstance, instance.currentPosition);
            }
        }

        private void CalculateAndSetObstruction(EventInstance eventInstance, Vector3 emitterPostion)
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

        private void ResetObstruction(params RoomAwareInstance[] instances)                
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                SetParameterValue(obstructionID, ObstructionMinValue, instance.eventInstance);
            }
        }

        private void ResetObstruction(List<RoomAwareInstance> instances) 
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                SetParameterValue(obstructionID, ObstructionMinValue, instance.eventInstance);
            }
        }

        #endregion

        #region Propagation Cost

        //Reusable data structures for the "CalculatePropagationCost" -method to reduce the GC overhead.
        private Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints = new Dictionary<SpatialAudioPortal, Vector3>();
        private List<SpatialAudioRoute> traversalRoutes = new List<SpatialAudioRoute>();
        private Dictionary<SpatialAudioPortal, Vector3> portalHitData = new Dictionary<SpatialAudioPortal, Vector3>();

        private void CalculatePropagationCost(List<RoomAwareInstance> instances)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                RoomAwareInstance instance = instances[i];
                SpatialAudioRoom instanceRoom = instance.currentRoom;

                portalHitData.Clear();
                UpdatePortalHitData(instance.currentPosition, playerPosition, ref portalHitData);
                portalClosestPoints.Clear();
                traversalRoutes.Clear();
                // Find and store routes from the instance towards player through portals.
                FindTraversalRoutes(instanceRoom, in portalHitData, instance.maxDistance, ref portalClosestPoints, instance, ref traversalRoutes);

                // Filter out any routes that did not reach the player.
                for (int j = traversalRoutes.Count - 1; j > -1; j--)
                {
                    SpatialAudioRoute route = traversalRoutes[j];
                    SpatialAudioNode lastNode = route.routePoints[route.routePoints.Count - 1];

                    if (!lastNode.isPlayer)
                    {
                        traversalRoutes.RemoveAt(j);
                    }
                }

                // If no route to the player was found, apply full propagation cost.
                if (traversalRoutes.Count < 1)
                {
                    if (activeModes.HasFlag(ActiveModes.Obstruction))
                    {
                        if (obstructionCheckThreshold >= CostMaxValue)
                        {
                            CheckObstruction(instance);
                        }
                        else
                        {
                            ResetObstruction(instance);
                        }
                    }
                    SetParameterValue(propagationID, CostMaxValue, instance.eventInstance);
                    continue;
                }

                // EXPERIMENTING WITH THE EMITTER VIRTUAL POSITION CALCULATIONS ->     
                if (experimentalMode)
                {
                    FindSoundArrivalPoints(ref traversalRoutes, in portalClosestPoints);
                    CalculateEmitterVirtualPositions(in traversalRoutes);

                    #if UNITY_EDITOR
                    VisualizeVirtualPositions(in traversalRoutes);
                    #endif
                }
                // <- EXPERIMENTING WITH THE EMITTER VIRTUAL POSITION CALCULATIONS

                // Calculate the propagation cost to the player for each route and pick the one with the lowest value.
                // If multiple routes have the same total propagation cost, pick the one with the shortest traversal distance.
                SpatialAudioRoute lowestPropagationCostRoute = null;
                float lowestPropagationCost = float.MaxValue;

                for (int j = 0; j < traversalRoutes.Count; j++)
                {
                    SpatialAudioRoute route = traversalRoutes[j];
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
                            if (route.routeLength < lowestPropagationCostRoute.routeLength)
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

                if (debugDrawModes.HasFlag(DebugDrawModes.PropationCost))
                {
                    DrawSoundPath(lowestPropagationCostRoute, portalClosestPoints);
                }
                
                if (activeModes.HasFlag(ActiveModes.Obstruction))
                {
                    if (obstructionCheckThreshold >= lowestPropagationCost)
                    {
                        CheckObstruction(instance);
                    }
                    else
                    {
                        ResetObstruction(null, instance);
                    }
                }

                // Assign the calculated propagation cost to the EventInstance.
                SetParameterValue(propagationID, lowestPropagationCost, instance.eventInstance);

                if (experimentalMode) 
                    continue;

                // Modify the fall-off distance of the instance to take into account the (potentially) longer route traversed. 
                instanceToPlayerDistances.TryGetValue(instance, out float directDistance);
                ScaleFalloffDistance(directDistance, lowestPropagationCostRoute.routeLength, instance);
            }
        }

        private void ResetPropagationCost(List<RoomAwareInstance> instances)
        {
            if (instances == null)
                return;

            foreach (var instance in instances)
            {
                if (instance == null)
                    continue;

                SetParameterValue(propagationID, CostMinValue, instance.eventInstance);

                if (experimentalMode)
                    continue;

                instance.eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, instance.maxDistance);
            }
        }

        private void UpdatePortalHitData(Vector3 instancePosition, Vector3 playerPosition, ref Dictionary<SpatialAudioPortal, Vector3> hitData)
        {
            Vector3 direction = (playerPosition - instancePosition).normalized;
            var ray = new Ray(instancePosition, direction);

            foreach (var item in colliderToPortalData)
            {
                if (item.Key != null && item.Value != null && !hitData.ContainsKey(item.Value))
                {
                    var collider = item.Key;
                    var portal = item.Value;

                    if (collider.bounds.IntersectRay(ray, out float distance))
                    {
                        Vector3 hitPoint = instancePosition + (direction * distance);
                        hitData.Add(portal, hitPoint);
                    }
                }
            }
        }

        //Reusable data structures for the "FindTraversalRoutes" -method to reduce the GC overhead.
        private List<SpatialAudioPortal> startingRoomPortals = new List<SpatialAudioPortal>();
        private List<SpatialAudioPortal> portalsToNewUnvisitedRooms = new List<SpatialAudioPortal>();
        private Dictionary<SpatialAudioPortal, float> newPortalsByDistance = new Dictionary<SpatialAudioPortal, float>();

        private void FindTraversalRoutes(SpatialAudioRoom startingRoom, in Dictionary<SpatialAudioPortal, Vector3> portalHitData, float routeMaxLength, 
                                         ref Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints, RoomAwareInstance instance,
                                         ref List<SpatialAudioRoute> traversalRoutes)
        {
            var instanceNode = new SpatialAudioNode();
            
            instanceNode.isInstance = true;
            instanceNode.roomAwareInstance = instance;

            // Get all the portals in the instance's room.
            startingRoomPortals.Clear();
            GetStartingRoomPortals(startingRoom, ref startingRoomPortals);

            if (startingRoomPortals.Count < 1)
                return;

            // For each portal, find the point traversing through which gives the shortest distance from the emitter to the player.
            // First, check the raycast data obtained earlier: if the portal was hit, store the hit point on the portal.
            // If the portal was not hit, calculate the closest point on the portal from both the player's and the emitter's position.
            // Of the two points, select and store the one which results in the lowest overall distance when we pass through it on route from the sound to the player.
            for (int i = 0; i < startingRoomPortals.Count; i++)
            {
                SpatialAudioPortal portal = startingRoomPortals[i];

                if (portalHitData.ContainsKey(portal))
                {
                    portalClosestPoints.Add(portal, portalHitData[portal]);
                }
                else
                {
                    CalculatePortalClosestPoint(portal, instance.currentPosition, ref portalClosestPoints);
                }

                // Check that the closest point on portal is within the maximum rolloff distance of the emitter.
                // If true, start a new route from that portal towards the player.
                // Store the route distance.
                float instanceToPortalDistance = Vector3.Distance(instance.currentPosition, portalClosestPoints[portal]);

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

            // Keep extending the routes until:
            // 1. They reach the player OR
            // 2. Their length exceeds the maximum rolloff distance of the emitter OR
            // 3. They cannot find any new rooms to visit OR
            // 4. The number of rooms visited exceeds the set max number (checkLevelLimit);
            for (int i = 0; i < traversalRoutes.Count; i++) 
            {
                SpatialAudioRoute route = traversalRoutes[i];

                while (route.routeLength < routeMaxLength && !CheckIfLastNodeIsPlayer(route) && 
                       route.visitedRooms.Count <= CheckLevelLimit && NewNodeHasBeenAdded(route))
                {
                    SpatialAudioNode lastNode = route.routePoints[route.routePoints.Count - 1];

                    // There should always be just two, process the one that has not been yet visited
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

                    if (unvisitedRoomFound)
                    {
                        SpatialAudioRoom currentUnvisitedRoom = unvisitedRoom;

                        if (currentUnvisitedRoom == currentPlayerRoom)
                        {
                            Vector3 portalPosition = portalClosestPoints[lastNode.nodePortal];
                            float distanceToPlayer = Vector3.Distance(portalPosition, playerPosition);
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
                        // From the current unvisited room, find the portals that lead to yet another unvisited rooms. 
                        portalsToNewUnvisitedRooms.Clear();
                        GetPortalsToUnvisitedRooms(currentUnvisitedRoom, in route.visitedRooms, ref portalsToNewUnvisitedRooms);
                        route.visitedRooms.Add(currentUnvisitedRoom);

                        // Temporarily store the new valid portals paired with their total route length. 
                        newPortalsByDistance.Clear();

                        for (int k = 0; k < portalsToNewUnvisitedRooms.Count; k++)
                        {
                            SpatialAudioPortal newPortal = portalsToNewUnvisitedRooms[k];

                            // Similarly to what was done earlier, find the closest traversal points on new portals.
                            // <- Only do the check if the traversal point for that portal has not yet been determined.
                            if (!portalClosestPoints.ContainsKey(newPortal))
                            {
                                if (portalHitData.ContainsKey(newPortal))
                                {
                                    portalClosestPoints.Add(newPortal, portalHitData[newPortal]);
                                }
                                else
                                {
                                    CalculatePortalClosestPoint(newPortal, instance.currentPosition, ref portalClosestPoints);
                                }
                            }

                            // Calculate the distance from the previous route node portal to the new portal
                            SpatialAudioNode previousNode = route.routePoints[route.routePoints.Count - 1];
                            Vector3 previousNodePortalClosestPoint = portalClosestPoints[previousNode.nodePortal];
                            Vector3 newPortalClosestPoint = portalClosestPoints[newPortal];              
                            float distance = Vector3.Distance(previousNodePortalClosestPoint, newPortalClosestPoint);
                            float totalRouteLength = route.routeLength + distance;

                            // Add the new portal to the route extension list if traversing to it does not result 
                            // in exceeding the maximum rolloff distance of the emitter.
                            if (totalRouteLength <= routeMaxLength)
                            {
                                newPortalsByDistance.Add(newPortal, totalRouteLength);
                            }
                        }
                            
                        // If the number of new valid portals for route extension is more than one, 
                        // create deep copies of the original route and add those to the 'travelsalRoutes' -list.
                        //  <- One of the portals always extends the current existing route.
                        // If no new valid portals were found, the last node and the cached last node of the route 
                        // are set as the same in order to terminate the 'while' -loop.
                        if (newPortalsByDistance.Count == 0)
                        {
                            route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                        }
                        else if (newPortalsByDistance.Count == 1)
                        {
                            foreach (var portal in newPortalsByDistance)
                            {
                                SpatialAudioNode newNode = portalToNodeData[portal.Key];
                                route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                                route.routePoints.Add(newNode);
                                route.routeLength = portal.Value;
                            }
                        }
                        else if (newPortalsByDistance.Count > 1)
                        {                                
                            int deepCopyNumber = newPortalsByDistance.Count - 1;

                            foreach (var item in newPortalsByDistance)
                            {
                                if (deepCopyNumber > 0)
                                {
                                    var routeCopy = new SpatialAudioRoute();
                                    var routePointsCopy = new List<SpatialAudioNode>(route.routePoints);
                                    var visitedRoomsCopy = new HashSet<SpatialAudioRoom>(route.visitedRooms);
                                    routeCopy.routePoints = routePointsCopy;
                                    routeCopy.visitedRooms = visitedRoomsCopy;
                                    routeCopy.cacheLastNode = routeCopy.routePoints[routeCopy.routePoints.Count - 1];

                                    SpatialAudioNode newNode = portalToNodeData[item.Key];
                                    routeCopy.routePoints.Add(newNode);
                                    routeCopy.routeLength = item.Value;
                                    traversalRoutes.Add(routeCopy);
                                    deepCopyNumber--;                                          
                                }
                                else
                                {
                                    SpatialAudioNode newNode = portalToNodeData[item.Key];
                                    route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                                    route.routePoints.Add(newNode);
                                    route.routeLength = item.Value;
                                }
                            }
                        }                      
                    }
                    else
                    {
                        route.cacheLastNode = route.routePoints[route.routePoints.Count - 1];
                    }
                }
            }
        }

        public void GetStartingRoomPortals(SpatialAudioRoom startingRoom, ref List<SpatialAudioPortal> startingRoomPortals)
        {
            for (int i = 0; i < startingRoom.roomConnections.Count; i++)
            {
                var roomConnection = startingRoom.roomConnections[i];

                for (int j = 0; j < roomConnection.connectingPortals.Length; j++)
                {
                    SpatialAudioPortal portal = roomConnection.connectingPortals[j];

                    if (!startingRoomPortals.Contains(portal))
                    {
                        startingRoomPortals.Add(portal);
                    }
                }                
            }
        }

        private void CalculatePortalClosestPoint(SpatialAudioPortal portal, Vector3 instancePosition, ref Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints)
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

        private float GetIndirectDistance(Vector3 instancePosition, Vector3 portalPosition, Vector3 playerPosition)
        {
            float instanceToPortal = Vector3.Distance(instancePosition, portalPosition);
            float portalToPlayer = Vector3.Distance(portalPosition, playerPosition);
            return instanceToPortal + portalToPlayer; 
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

        private void GetPortalsToUnvisitedRooms(SpatialAudioRoom room, in HashSet<SpatialAudioRoom> visitedRooms, ref List<SpatialAudioPortal> newPortals)
        {
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
                    posA = nodeA.roomAwareInstance.currentPosition;
                }
                else { portalPositions.TryGetValue(nodeA.nodePortal, out posA); }

                // Node B is always a portal
                portalPositions.TryGetValue(nodeB.nodePortal, out posB);

                // Node C can be either a portal or the player
                if (nodeC.isInstance)
                {
                    posC = nodeC.roomAwareInstance.currentPosition;
                }
                else 
                {
                    posC = playerPosition;   
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
                var result = instance.eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, scaledRolloff);

                if (result != FMOD.RESULT.OK)
                {
                    Debug.LogError(result);
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

                if (n1.isInstance) { pos1 = n1.roomAwareInstance.currentPosition; }
                else if (n1.isPlayer) { pos1 = playerPosition; }
                else { portalPositions.TryGetValue(n1.nodePortal, out pos1); }

                if (n2.isInstance) { pos2 = n2.roomAwareInstance.currentPosition; }
                else if (n2.isPlayer) { pos2 = playerPosition; }
                else { portalPositions.TryGetValue(n2.nodePortal, out pos2); }

                Debug.DrawLine(pos1, pos2, Color.magenta, CheckInterval);
            }
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

        #region Experimental

        private void FindSoundArrivalPoints (ref List<SpatialAudioRoute> routesToPlayer, 
                                             in Dictionary<SpatialAudioPortal, Vector3> portalClosestPoints)
        {
            for (int i = 0; i < routesToPlayer.Count; i++)
            {
                SpatialAudioRoute route = routesToPlayer[i];

                if (route == null)
                    continue;

                SpatialAudioPortal arrivalPortal = route.cacheLastNode.nodePortal; 

                if (arrivalPortal.portalType == SpatialAudioPortal.PortalType.Opening && arrivalPortal.PortalStatus < 1) // temp condition for testing
                {
                    bool pointFound = portalClosestPoints.TryGetValue(arrivalPortal, out Vector3 arrivalPoint);

                    if (pointFound)
                    {
                        route.arrivedThroughOpening = true;
                        route.arrivalPoint = arrivalPoint;                        
                    }
                }
            }
        }
  
        private void CalculateEmitterVirtualPositions(in List<SpatialAudioRoute> routesToPlayer)
        {
            for (int i = 0; i < routesToPlayer.Count; i++)
            {
                SpatialAudioRoute route = routesToPlayer[i];

                if (route == null || !route.arrivedThroughOpening)
                    continue;

                Vector3 directionToVirtualPosition = (route.arrivalPoint - playerPosition).normalized;
                route.virtualPosition = playerPosition + directionToVirtualPosition * route.routeLength;
            }
        }

        private void VisualizeVirtualPositions(in List<SpatialAudioRoute> routesToPlayer)
        {
            for (int i = 0; i < routesToPlayer.Count; i++)
            {
                SpatialAudioRoute route = routesToPlayer[i];

                if (route == null || !route.arrivedThroughOpening)
                    continue;

                Debug.DrawLine(playerPosition, route.virtualPosition, Color.white, CheckInterval);
            }
        }
        
        #endregion
    }
}