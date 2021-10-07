using System.Collections;
using UnityEngine;

/*
Module to replace the sound set in WheelGamePlaySoundOnEventModule executeOnSpinComplete from a sound 
matching "soundNameToReplace" and of the eventType OnSpinComplete with a sound named soundNameReplacementCredits
or soundNameReplacementJackpot based on the outcome credits value. Jackpots need to return 0 credit

Creation Date: August 27, 2019
Original Author: Shaun Peoples

First Use: Zynga06
*/
public class WheelGameSetEventSoundOnSpinCompleteModule : WheelGamePlaySoundOnEventModule
{
    [SerializeField] private string spinCompleteSoundToReplace = "SOUNDNAME_TO_REPLACE";
    [SerializeField] private string spinCompleteSoundCredits;
    [SerializeField] private string spinCompleteSoundJackpot;

    public override IEnumerator executeOnSpinComplete()
    {
        WheelOutcome outcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
        WheelPick wheelPick = outcome.getNextEntry();
        bool isCreditsWin = wheelPick.wins[wheelPick.winIndex].credits > 0;

        for (int i = 0; i < audioListEventsDictionary[AudioEventType.OnSpinComplete].audioInformationList.Count; i++)
        {
            if (audioListEventsDictionary[AudioEventType.OnSpinComplete].audioInformationList.audioInfoList[i].SOUND_NAME == spinCompleteSoundToReplace)
            {
                audioListEventsDictionary[AudioEventType.OnSpinComplete].audioInformationList.audioInfoList[i].SOUND_NAME = isCreditsWin ? spinCompleteSoundCredits : spinCompleteSoundJackpot;
            }
        }

        return base.executeOnSpinComplete();
    }


    private void Reset()
    {
        AudioListEvent audioListEvent = new AudioListEvent();
        audioListEvent.name = "OnSpinCompleteSound";
        audioListEvent.audioInformationList = new AudioListController.AudioInformationList("SOUNDNAME_TO_REPLACE");
        audioListEvent.eventType = AudioEventType.OnSpinComplete;
        audioListEvents.Add(audioListEvent);
    }
}
