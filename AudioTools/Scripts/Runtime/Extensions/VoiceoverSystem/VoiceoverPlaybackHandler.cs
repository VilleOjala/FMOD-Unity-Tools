// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Voiceover System/Voiceover Playback Handler")]
    public class VoiceoverPlaybackHandler : MonoBehaviour
    {
        public Speaker speaker = Speaker.Player;

        [FMODUnity.EventRef]
        public string masterVoiceoverEvent;

        public Transform followTransform;

        public bool spatialAudioRoomAware = false;

        [HideInInspector]
        public SpatialAudioRoom initialRoom;

        FMOD.Studio.EventInstance eventInstance;
        FMOD.Studio.EventDescription eventDescription;
        float maxDistance; //if is 3D 

        FMOD.Studio.EVENT_CALLBACK voiceoverCallback;

        bool isSpeaking = false;
        string currentDialogue;
        bool coroutineRunning = false;

        bool is3D = false;
        bool initializationSuccesfull = false;

        private VoiceoverManager voiceoverManager;

        public bool Initialize(VoiceoverManager manager)
        {
            if (manager == null)
                return false;
            else
            {
                voiceoverManager = manager;
            }    
        
            if (!string.IsNullOrEmpty(masterVoiceoverEvent))
            {
                eventDescription = FMODUnity.RuntimeManager.GetEventDescription(masterVoiceoverEvent);

                if (eventDescription.isValid())
                {
                    eventDescription.is3D(out is3D);
                    eventDescription.getMinMaxDistance(out float minDistance, out maxDistance);
                }
                else
                {
                    Debug.LogError("The master voiceover event is invalid for '" + speaker.ToString() + "'.");
                    return false;
                }
            }
            else
            {
                Debug.LogError("The master voiceover event is missing for '" + speaker.ToString() + "'.");
                return false;
            }

            voiceoverCallback = new FMOD.Studio.EVENT_CALLBACK(VoiceEventCallback);
            initializationSuccesfull = true;

            return true;
        }

        public int PlayVoiceover(string key, string dialogueName, float overrideDuration) 
        {
            if (!initializationSuccesfull || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(dialogueName))
                return -1;

            if (!isSpeaking)
            {
                bool didSucceed = PlayProtocol(key, dialogueName, overrideDuration);

                if (didSucceed)
                    return 1;
                else
                    return -1;
            }
            else if (eventInstance.isValid())
            {
                // Stop the current dialogue with a quick fade out and then start the new line.
                // <- No click/pop stops, but also enforces the speaker monophony.
                eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

                if (coroutineRunning)
                {
                    StopAllCoroutines();
                    coroutineRunning = false;
                }

                // Inform the voiceover manager that the playback handler is busy stopping the previous dialogue line and it should queue the line.
                return 0;        
            }
            else // If we are on a customly assigned pacing wait after a line has stopped playing let's abort this wait and start the new line immediately.
            {
                if (coroutineRunning)
                {
                    StopAllCoroutines();
                    coroutineRunning = false;
                    isSpeaking = false;
                    currentDialogue = null;
                }

                bool didSucceed = PlayProtocol(key, dialogueName, overrideDuration);

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
                if (eventInstance.isValid())
                {
                    eventInstance.stop(stopMode);

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

                    if(voiceoverManager != null)
                        voiceoverManager.ReportSpeakerAvailability(speaker, latestDialogue);
                }
            }
        }

        private bool PlayProtocol(string key, string dialogueName, float overrideDuration)
        {
            eventInstance = FMODUnity.RuntimeManager.CreateInstance(masterVoiceoverEvent);

            if (!eventInstance.isValid())
                return false;

            GCHandle stringHandle = GCHandle.Alloc(key, GCHandleType.Pinned);
            eventInstance.setUserData(GCHandle.ToIntPtr(stringHandle));
            eventInstance.setCallback(voiceoverCallback);

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

                // If the followed game object has a rigidbody, retrieve it and pass it to FMODUnity Runtimemanager for velocity updates. 
                Rigidbody rb = transformToFollow.gameObject.GetComponent<Rigidbody>();

                FMODUnity.RuntimeManager.AttachInstanceToGameObject(eventInstance, transformToFollow, rb);

                if (spatialAudioRoomAware && SpatialAudioManager.Instance != null)
                {
                    SpatialAudioManager.Instance.RegisterRoomAwareInstance(eventInstance, transformToFollow, maxDistance, initialRoom);
                }
            }

            isSpeaking = true;
            currentDialogue = dialogueName;

            if (overrideDuration >= 0)
            {
                StartCoroutine(WaitBeforeDialogueRelease(overrideDuration));
                coroutineRunning = true;
            }

            eventInstance.start();

            return true;
        }

        void Update()
        {
            if (isSpeaking)
            {
                if (eventInstance.isValid())
                {
                    FMOD.Studio.PLAYBACK_STATE playbackState;               
                    eventInstance.getPlaybackState(out playbackState);

                    if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                    {
                        eventInstance.release();

                        if (!coroutineRunning)
                        {
                            isSpeaking = false;
                        }
                        if (!string.IsNullOrEmpty(currentDialogue) && !coroutineRunning)
                        {
                            string latestDialogue = currentDialogue;
                            currentDialogue = null;

                            if (voiceoverManager != null)
                                voiceoverManager.ReportSpeakerAvailability(speaker, latestDialogue);
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
                voiceoverManager.ReportSpeakerAvailability(speaker, latestDialogue);
        }
        
        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        static FMOD.RESULT VoiceEventCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr); // Note: this is just a new wrapper, not a new event instance.

            IntPtr stringPtr;
            instance.getUserData(out stringPtr);

            GCHandle stringHandle = GCHandle.FromIntPtr(stringPtr);
            String key = stringHandle.Target as String;

            switch (type)
            {
                case FMOD.Studio.EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                    {
                        FMOD.MODE soundMode = FMOD.MODE.LOOP_NORMAL | FMOD.MODE.CREATECOMPRESSEDSAMPLE | FMOD.MODE.NONBLOCKING; 
                        var parameter = (FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES));

                        if (key.Contains("."))
                        {
                            FMOD.Sound dialogueSound;
                            var soundResult = FMODUnity.RuntimeManager.CoreSystem.createSound(Application.streamingAssetsPath + "/" + key, soundMode, out dialogueSound);
                            if (soundResult == FMOD.RESULT.OK)
                            {
                                parameter.sound = dialogueSound.handle;
                                parameter.subsoundIndex = -1;
                                Marshal.StructureToPtr(parameter, parameterPtr, false);
                            }
                        }
                        else
                        {
                            FMOD.Studio.SOUND_INFO dialogueSoundInfo;
                            var keyResult = FMODUnity.RuntimeManager.StudioSystem.getSoundInfo(key, out dialogueSoundInfo);
                            if (keyResult != FMOD.RESULT.OK)
                            {
                                break;
                            }
                            FMOD.Sound dialogueSound;
                            var soundResult = FMODUnity.RuntimeManager.CoreSystem.createSound(dialogueSoundInfo.name_or_data, soundMode | dialogueSoundInfo.mode, ref dialogueSoundInfo.exinfo, out dialogueSound);
                            if (soundResult == FMOD.RESULT.OK)
                            {
                                parameter.sound = dialogueSound.handle;
                                parameter.subsoundIndex = dialogueSoundInfo.subsoundindex;
                                Marshal.StructureToPtr(parameter, parameterPtr, false);
                            }
                        }
                        break;
                    }
                case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                    {
                        var parameter = (FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES));
                        var sound = new FMOD.Sound(parameter.sound);
                        sound.release();

                        break;
                    }
                case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED:
                    {
                        stringHandle.Free();

                        break;
                    }
            }
            return FMOD.RESULT.OK;
        }

        void OnDestroy()
        {
            if (eventInstance.isValid())
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
        }

        void OnValidate()
        {
            gameObject.name = speaker.ToString() + "VoiceoverPlaybackHandler";   
        }
    }
}