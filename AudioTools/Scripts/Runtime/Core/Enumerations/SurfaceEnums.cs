// Audio Implementation Tools for FMOD and Unity
// Copyright 2020, Ville Ojala, All rights reserved.

namespace AudioTools
{
    // The listings below are just for the sake of example - replace them with what is relevant for your game.
    public enum SurfaceType
    {
        Concrete = 0,
        Grass = 1,
        Gravel = 2,
        Sand = 3,
        Snow = 4,
        Parquet = 5,
        Wood = 6,
        Water = 7,
        Metal = 8,
        ManholeCover = 9
    }

    public enum SurfaceLayerType
    {
        None = 0, // <-- Exception: Do not delete or alter this first value!
        Wet = 1,
        Foliage = 2,
        Shards = 3,
        Blood = 4,
    }
}