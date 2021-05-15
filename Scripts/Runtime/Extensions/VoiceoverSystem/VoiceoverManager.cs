// MIT License
// Audio Implementation Tools for FMOD and Unity
// Copyright 2021, Ville Ojala.
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioTools
{
    [AddComponentMenu("Audio Tools/Extensions/Voiceover System/Voiceover Manager")]
    public class VoiceoverManager : MonoBehaviour
    {
        public event Action<string> DialogueReleased;

        public List<VoiceoverPlaybackHandler> voiceoverPlaybackHandlers = new List<VoiceoverPlaybackHandler>();
        private List<VoiceoverPlaybackHandler> validPlaybackHandlers = new List<VoiceoverPlaybackHandler>();
        private Dictionary<Speaker, VoiceoverPlaybackHandler> playbackHandlersBySpeaker = new Dictionary<Speaker, VoiceoverPlaybackHandler>();

        public VoiceoverDurationSet voiceoverDurationSet;
        private Dictionary<string, float> durationByKey = new Dictionary<string, float>();

        private Dictionary<string, List<Speaker>> activeDialogues = new Dictionary<string, List<Speaker>>();
        private List<QueuedLine> queuedLines = new List<QueuedLine>();

        class QueuedLine
        {
            public Speaker speaker;
            public string key;
            public string dialogueName;
        }

        void Awake()
        {
            for (int i = 0; i < voiceoverPlaybackHandlers.Count; i++)
            {
                var playbackHandler = voiceoverPlaybackHandlers[i];

                if (playbackHandler != null)
                {
                    bool isInitialized = playbackHandler.Initialize(this);

                    if (isInitialized)
                        validPlaybackHandlers.Add(playbackHandler);
                }
            }
           
            for (int i = 0; i < validPlaybackHandlers.Count; i++)
            {
                var playbackHandler = validPlaybackHandlers[i];

                Speaker speaker = playbackHandler.speaker;

                if (!playbackHandlersBySpeaker.ContainsKey(speaker))
                {
                    playbackHandlersBySpeaker.Add(speaker, playbackHandler);
                }
                else
                {
                    Debug.LogWarning("Speaker + '" + speaker.ToString() + "' has multiple Voiceover Playback Handlers.");
                }
            }

            if (voiceoverDurationSet != null)
            {
                for (int i = 0; i < voiceoverDurationSet.keyDurations.Count; i++)
                {
                    var pairing = voiceoverDurationSet.keyDurations[i];

                    if (pairing != null)
                    {
                        if (!durationByKey.ContainsKey(pairing.key))
                        {
                            durationByKey.Add(pairing.key, pairing.duration);
                        }
                        else
                        {
                            Debug.LogWarning("Voiceover Duration Set has multiple '" + pairing.key + "' keys.");
                        }
                    }
                }
            }
        }

        //Game's dialogue system should call this function to start playing voiceovers for a new dialogue 
        // or to provide the next line in an already playing dialogue when it receives a release callback from the Voiceover Manager.
        public void PlayDialogue(Speaker speaker, string key, string dialogueName)
        {
            if (playbackHandlersBySpeaker.ContainsKey(speaker))
            {
                if (!activeDialogues.ContainsKey(dialogueName))
                {
                    var dialogueAssociatedSpeakers = new List<Speaker>();
                    activeDialogues.Add(dialogueName, dialogueAssociatedSpeakers);
                }

                var playbackHandler = playbackHandlersBySpeaker[speaker];

                float durationOverride = -1;

                if (durationByKey.Count > 0 && durationByKey.ContainsKey(key))
                {
                    float value = durationByKey[key];

                    if (value >= 0)
                        durationOverride = value;
                }

                // If return = -1 <- something unexpected failed in starting the dialogue.
                // If return = 0 <- the speaker is busy with another dialogue line, queue this line and try again once the speaker reports that is free.
                // If return = 1 <- starting the line succeeded.
                int result = playbackHandler.PlayVoiceover(key, dialogueName, durationOverride);

                if (result == 1)
                {
                    var speakers = activeDialogues[dialogueName];

                    if (speakers.Contains(speaker) == false)
                        speakers.Add(speaker);
                }
                else if (result == 0)
                {
                    QueuedLine queuedLine = new QueuedLine();
                    queuedLine.speaker = speaker;
                    queuedLine.key = key;
                    queuedLine.dialogueName = dialogueName;
                    queuedLines.Add(queuedLine);

                }
                else if (result == -1)
                {
                    Debug.LogError("Playing a voiceover line failed for speaker '" + playbackHandler.speaker + "'.");
                }
            }
            else
            {
                Debug.LogError("Speaker '" + speaker.ToString() + "' does not have valid Voiceover Playback Handler");
            }
        }

        // It is the responsibility of the game's dialogue system to inform the Voiceover Manager that a particular dialogue has finished playing.
        public void SetDialogueFinished (string dialogueName)
        {
            if (activeDialogues.ContainsKey(dialogueName))
            {
                // 1. Remove possible queued lines associated with the finished dialogue.
                for (int i = queuedLines.Count - 1; i >= 0; i--)
                {
                    QueuedLine queuedLine = queuedLines[i];

                    if (queuedLine.dialogueName == dialogueName)
                    {
                        queuedLines.RemoveAt(i);
                    }
                }
            
                // 2. Stop any possible actively talking speakers associated with the finished dialogue. 
                var speakers = activeDialogues[dialogueName];

                for (int i = 0; i < speakers.Count; i++)
                {
                    Speaker speaker = speakers[i];

                    VoiceoverPlaybackHandler playbackHandler = playbackHandlersBySpeaker[speaker];

                    if (playbackHandler != null)
                    {
                        playbackHandler.StopVoiceover(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); 
                    }
                }
                
                // 3. Remove the finished dialogue from the list of active dialogues.
                activeDialogues.Remove(dialogueName);
            }
        }

        // Voiceover Playback Handlers report themselves as being available for a new dialogue line once they are finished with the previous one.
        public void ReportSpeakerAvailability(Speaker availableSpeaker, string latestPlayingDialogue)
        {
            // Remove the association of the speaker with the dialogue it just finished playing voiceovers for (if the dialogue is still active).
            if (activeDialogues.ContainsKey(latestPlayingDialogue))
            {
                if (activeDialogues[latestPlayingDialogue].Contains(availableSpeaker))
                {
                    int index = activeDialogues[latestPlayingDialogue].IndexOf(availableSpeaker);
                    activeDialogues[latestPlayingDialogue].RemoveAt(index);
                }
            }

            // Check if the speaker has queued lines.
            bool validQueuedLineFound = false;

            for (int i = queuedLines.Count - 1; i >= 0; i--)
            {
                QueuedLine queuedLine = queuedLines[i];

                if(queuedLine.speaker == availableSpeaker && !validQueuedLineFound)
                {
                    // If there are multiple queued lines for the speaker the latest addition will be played.
                    validQueuedLineFound = true;
                    PlayDialogue(queuedLine.speaker, queuedLine.key, queuedLine.dialogueName);
                    queuedLines.RemoveAt(i);
                }
            }

            if (!validQueuedLineFound && activeDialogues.ContainsKey(latestPlayingDialogue))
            {
                // Send a callback telling the dialogue system that it can now provide the Voiceover Manager with the next line in an active dialogue.
                DialogueReleased?.Invoke(latestPlayingDialogue);
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < validPlaybackHandlers.Count; i++)
            {
                Destroy(validPlaybackHandlers[i].gameObject);
            }
        }
    }
}