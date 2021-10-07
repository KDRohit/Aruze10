using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Dialog audio pack allows the setup, and usage of downloaded audio files (direct from CDN). This provides
 * the skeleton data, and preloading options for audio that will be used pretty much anywhere for any given dialog.
 *
 * Use case is to create an instance of a DialogAudioPack within your dialogbase class. Then make sure to add the audio
 * clip names using addAudio(). When finished, you can preloadAudio(), but it's not necessary.
 *
 * From you dialogbase class, you can then call playAudioFromEos(audioPack.getAudioKey(<insert key type, if it's open etc>));
 */
public class DialogAudioPack
{
    // =============================
    // PRIVATe
    // =============================
    private string audioPackKey;
    private string audioPackKeyPascal;
    private Dictionary<string, DialogAudioName> audioNames;

    // =============================
    // CONST
    // =============================
    public const string CLOSE = "close";
    public const string OPEN = "open";
    public const string OK = "ok";
    public const string MUSIC = "music";

    public DialogAudioPack(string audioPackKey = "")
    {
        if (!string.IsNullOrEmpty(audioPackKey))
        {
            init(audioPackKey); 
        }
    }

    private void init(string audioPackKey)
    {
        this.audioPackKey = audioPackKey;
        audioNames = new Dictionary<string, DialogAudioName>();
        audioPackKeyPascal = CommonText.snakeCaseToPascalCase(audioPackKey);
    }

    public string getAudioKey(string keyName)
    {
        if (audioNames != null && audioNames.ContainsKey(keyName))
        {
            return audioNames[keyName].soundName;
        }

        return "";
    }

    public void addAudio(string keyName, string audioClipName)
    {
        // add the prefix audio key to the audio clip name if it doesn't already exist
        if (!audioClipName.Contains(audioPackKeyPascal))
        {
            audioClipName = audioPackKeyPascal + audioClipName;
        }

        if (audioNames.ContainsKey(keyName))
        {
            // squash the old one
            audioNames[keyName] = new DialogAudioName(audioClipName);
        }
        else
        {
            audioNames.Add(keyName, new DialogAudioName(audioClipName));
        }
    }

    public void preloadAudio()
    {
        foreach (KeyValuePair<string, DialogAudioName> entry in audioNames)
        {
            string audioName = entry.Value.soundName;

            if (!string.IsNullOrEmpty(audioName))
            {
                AudioInfo audioInfo = AudioInfo.find(audioName, true);

                if (audioInfo != null)
                {
                    audioInfo.prepareClip();
                }
            }
        }
    }

    public class DialogAudioName
    {
        public string soundName;

        public DialogAudioName(string clipName)
        {
            soundName = clipName;
        }
    }
}