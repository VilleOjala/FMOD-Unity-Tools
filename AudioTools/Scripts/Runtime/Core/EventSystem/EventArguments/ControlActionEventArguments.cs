// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using FMODUnity;

namespace FMODUnityTools
{
    public class ControlActionEventArguments : EventArguments
    {
        public ControlAction controlAction;
        public ParamRef[] parameters;

        public ControlActionEventArguments(EventTag eventTag, ControlAction controlAction, params ParamRef[] parameters) : base(eventTag)
        {
            this.controlAction = controlAction;
            this.parameters = parameters;
        }
    }
}
