// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

namespace AudioTools
{
    public enum MovementType
    {
        // -> Basic movement types used by the Footstep System, do not delete.

        Walk = 0,
        Run = 1,
        WalkCrouched = 2,
        RunCrouched = 3,

        // Small movement when e.g. player presses WASD key for only a short duration. 
        // Should not produce a proper walk footstep but rather some foley and maybe faint shoe drags/slides etc.
        Sidle = 4,
        SidleCrouched = 5,

        WalkStairsUp = 6,
        WalkStairsDown = 7,             
        RunStairsUp = 8,
        RunStairsDown = 9,

        WalkCrouchedStairsUp = 10,
        WalkCrouchedStairsDown = 11, 
        RunCrouchedStairsUp = 12,
        RunCrouchedStairsDown = 13,

        // <- End of basic movement types, add your own after these.
    }

    // The listings below are just for the sake of example - replace with what is relevant for your game.
    public enum ShoeOrFeetType
    {
        Barefeet = 0,
        Boots = 1,
        Heels = 2,
        Sneakers = 3,
        Monster = 4,
        Robot = 5
    }

    public enum FoleyType
    {
        Naked = 0,
        CombatGear = 1,
        TrackSuit = 2,
        Tuxedo = 3,
        DragonScale = 4
    }
}