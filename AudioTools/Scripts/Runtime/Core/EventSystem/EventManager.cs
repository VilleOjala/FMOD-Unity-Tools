// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using System.Linq;

namespace FMODUnityTools
{
    public static class EventManager
    {
        private static List<WeakReference> listeners = new List<WeakReference>();

        public static void RegisterListener(IEventListener listener)
        {
            listeners.Add(new WeakReference(listener));
        }

        public static void UnregisterListener(IEventListener listener)
        {
            listeners.RemoveAll(x => x.Target == listener);
        }

        public static void PostEvent(EventArguments eventArgs)
        {
            var aliveListeners = from listener in listeners where listener.IsAlive select listener;
            listeners = aliveListeners.ToList();

            foreach (var listener in listeners)
            {
                (listener.Target as IEventListener).EventReceived(eventArgs);
            }
        }
    }
}