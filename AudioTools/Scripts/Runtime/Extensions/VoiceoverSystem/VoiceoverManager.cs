// FMOD-Unity-Tools by Ville Ojala
// MIT License
// https://github.com/VilleOjala/FMOD-Unity-Tools

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FMODUnityTools
{
    [AddComponentMenu("FMOD Unity Tools/Extensions/Voiceover System/Voiceover Manager")]
    public class VoiceoverManager : MonoBehaviour
    {
        public event Action<string> DialogueReleased;
        public List<VoiceoverPlaybackHandler> voiceoverPlaybackHandlers = new List<VoiceoverPlaybackHandler>();
        private List<VoiceoverPlaybackHandler> validPlaybackHandlers = new List<VoiceoverPlaybackHandler>();
        private Dictionary<Speaker, VoiceoverPlaybackHandler> playbackHandlersBySpeaker = new Dictionary<Speaker, VoiceoverPlaybackHandler>();
        
        public KeyOffsetData keyOffsetData;
        private Dictionary<string, float> keyOffsetPairs = new Dictionary<string, float>();

        private Dictionary<string, List<Speaker>> activeDialogues = new Dictionary<string, List<Speaker>>();
        private List<QueuedLine> queuedLines = new List<QueuedLine>();

        [Tooltip("Default offset in seconds in relation to the length of the voiceover file, " +
                 "which gives us the duration after which the next line in a dialogue can start to play")]
        [Min(0), SerializeField]
        private float defaultReleaseOffset = 0.5f;

        private class QueuedLine
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
                    bool wasInitialized = playbackHandler.Initialize(this);

                    if (wasInitialized)
                    {
                        validPlaybackHandlers.Add(playbackHandler);
                    }                
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
                    Debug.LogWarning("Speaker + '" + speaker.ToString() + "' has multiple VoiceoverPlaybackHandlers.");
                }
            }

            if (keyOffsetData != null)
            {
                for (int i = 0; i < keyOffsetData.keyOffsets.Count; i++)
                {
                    var pair = keyOffsetData.keyOffsets[i];

                    if (pair != null)
                    {
                        if (!keyOffsetPairs.ContainsKey(pair.key))
                        {
                            keyOffsetPairs.Add(pair.key, pair.offset);
                        }
                        else
                        {
                            Debug.LogWarning("KeyOffsetData has multiple entries for key: " + pair.key);
                        }
                    }
                }
            }
        }

        /* Game's dialogue system should call this function to start playing voiceovers for a new dialogue, or to provide 
         * the next line in an already playing dialogue when it receives a callback from the VoiceoverManager. */
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
                float releaseOffset = defaultReleaseOffset;

                if (keyOffsetPairs.ContainsKey(key))
                {
                    releaseOffset = keyOffsetPairs[key];
                }

                /* result == -1 -> something unexpected failed in starting the dialogue line.
                 * result == 0  -> the speaker is busy with another dialogue line - queue this line and try again once the speaker reports that it is now available.
                 * result == 1  -> starting the dialogue line succeeded. */
                int result = playbackHandler.PlayVoiceover(key, dialogueName, releaseOffset);

                if (result == 1)
                {
                    var speakers = activeDialogues[dialogueName];

                    if (!speakers.Contains(speaker))
                    {
                        speakers.Add(speaker);
                    }
                }
                else if (result == 0)
                {
                    var queuedLine = new QueuedLine();
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

        // It is the responsibility of the game's dialogue system to inform VoiceoverManager that a particular dialogue has finished.
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
            
                // 2. Stop any actively talking speakers associated with the finished dialogue. 
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

        // VoiceoverPlaybackHandlers report themselves as being available for a new dialogue line once they have finished with the previous one.
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

                if (queuedLine.speaker == availableSpeaker && !validQueuedLineFound)
                {
                    // If there are multiple queued lines for the speaker, the latest addition will be played.
                    validQueuedLineFound = true;
                    PlayDialogue(queuedLine.speaker, queuedLine.key, queuedLine.dialogueName);
                    queuedLines.RemoveAt(i);
                }
            }

            if (!validQueuedLineFound && activeDialogues.ContainsKey(latestPlayingDialogue))
            {
                // Send a callback telling the dialogue system that it can now provide VoiceoverManager with the next line in an active dialogue.
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