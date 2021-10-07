using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BannerTypeEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;

public class Osa02PortalScript : PortalScript
{
	[SerializeField] private Shader monochrome;
	private bool firstReveal = true;
	private const string PICKME_ANIMATION_NAME = "banner_clickMe_Pickme";
	private const string REVEAL_ANIMATION_NAME = "banner_clickMe_Reveal";
	private const string FREESPIN_REVEAL_ANIMATION = "FreeSpins_Banner";
	private const string CHALLENGE_REVEAL_ANIMATION = "HatPick_Banner";

	public override void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		firstReveal = true;
		base.beginPortal(bannerRoots, banners, bannerOverlay, outcome, multiplier);
	}

	// Plays the pickme Animation on the banner if one exsits while taking care to make sure only one is played at a time.
	protected override IEnumerator playPickMeAnimation()
	{
		List<Animator> bannerAnimators = new List<Animator>();
		foreach (GameObject bannerObject in bannerObjects)
		{
			Animator bannerAnimator = bannerObject.GetComponent<Animator>();
			if (bannerAnimator != null)
			{
				bannerAnimators.Add(bannerAnimator);
			}
			else
			{
				Debug.LogWarning("There was a problem getting the Animator from the banner.");
			}
		}
		while (bannerAnimators.Count > 0)
		{
			int bannerToAddPickmeAnimation = Random.Range(0, bannerAnimators.Count);
			Animator bannerAnimator = bannerAnimators[bannerToAddPickmeAnimation];
			if (bannerAnimator != null)
			{
				bannerAnimator.Play(PICKME_ANIMATION_NAME);
				while (!bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(PICKME_ANIMATION_NAME))
				{
					yield return null;
				}
				// Wait for the animation to stop.
				while (bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(PICKME_ANIMATION_NAME))
				{
					yield return null;
				}
			}
			yield return new WaitForSeconds(Random.Range(1.0f, 3.5f));
		}
	}

	protected override IEnumerator doBeginPortalReveals(GameObject objClicked, RevealDelegate revealDelegate)
	{
		if (_bannerMap[BannerTypeEnum.CLICKME].pickMePrefab != null)
		{
			stopPickMeAnimation(true);
			StopCoroutine("playPickMeAnimation");		
		}
		GameObject revealObject;
		revealedObjects = new List<GameObject>();
		//It is possible to send the text object clicked on since clicking on the object itself may be impossible from a 
		//perspective camera, so we will check to see if we got a text object, and if so, grab the associated revealObject.
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			GameObject bannerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Text");
			if (objClicked.transform.parent.gameObject == bannerObjects[i] || objClicked == bannerText)
			{
				yield return StartCoroutine(revealDelegate(bannerObjects[i]));
				_revealedIndex = i;
				
				LabelWrapper label;
				LabelWrapper footerLabel;
				LabelWrapper headerLabel;
				getBannerLabels(bannerTextObjects[i], out label, out footerLabel, out headerLabel);

				resetBannerPositions(i, headerLabel.gameObject, label.gameObject, footerLabel.gameObject);

				label.text = "";
				footerLabel.text = "";
				headerLabel.text = "";
				if (_outcome.isChallenge)
				{
					revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);
					setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
				}
				else if(_outcome.isGifting)
				{
					revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);
					setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
				}
				else
				{
					revealObject = null;
				}

				if (revealObject != null)
				{
					VisualEffectComponent.Create(_revealVfx, revealObject);
				}

				yield return StartCoroutine(playPickedBannerAnimation(revealObject));
				bannerObjects[i] = null;
			}
		}

		
		revealBanners();
	}

	private IEnumerator playPickedBannerAnimation(GameObject banner)
	{
		Animator bannerAnimator = banner.GetComponent<Animator>();
		if (bannerAnimator != null)
		{
			string revealAnimation = "";
			if (_outcome.isChallenge)
			{
				Audio.play("PortalRevealHatPickOSA02");
				revealAnimation = CHALLENGE_REVEAL_ANIMATION;
			}
			else
			{
				Audio.play("PortalRevealFreespinCrowOSA02");
				revealAnimation = FREESPIN_REVEAL_ANIMATION;
			}
			bannerAnimator.Play(revealAnimation);
			while (!bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(revealAnimation))
			{
				yield return null;
			}
			// Wait for the animation to stop.
			while (bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(revealAnimation))
			{
				yield return null;
			}
		}
	}

	protected override IEnumerator doAnimateBannerAndFlyout(GameObject banner)
	{
		Animator bannerAnimator = banner.GetComponent<Animator>();
		// Play the reveal animation and wait for it to finish.
		if (bannerAnimator != null)
		{
			bannerAnimator.Play(REVEAL_ANIMATION_NAME);
			while (!bannerAnimator.GetCurrentAnimatorStateInfo(0).IsName(REVEAL_ANIMATION_NAME))
			{
				yield return null;
			}
			yield return new TIWaitForSeconds(1.1f);
		}
		else
		{
			Debug.LogWarning("There was no Animator on the banner to animate.");
		}
		Destroy(banner);
	}

	protected override GameObject createRevealBanner(SlotBaseGame.BannerInfo.BannerTypeEnum bannerName, GameObject parentBanner)
	{
		GameObject banner = base.createRevealBanner(bannerName, parentBanner);

		if (!firstReveal)
		{
			foreach (Renderer renderer in banner.GetComponentsInChildren<Renderer>())
			{
				if (monochrome != null)
				{
					renderer.material.shader = monochrome;
				}
			}

			foreach (UILabelStyler style in banner.GetComponentsInChildren<UILabelStyler>())
			{
				style.style = null;
				style.labelWrapper.color = Color.gray;
				style.labelWrapper.effectStyle = "none";
				style.labelWrapper.isGradient = false;
			}
		}
		else
		{
			firstReveal = false;
		}
		banner.SetActive(false);

		StartCoroutine(showBanner(banner));
		return banner;
	}

	private IEnumerator showBanner(GameObject banner)
	{
		banner.SetActive(true);
		yield return null;
	}
}