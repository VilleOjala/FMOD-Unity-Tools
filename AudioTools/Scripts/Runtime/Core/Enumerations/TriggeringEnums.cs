// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

namespace AudioTools
{
    public enum TriggerOn
    {
        None = 0,
        Start = 1,
        OnDisable = 2,
        OnDestroy = 3,
        OnTriggerEnter = 4,
        OnTriggerExit = 5
    }

    public enum TriggeringType
    {
        Event = 0,
        DirectReference = 1
    }

    public enum TriggeringAction
    {
        StartSound = 0,
        StopSound = 1,
        StopPersistentSound = 2,
        StopAllPersistentSounds = 3
    }

    public enum RequiredTags
    {
        Player = 0,
        NonPlayer = 1,
        AnyTagged = 2,
        Custom = 3,
        None = 4
    }

    public enum TriggererType
    {
        Nothing = 0,
        Player = 1,
        NonPlayer = 2,
        Custom = 3
    }
}