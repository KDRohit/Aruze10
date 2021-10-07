using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This module allows you to play any animation defined on a symbol in the reel setup.
// Animations are played when each reel comes to a stop, and is used to override the 
// behaviour in SpinReel.cs. This also allows you to play a list of sounds with the animation.
// Additionally, you can use this module to prevent an anticipation animation from playing by
// simply defining it for a symbol and setting the animation type to None.
//
// Using in gen41 to block aniticipation animations playing at the wrong time in freespins.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Dec 11th, 2017
public class PlaySymbolAnticipationAnimationWithSoundOnEachReelStopModule : SlotModule
{
    public List<CustomAnticipationAnimationData> anticipationAnimationDataList;
    private Dictionary<string, CustomAnticipationAnimationData> _anticipationAnimationMap;
    private Dictionary<string, bool> _symbolSoundsPlayed;

    public override void Awake()
    {
        base.Awake();
        createAnticipationAnimationDataMap();
        _symbolSoundsPlayed = new Dictionary<string, bool>();
    }

    public override bool needsToExecuteOnReelsSpinning()
    {
        return anticipationAnimationDataList != null && anticipationAnimationDataList.Count > 0;
    }

    public override IEnumerator executeOnReelsSpinning()
    {
        _symbolSoundsPlayed.Clear();
        yield break;
    }

    // Creates a mapping of symbol name to its custom animation data for quick lookup.
    private void createAnticipationAnimationDataMap()
    {
        if (anticipationAnimationDataList != null)
        {
            _anticipationAnimationMap = new Dictionary<string, CustomAnticipationAnimationData>();
            for (int i = 0; i < anticipationAnimationDataList.Count; i++)
            {
                if (!_anticipationAnimationMap.ContainsKey(anticipationAnimationDataList[i].symbolName))
                {
                    _anticipationAnimationMap.Add(anticipationAnimationDataList[i].symbolName, anticipationAnimationDataList[i]);
                }
            }
        }
    }

    // We play animations if the symbol name matches and the reelID is defined.
    public override bool needsToExecuteForSymbolAnticipation(SlotSymbol symbol)
    {
        if (_anticipationAnimationMap != null)
        {
            return _anticipationAnimationMap.ContainsKey(symbol.name) && _anticipationAnimationMap[symbol.name].shouldPlayAnimationOnReelId(symbol.reel.reelID);
        }
        return false;
    }

    public override void executeForSymbolAnticipation(SlotSymbol symbol)
    {
        if (_anticipationAnimationMap != null && _anticipationAnimationMap.ContainsKey(symbol.name))
        {
            CustomAnticipationAnimationData anticipationData = _anticipationAnimationMap[symbol.name];
            SymbolAnimator symbolAnimator = symbol.getAnimator();
            symbolAnimator.playAnimation(anticipationData.symbolAnimationType);

            if (anticipationData.soundsPlayedDuringAnimation != null)
            {
                if (!anticipationData.onlyPlaySoundOnce || !_symbolSoundsPlayed.ContainsKey(symbol.name))
                {
                    AudioListController.playListOfAudioInformation(anticipationData.soundsPlayedDuringAnimation);
                }
            }
        }
    }

    // Data class for holding information about animations for each symbol
    // animations we want to override.
    [System.Serializable]
    public class CustomAnticipationAnimationData
    {
        public List<int> reelIdsAffected = new List<int>();
        public string symbolName = "";
        public SymbolAnimationType symbolAnimationType;
        public AudioListController.AudioInformationList soundsPlayedDuringAnimation = new AudioListController.AudioInformationList();
        public bool onlyPlaySoundOnce;

        public bool shouldPlayAnimationOnReelId(int reelID)
        {
            for (int i = 0; i < reelIdsAffected.Count; i++)
            {
                if (reelIdsAffected[i] == reelID)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
