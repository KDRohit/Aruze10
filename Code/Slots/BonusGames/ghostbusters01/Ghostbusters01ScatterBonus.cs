using UnityEngine;
using System.Collections;

public class Ghostbusters01ScatterBonus : GenericScatterBonus
{
	[SerializeField] private float timeBetweenIntroAnims = 0.0f;
	[SerializeField] private string scatterBonusIconIntroAnim = "Intro";

	public override void init()
	{
		base.init();
		StartCoroutine(PlayIconIntroAnims());
	}

	//Play the anims with a delay so they waterfall in
	private IEnumerator PlayIconIntroAnims()
	{
		foreach (ScatterBonusIcon scatterBonusIcon in currentBonusIcons)
		{
			scatterBonusIcon.iconAnimator.Play(scatterBonusIconIntroAnim);
			yield return new TIWaitForSeconds(timeBetweenIntroAnims);
		}
	}

	protected override void doPickedRevealEffects (string currentWonSymbolName)
	{
		switch (currentWonSymbolName)
		{
		case "fruitcake":
			specialWin = true;
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_SOUND));
			Audio.play(Audio.soundMap(SCATTER_REVEAL_VALUE_1_VO));
			StartCoroutine(playSecondVOsoundWithDelay());
			break;
		case "eggnog":
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_NORMAL_FLOURISH));
			break;
		case "shoes":
			Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_NORMAL_FLOURISH));
			break;
		}
	}

	private IEnumerator playSecondVOsoundWithDelay()
	{
		float fanfareWait = SCATTER_WAIT_SECONDS_BEFORE_UNPICKED_REVEAL + REVEAL_WAIT_2 * (wheelOutcome.extraInfo-1) + PRE_SHOW_REVEALS_WAIT + PRE_FADE_WAIT + FADE_OUT_DURATION;
		yield return new TIWaitForSeconds(fanfareWait);
		Audio.play(Audio.soundMap(SCATTER_REVEAL_PICKED_BIG_VO));
	}

}
