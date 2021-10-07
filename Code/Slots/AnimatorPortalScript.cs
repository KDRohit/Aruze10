using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BannerEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;

public class AnimatorPortalScript : PortalScript
{
	[SerializeField] private float TIME_BEFORE_PICKME_STARTS = 2.0f;
	[SerializeField] private string PICKME_ANIMATION_NAME = "";
	[SerializeField] private string PICKME_REVEAL_NAME = "";
	[SerializeField] private string FREESPIN_ANIMATION_NAME = "";
	[SerializeField] private string PICKEM_ANIMATION_NAME = "";
	[SerializeField] private string FREESPIN_REVEAL_ANIMATION_NAME = "";
	[SerializeField] private string PICKEM_REVEAL_ANIMATION_NAME = "";
	[SerializeField] private float TIME_BEFORE_REVEALS = 1.0f;
	
	public override void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		base.beginPortal(bannerRoots, banners, bannerOverlay, outcome, multiplier);
		StartCoroutine(playPickMeAnimation());
	}

	// Plays the pickme Animation on the banner if one exsits while taking care to make sure only one is played at a time.
	protected override IEnumerator playPickMeAnimation()
	{
		yield return new TIWaitForSeconds(TIME_BEFORE_PICKME_STARTS);
		while (true)
		{
			int bannerToPlayPickmeOn = Random.Range(0,bannerObjects.Count);
			if (bannerObjects[bannerToPlayPickmeOn] != null)
			{
				Animator pickMeAnimator = bannerObjects[bannerToPlayPickmeOn].GetComponentInChildren<Animator>();
				if (pickMeAnimator != null)
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickMeAnimator, PICKME_ANIMATION_NAME));
				}
				yield return new WaitForSeconds(Random.Range(0.0f,7.5f));
			}
			else
			{
				// We can stop this while loop now.
				break;
			}
		}
	}

	// Utility function to show revealed banners.
	protected override GameObject createRevealBanner(BannerEnum bannerName, GameObject parentBanner)
	{
		Animator revealAnimator = parentBanner.GetComponentInChildren<Animator>();
		if (revealAnimator != null)
		{
			revealAnimator.Play(PICKME_REVEAL_NAME);
		}
		GameObject symbol = base.createRevealBanner(bannerName, parentBanner);
		if (revealedObjects.Count == 1)
		{
			// This is the first reveal and we should play the good animation.
			revealAnimator = revealedObjects[0].GetComponentInChildren<Animator>();
			if (revealAnimator != null)
			{
				if (bannerName == BannerEnum.GIFTING)
				{
					revealAnimator.Play(FREESPIN_ANIMATION_NAME);
				}
				else if (bannerName == BannerEnum.CHALLENGE)
				{
					revealAnimator.Play(PICKEM_ANIMATION_NAME);
				}
				else
				{
					Debug.LogWarning("Not sure what animation to play for this reveal.");
				}
			}
		}
		else
		{
			// This is the first reveal and we should play the good animation.
			revealAnimator = revealedObjects[0].GetComponentInChildren<Animator>();
			if (revealAnimator != null)
			{
				if (bannerName == BannerEnum.GIFTING)
				{
					revealAnimator.Play(FREESPIN_REVEAL_ANIMATION_NAME);
				}
				else if (bannerName == BannerEnum.CHALLENGE)
				{
					revealAnimator.Play(PICKEM_REVEAL_ANIMATION_NAME);
				}
				else
				{
					Debug.LogWarning("Not sure what animation to play for this reveal.");
				}
			}
		}
		return symbol;
		
	}

	protected override IEnumerator doAnimateBannerAndFlyout(GameObject banner)
	{
		yield return StartCoroutine(doAnimateBannerOnly(banner));
		yield return new TIWaitForSeconds(TIME_BEFORE_REVEALS);
	}

	protected override void AnimateBannerFlyout(GameObject banner)
	{
		Destroy(banner);
	}
}