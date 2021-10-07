using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Module to play a list of sounds that could block before proceeding on to do the normal bonus acquired effects
first used for gen73 to add an additional beel sound that the sound team wanted before playin the symbol animaitons and sounds

Original Author: Scott Lepthien
Creation Date: May 11, 2017
*/
public class PlaySoundsBeforeBonusAcquiredEffectsModule : SlotModule 
{
	[SerializeField] private AudioListController.AudioInformationList soundsToPlayBeforeBonusAcquiredEffects;

	public override void Awake()
	{
		base.Awake();

		if (!(reelGame is SlotBaseGame))
		{
			Debug.LogError("PlaySoundsBeforeBonusAcquiredEffectsModule.Awake() - This module can only be attached to SlotBaseGame classes!  Destroying this module.");
			Destroy(this);
		}		
	}

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		return true;
	}
	
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(soundsToPlayBeforeBonusAcquiredEffects));
		yield return StartCoroutine(reelGame.engine.playBonusAcquiredEffects());
		SlotBaseGame baseGame = reelGame as SlotBaseGame;
		baseGame.isBonusOutcomePlayed = true;
	}
}
