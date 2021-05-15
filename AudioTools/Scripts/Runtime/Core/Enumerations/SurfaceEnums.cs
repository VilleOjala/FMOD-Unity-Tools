// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

namespace AudioTools
{
    // The listings below are just for the sake of an example - replace them with what is relevant for your game.
    public enum SurfaceType
    {
        Concrete,
        Grass,
        Gravel,
        Sand,
        Snow,
        Parquet,
        Wood,
        Water,
        Metal,
        ManholeCover
    }

    public enum SurfaceLayerType
    {
        None, // <-- Exception: Do not delete or alter this first value!
        Wet,
        Foliage,
        Shards,
        Blood,
    }
}