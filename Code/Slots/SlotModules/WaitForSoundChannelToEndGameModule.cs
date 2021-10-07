using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaitForSoundChannelToEndGameModule : SlotModule
{
	[SerializeField] private string channelName; // eg "type_vo"
	
	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return true;
	}

	public override IEnumerator executeOnFreespinGameEnd()
	{
		while (Audio.isAudioPlayingByChannel(channelName))
		{
			yield return null;
		}
	}
}
