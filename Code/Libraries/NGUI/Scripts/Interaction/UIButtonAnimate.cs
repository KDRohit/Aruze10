using UnityEngine;
using System.Collections;

/// Added by Zynga - Stephan Schwirzke
/// <summary>
/// Works in conjuction with the existing UIButtonColor system, allows you to play animation list on a button press/release
/// audio can also be added to the animation list
/// </summary>
public class UIButtonAnimate : UIButtonColor
{
	public AnimationListController.AnimationInformationList pressAnimationList;
	public AnimationListController.AnimationInformationList hoverAnimationList;
	public AnimationListController.AnimationInformationList restoreAnimationList;

	protected override void Init ()
	{
		base.Init();
	}

	public override void OnPress(bool isPressed)
	{
		base.OnPress(isPressed);

		if (isPressed)
		{
			if (pressAnimationList != null)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(pressAnimationList));
			}
		}
		else
		{
			playRestoreAnimations();
		}
	}
	
	public override void OnHover(bool isOver)
	{
		base.OnHover(isOver);

		if (isOver)
		{
			if (hoverAnimationList != null)
			{			
				StartCoroutine(AnimationListController.playListOfAnimationInformation(hoverAnimationList));
			}
		}
		else
		{
			playRestoreAnimations();
		}		
	}

	private void playRestoreAnimations()
	{
		if (restoreAnimationList != null)
		{			
			StartCoroutine(AnimationListController.playListOfAnimationInformation(restoreAnimationList));
		}
	}
}
