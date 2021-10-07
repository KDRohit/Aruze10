using UnityEngine;
using System.Collections;

/**
* CumulativeBonusToPortalTransitionModule.cs
* Author: Scott Lepthien
* 
* Implements a portal transition which happens after the bonus symbols have been accumulated
* First used by zynga04
*/
public class CumulativeBonusToPortalTransitionModule : CumulativeBonusModule 
{
	[SerializeField] private Animator portalTransitionAnimator;
	[SerializeField] private string portalTransitionStartAnimName;
	[SerializeField] private bool playCumulativeSymbolOffBeforeTransition = true;

	[SerializeField] private AudioListController.AudioInformationList portalTransitionAudio; // audio list to play on transition

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());

		if (reelGame.outcome.isBonus)
		{
			// turn off accumulated symbols before the transition is played (zynga04)
			if (playCumulativeSymbolOffBeforeTransition)
			{
				foreach (Animator cumulativeAnimator in cumulativeSymbolAnimators)
				{
					cumulativeAnimator.Play(CUMULATIVE_SYMBOL_OFF_ANIM_NAME);
				}
			}

			// play the transition to portal
			if (portalTransitionAnimator != null)
			{
				portalTransitionAnimator.gameObject.SetActive(true);

				yield return StartCoroutine(AudioListController.playListOfAudioInformation(portalTransitionAudio));
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalTransitionAnimator, portalTransitionStartAnimName));
			}
			else
			{
				Debug.LogWarning("portalTransitionAnimator is null!");
			}

			// turn off accumulated symbols after the transition is played (twilight01)
			if (!playCumulativeSymbolOffBeforeTransition)
			{
				foreach (Animator cumulativeAnimator in cumulativeSymbolAnimators)
				{
					cumulativeAnimator.Play(CUMULATIVE_SYMBOL_OFF_ANIM_NAME);
				}
			}
		}
	}
	
	public override IEnumerator executeOnBonusGameEnded()
	{	
		yield return StartCoroutine(base.executeOnBonusGameEnded());	
		portalTransitionAnimator.gameObject.SetActive(false);
		yield break;
	}
}
