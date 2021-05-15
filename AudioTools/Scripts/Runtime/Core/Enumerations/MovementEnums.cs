// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

namespace AudioTools
{
    public enum MovementType
    {
        // -> Basic movement types used by the Footstep System, do not delete.

        Walk,
        Run,
        WalkCrouched,
        RunCrouched,

        // Small movement when e.g. player presses WASD key for only a short duration. 
        // Should not produce a proper walk footstep but rather some foley and maybe faint shoe drags/slides etc.
        Sidle,
        SidleCrouched,

        WalkStairsUp,
        WalkStairsDown,             
        RunStairsUp,
        RunStairsDown,

        WalkCrouchedStairsUp,
        WalkCrouchedStairsDown, 
        RunCrouchedStairsUp,
        RunCrouchedStairsDown,

        // <- End of basic movement types, add your own after these.
    }

    // The listings below are just for the sake of an example - replace with what is relevant for your game.
    public enum ShoeOrFeetType
    {
        Barefeet,
        Boots,
        Heels,
        Sneakers,
        Monster,
        Robot
    }

    public enum FoleyType
    {
        Naked,
        CombatGear,
        TrackSuit,
        Tuxedo,
        DragonScale
    }
}