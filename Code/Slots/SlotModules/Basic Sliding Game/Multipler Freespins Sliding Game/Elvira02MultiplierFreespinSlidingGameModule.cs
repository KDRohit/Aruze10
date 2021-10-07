using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class to keep the custom VO call for Elvira02 freespins out of the base module.

Original Author: Leo Schnee
*/
public class Elvira02MultiplierFreespinSlidingGameModule : MultiplierFreespinSlidingGameModule 
{
	private const string SUMMARY_VO_SOUND = "SummaryVOEL02";

	protected override IEnumerator playCustomGameEndedSound()
	{
		Audio.play(SUMMARY_VO_SOUND);
		yield break;
	}
}
