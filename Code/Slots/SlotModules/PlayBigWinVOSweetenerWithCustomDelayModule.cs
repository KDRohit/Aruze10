using UnityEngine;
using System.Collections;

public class PlayBigWinVOSweetenerWithCustomDelayModule : SlotModule
{
	[SerializeField] private float BIG_WIN_VO_DELAY = 0.0f;
	private const string bigWinVOSound = "bigwin_vo_sweetener";

	public override bool needsToOverrideBigWinSounds ()
	{
		return true;
	}

	public override void overrideBigWinSounds ()
	{
		Audio.playWithDelay(Audio.soundMap(bigWinVOSound), BIG_WIN_VO_DELAY);
	}
}
