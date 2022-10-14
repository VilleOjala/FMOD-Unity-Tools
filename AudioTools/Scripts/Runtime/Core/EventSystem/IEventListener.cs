// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

namespace FMODUnityTools
{
    public interface IEventListener
    {
        public void EventReceived(EventArguments eventArgs);
    }
}