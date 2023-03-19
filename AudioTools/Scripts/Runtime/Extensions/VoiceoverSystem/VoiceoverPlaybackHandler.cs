// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;
using FMOD.Studio;
using FMODUnity;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Voiceover System/Voiceover Playback Handler")]
    public class VoiceoverPlaybackHandler : MonoBehaviour
    {
        private VoiceoverManager voiceoverManager;
        public Speaker speaker;
        public EventReference voiceoverEvent;
        private EventDescription voiceoverDescription;
        private EventInstance voiceoverInstance;
        public Transform followTransform;       
        public bool spatialAudioRoomAware = false;

        [HideInInspector]
        public SpatialAudioRoom fixedRoom;

        EVENT_CALLBACK voiceoverCallback;
        bool isSpeaking = false;
        string currentDialogue;
        bool coroutineRunning = false;
        bool is3D = false;
        bool initializationSuccesfull = false;

        public bool Initialize(VoiceoverManager manager)
        {
            if (manager == null)
            {
                return false;
            }
            else
            {
                voiceoverManager = manager;
            }

            if (!HelperMethods.TryRetrieveDescriptionIfNotAlreadyValid(voiceoverEvent, ref voiceoverDescription))
            {
                return false;
            }

            voiceoverDescription.is3D(out is3D);
            voiceoverCallback = new EVENT_CALLBACK(VoiceEventCallback);
            initializationSuccesfull = true;
            return true;
        }

        public int PlayVoiceover(string key, string dialogueName, float releaseOffset) 
        {
            if (!initializationSuccesfull || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(dialogueName))
                return -1;

            if (!isSpeaking)
            {
                bool didSucceed = PlayProtocol(key, dialogueName, releaseOffset);

                if (didSucceed)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else if (voiceoverInstance.isValid())
            {
                /* Stop the current dialogue line with a quick fade out and then start the new line.
                 * This both prevents the occurence of clicks / pops and enforces the speaker monophony. */
                voiceoverInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

                if (coroutineRunning)
                {
                    StopAllCoroutines();
                    coroutineRunning = false;
                }

                // Inform VoiceoverManager that the playback handler is busy stopping the previous dialogue line and it should queue the line.
                return 0;        
            }
            else // If we are on a customly assigned pacing wait after a line has stopped playing, let's abort this wait and start the new line immediately.
            {
                if (coroutineRunning)
                {
                    StopAllCoroutines();
                    coroutineRunning = false;
                    isSpeaking = false;
                    currentDialogue = null;
                }

                bool didSucceed = PlayProtocol(key, dialogueName, releaseOffset);

                if (didSucceed)
                    return 1;
                else
                    return -1;
            }
        }

        public void StopVoiceover(FMOD.Studio.STOP_MODE stopMode)
        {
            if (isSpeaking)
            {
                if (voiceoverInstance.isValid())
                {
                    voiceoverInstance.stop(stopMode);

                    if (coroutineRunning)
                    {
                        StopAllCoroutines();
                        coroutineRunning = false;
                    }
                }
                else if (coroutineRunning)
                {
                    StopAllCoroutines();
                    coroutineRunning = false;
                    isSpeaking = false;
                    string latestDialogue = currentDialogue;
                    currentDialogue = null;

                    if (voiceoverManager != null)
                    {
                        voiceoverManager.ReportSpeakerAvailability(speaker, latestDialogue);
                    }
                }
            }
        }

        private bool PlayProtocol(string key, string dialogueName, float releaseOffset)
        {
            voiceoverDescription.createInstance(out voiceoverInstance);
            var stringHandle = GCHandle.Alloc(key, GCHandleType.Pinned);
            voiceoverInstance.setUserData(GCHandle.ToIntPtr(stringHandle));
            voiceoverInstance.setCallback(voiceoverCallback);

            if (is3D)
            {
                Transform transformToFollow;

                if (followTransform != null)
                {
                    transformToFollow = followTransform;
                }
                else
                {
                    transformToFollow = gameObject.transform;
                }

                Rigidbody rb = null;
                RuntimeManager.AttachInstanceToGameObject(voiceoverInstance, transformToFollow, rb);

                if (spatialAudioRoomAware && SpatialAudioManager.Instance != null)
                {
                    SpatialAudioManager.Instance.RegisterRoomAwareInstance(voiceoverInstance, fixedRoom);
                }
            }

            isSpeaking = true;
            currentDialogue = dialogueName;
            
            if (releaseOffset != 0)
            {
                if (TryGetVoiceoverLength(key, out float length))
                {
                    float releaseAfter = length + releaseOffset;

                    if (releaseAfter < 0)
                    {
                        releaseAfter = 0f;
                    }

                    StartCoroutine(WaitBeforeDialogueRelease(releaseAfter));
                    coroutineRunning = true;
                }
            }

            voiceoverInstance.start();
            return true;
        }

        void Update()
        {
            if (isSpeaking)
            {
                if (voiceoverInstance.isValid())
                {
                    voiceoverInstance.getPlaybackState(out PLAYBACK_STATE playbackState);

                    if (playbackState == PLAYBACK_STATE.STOPPED)
                    {
                        voiceoverInstance.release();

                        if (!coroutineRunning)
                        {
                            isSpeaking = false;
                        }
                        if (!string.IsNullOrEmpty(currentDialogue) && !coroutineRunning)
                        {
                            string latestDialogue = currentDialogue;
                            currentDialogue = null;

                            if (voiceoverManager != null)
                            {
                                voiceoverManager.ReportSpeakerAvailability(speaker, latestDialogue);
                            }
                        }
                    }
                }
                else if (!coroutineRunning) 
                {
                    isSpeaking = false;
                }
            }
        }
  
        IEnumerator WaitBeforeDialogueRelease(float time)
        { 
            yield return new WaitForSeconds(time);
            isSpeaking = false;
            coroutineRunning = false;
            string latestDialogue = currentDialogue;
            currentDialogue = null;

            if (voiceoverManager != null)
            {
                voiceoverManager.ReportSpeakerAvailability(speaker, latestDialogue);
            }
        }
        
        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static FMOD.RESULT VoiceEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parametersPtr)
        {
            var instance = new EventInstance(instancePtr); 
            instance.getUserData(out IntPtr stringPtr);
            GCHandle stringHandle = GCHandle.FromIntPtr(stringPtr);
            var key = stringHandle.Target as string;

            switch (type)
            {
                case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                    {
                        var soundMode = FMOD.MODE.LOOP_NORMAL | FMOD.MODE.CREATECOMPRESSEDSAMPLE | FMOD.MODE.NONBLOCKING; 
                        var properties = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parametersPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
                        var result = RuntimeManager.StudioSystem.getSoundInfo(key, out SOUND_INFO soundInfo);

                        if (result != FMOD.RESULT.OK)
                            break;

                        result = RuntimeManager.CoreSystem.createSound(soundInfo.name_or_data, soundMode | soundInfo.mode, ref soundInfo.exinfo, out FMOD.Sound sound);

                        if (result != FMOD.RESULT.OK)
                            break;
                        
                        properties.sound = sound.handle;
                        properties.subsoundIndex = soundInfo.subsoundindex;
                        Marshal.StructureToPtr(properties, parametersPtr, false);
                        break;
                    }
                case EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                    {
                        var parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parametersPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
                        var sound = new FMOD.Sound(parameter.sound);
                        sound.release();
                        break;
                    }
                case EVENT_CALLBACK_TYPE.DESTROYED:
                    {
                        stringHandle.Free();
                        break;
                    }
            }
            return FMOD.RESULT.OK;
        }

        private static bool TryGetVoiceoverLength(string key, out float length)
        {
            length = 0;

            if (string.IsNullOrEmpty(key))
                return false;

            var result = RuntimeManager.StudioSystem.getSoundInfo(key, out SOUND_INFO soundInfo);
           
            if (result != FMOD.RESULT.OK) 
            { 
                Debug.LogError(result); 
                return false; 
            }

            FMOD.MODE mode = FMOD.MODE.OPENONLY;
            result = RuntimeManager.CoreSystem.createSound(soundInfo.name_or_data, mode, ref soundInfo.exinfo, out FMOD.Sound sound);
            
            if (result != FMOD.RESULT.OK) 
            { 
                Debug.LogError(result); 
                return false; 
            }

            result = sound.getSubSound(soundInfo.subsoundindex, out FMOD.Sound subSound);

            if (result != FMOD.RESULT.OK)
            {
                sound.release();
                Debug.LogError(result);
                return false;
            }

            result = subSound.getLength(out uint duration, FMOD.TIMEUNIT.MS);
  
            if (result != FMOD.RESULT.OK)
            {
                sound.release();
                Debug.LogError(result);
                return false;
            }

            length = ((float)duration) / 1000;
            sound.release();
            return true;
        }

        void OnDestroy()
        {
            if (voiceoverInstance.isValid())
            {
                voiceoverInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                voiceoverInstance.release();
            }
        }
    }
}