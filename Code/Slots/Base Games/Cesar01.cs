using UnityEngine;
using System.Collections;

//Custom basegame to handle the transistion to the portal, modeled after Osa02
public class Cesar01 : TumbleSlotBaseGame
{
	[SerializeField] protected AnimationListController.AnimationInformation reelsOutAnimation;	

	// Starts up the BonusGameMannager.
	public override void goIntoBonus()
	{
		RoutineRunner.instance.StartCoroutine(doStartBonus());
	}

	private IEnumerator doStartBonus()
	{			
		if (SpinPanel.instance != null)
		{
			float spinPanelSlideOutTime = TRANSITION_SLIDE_TIME;
			if (SpinPanel.instance.backgroundWingsWidth != null)
			{
				float spinPanelBackgroundHeight = SpinPanel.instance.backgroundWingsWidth.localScale.y;
				spinPanelSlideOutTime *= spinPanelBackgroundHeight / NGUIExt.effectiveScreenHeight;
			}
			StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, spinPanelSlideOutTime, false));			
			if (reelGameBackground != null)
			{
				StartCoroutine(reelGameBackground.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Fullscreen, spinPanelSlideOutTime, iTween.EaseType.linear));
			}
		}
		yield return StartCoroutine(AnimationListController.playAnimationInformation(reelsOutAnimation));

		base.goIntoBonus();

		yield return null;

		// Fix the SpinPanel.
		if (SpinPanel.instance != null)
		{
			SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
		}

		if (reelGameBackground != null)
		{
			StartCoroutine(reelGameBackground.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Basegame, 0, iTween.EaseType.linear));
		}
	}
}
