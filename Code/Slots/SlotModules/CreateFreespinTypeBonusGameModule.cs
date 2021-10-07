using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module is currently used to override the freespins type before it is created
// in the SlotResourceMap/SlotResourceData modules. 
// Author : Shaun Peoples <speoples@zynga.com>
// Date : November 07, 2020
// Games : orig008
//
public class CreateFreespinTypeBonusGameModule : SlotModule
{
	[Header("Super Bonus Settings")]
	[Tooltip("Delay before the bonus is launched")]
	[SerializeField] private List<string> bonusGames;
	[SerializeField] private SlotResourceMap.FreeSpinTypeEnum freeSpinsType;

	public override void Awake()
	{
		base.Awake();
		for(int i = 0; i < bonusGames.Count; ++i)
		{
			string gameName = bonusGames[i];
			gameName = string.Format(gameName, GameState.game.keyName);
			bonusGames[i] = gameName;
		}
	}
	
    public override bool needsToExecutePreReelsStopSpinning()
    {
		if (reelGame == null || reelGame.outcome == null)
		{
			return false;
		}
			
		foreach (KeyValuePair<BonusGameType, BaseBonusGameOutcome> bonusGameOutcomes in BonusGameManager.instance.outcomes)
		{
			if (bonusGames.Contains(bonusGameOutcomes.Value.bonusGameName))
			{
				return true;
			}
		}
	
		return false;
    }
    
    public override IEnumerator executePreReelsStopSpinning()
    {
		SlotResourceMap.freeSpinType = freeSpinsType;
		yield break;
    } 
}
