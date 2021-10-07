using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class to keep the custom VO call for Gen07 freespins out of the base module.

Original Author: Leo Schnee
*/
public class Gen07MultiplierFreespinSlidingGameModule : MultiplierFreespinSlidingGameModule 
{
	private const string SUMMARY_VO_SOUND = "SummaryVOUnicorn";

	protected override IEnumerator playCustomGameEndedSound()
	{
		Audio.play(SUMMARY_VO_SOUND);
		yield break;
	}
}
