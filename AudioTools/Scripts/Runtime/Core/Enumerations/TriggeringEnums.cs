// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

namespace AudioTools
{
    public enum TriggerOn
    {
        None,
        Start,
        OnDisable,
        OnDestroy,
        OnTriggerEnter,
        OnTriggerExit
    }

    public enum TriggeringType
    {
        Event,
        DirectReference
    }

    public enum TriggeringAction
    {
        StartSound,
        StopSound,
        StopPersistentSound,
        StopAllPersistentSounds
    }

    public enum RequiredTags
    {
        Player,
        NonPlayer,
        AnyTagged,
        Custom,
        None 
    }

    public enum TriggererType
    {
        Nothing,
        Player,
        NonPlayer,
        Custom 
    }
}