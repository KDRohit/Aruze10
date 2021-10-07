using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Slot module for canceling the music when entering a slot game, used for games that don't have
an intro music track, but don't want the lobby sounds playing before the user hits the spin
button.  See aruze02 for an example.

Original Author: Scott Lepthien
Creation Dat: 3/22/2017
*/
public class CancelMusicOnLoadModule : SlotModule 
{
	[SerializeField] private float MUSIC_FADE_TIME = 1.0f;

	public override bool needsToExecuteAfterLoadingScreenHidden()
	{
		return true;
	}
	
	public override IEnumerator executeAfterLoadingScreenHidden()
	{
		Audio.switchMusicKeyImmediate("", MUSIC_FADE_TIME);
		yield break;
	}
}
