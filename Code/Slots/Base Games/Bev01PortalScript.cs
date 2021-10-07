using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BannerEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;

/*
 Handles the special animations and orders for the bev01 portal reveal.
*/

public class Bev01PortalScript : PortalScript
{
	public GameObject oilSpoutEffectPrefab = null;
	public UICamera extraCamera;

	private ParticleSystem[] oilSproutParticles;
	private List<Animator> bannerAnimators;

	private GameObject oilSpoutEffect = null;
	private SlotSymbol bonusSymbol = null;
	private float particleSystemStartSize = 0;
	private bool firstPick = true;

	// Constants
	private const float TIME_BEFORE_PLAYING_BANNER_ANIMATION = 1.0f;					// THis is how long to let the eruption play before gooping down the banners.
	private const float TIME_CAMERA_PAN_UP = 2.0f;										// How long to tween the BN symbol down before starting the oil sprout.
	private const float TIME_GOOP_DOWN_ANIMATION = 1.5f;
	private const float TIME_STOP_SPROUT = 1.5f;
	private const float TIME_FOR_REVEAL = 1.033f;
	// Sound names
	private const string INTRO_VO = "BonusYesVOBeverly";								// Name of the sound played at the start of the portal sequence
	private const string PICK_VO = "PortalVOBeverly";									// Name of the sound played once a choice is pickable.
	private const string REVEAL_FS = "PortalRevealFreespinBeverly";						// Name of sound played if the FS game is chosen
	private const string REVEAL_PICKEM = "PortalRevealGusherBeverly";					// Name of sound played if the pickem game is chosen.



	// Our start to getting a portal to display
	public override void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		StartCoroutine(strikeOilThenBeginPortal(bannerRoots, banners, bannerOverlay, outcome, multiplier));
	}

	public IEnumerator strikeOilThenBeginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		// Start playing the music ASAP for this.
		Audio.switchMusicKeyImmediate("PortalBGBeverly");
		Audio.play(INTRO_VO);

		// Lets grab the BN symbol from the slot Reels. We know for this game it has to come from the second reel.
		SlotSymbol[] visibleSymbolsOnBNReel = SlotBaseGame.instance.engine.getVisibleSymbolsAt(1);
		bonusSymbol = null;
		// Get the bonus symbol.
		foreach (SlotSymbol symbol in visibleSymbolsOnBNReel)
		{
			if (symbol.name == "BN-4A-3A")
			{
				bonusSymbol = symbol;
				break;
			}
		}
		if (bonusSymbol == null)
		{
			Debug.LogError("Wasn't able to get the bonus symbol for the bev01 portal reveal effect.");
		}
		else
		{
			bonusSymbol.animator.deactivate();
		}

		// We need to grab the oil sprout effect.
		if (oilSpoutEffectPrefab != null)
		{
			oilSpoutEffect = CommonGameObject.instantiate(oilSpoutEffectPrefab) as GameObject;
			oilSpoutEffect.transform.parent = transform;
			oilSpoutEffect.transform.localPosition = oilSpoutEffectPrefab.transform.localPosition;
			oilSproutParticles = oilSpoutEffect.GetComponentsInChildren<ParticleSystem>();
			yield return new WaitForSeconds(TIME_BEFORE_PLAYING_BANNER_ANIMATION);
		}

		// Play the Effect of the oil rolling down the banners.
		_revealedIndex = -1;
		_banners = banners;
		_bannerRoots = bannerRoots;
		_bannerTextOverlay = CommonGameObject.instantiate(bannerOverlay) as GameObject;
		_outcome = outcome;
		_multiplier = multiplier;
		bonusAdded = false;
		spinsAdded = false;
		
		// Populate a map for the purposes of generating the banners.
		
		_bannerMap = new Dictionary<BannerEnum, SlotBaseGame.BannerInfo>();
		foreach (SlotBaseGame.BannerInfo banner in _banners)
		{
			_bannerMap.Add(banner.bannerType, banner);
		}

		bannerAdjustment = new Vector3(0.0f, SlotBaseGame.instance.getSymbolVerticalSpacingAt(1), 0.0f);
		_revealVfx = _bannerMap[BannerEnum.CLICKME].revealVfx;
		
		bannerObjects = new List<GameObject>();
		//Add the CLICKME banners to where they should be
		setupClickMeBanners();

		// Grab the click banners that were just added.
		bannerAnimators = new List<Animator>();
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
		if (bannerAnimators.Count > 0)
		{
			yield return new WaitForSeconds(TIME_GOOP_DOWN_ANIMATION);
			Audio.play(PICK_VO);
		}
		// Go through each of the particle systems and reduce their size so it looks like they are getting smaller.
		// We also want to change the scale over lifetime to match these changes.
		if (oilSproutParticles != null)
		{
			particleSystemStartSize = oilSproutParticles[0].main.startSizeMultiplier;
			iTween.ValueTo(gameObject, iTween.Hash("from", particleSystemStartSize, "to", particleSystemStartSize/2, "time", TIME_STOP_SPROUT, "onupdate", "updateParticleSystemSize", "easetype", iTween.EaseType.linear));
			iTween.ValueTo(gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "time", TIME_STOP_SPROUT, "onupdate", "updateParticleSystemLifetime", "easetype", iTween.EaseType.linear));
			yield return new WaitForSeconds(TIME_STOP_SPROUT);
			// We need to wait for one frame so that these things are no racing the destruction.
			yield return null;
		}
		
		BonusGameManager.instance.attachTextOverlay(_bannerTextOverlay);
		bannerTextObjects = CommonGameObject.findDirectChildren(_bannerTextOverlay);
		orginalBannerPositions = new Vector3[bannerTextObjects.Count,3];
		for (int i = 0; i < bannerTextObjects.Count; i++)
		{
			GameObject banner = bannerTextObjects[i];
			GameObject bannerText = CommonGameObject.findDirectChild(banner, "Text");
			GameObject headerText = CommonGameObject.findDirectChild(banner, "Header");
			GameObject footerText = CommonGameObject.findDirectChild(banner, "Footer");
			
			//We need to get the position of these texts right here.
			orginalBannerPositions[i, 0] = headerText.transform.localPosition;
			orginalBannerPositions[i, 1] = bannerText.transform.localPosition;
			orginalBannerPositions[i, 2] = footerText.transform.localPosition;
		}


		
		StartCoroutine("playPickMeAnimation");
		
	}

	private void updateBNSymbolUV(float v)
	{
		if (bonusSymbol != null)
		{
			Material symbolMaterial = bonusSymbol.animator.material;
			if (symbolMaterial != null)
			{
				Vector2 textureOffset = symbolMaterial.mainTextureOffset;
				textureOffset.y = v;
				symbolMaterial.mainTextureOffset = textureOffset;
			}
		}
	}

	private void updateParticleSystemSize(float size)
	{
		if (oilSproutParticles == null)
		{
			return;
		}
		foreach (ParticleSystem ps in oilSproutParticles)
		{
			if (ps == null)
			{
				continue;
			}
			ParticleSystem.MainModule mainParticleModule = ps.main;
			mainParticleModule.startSize = size;
		}
	}

	private void updateParticleSystemLifetime(float size)
	{
		if (oilSproutParticles == null)
		{
			return;
		}
		foreach (ParticleSystem ps in oilSproutParticles)
		{
			if (ps == null)
			{
				continue;
			}
			ParticleSystem.MainModule mainParticleModule = ps.main;
			mainParticleModule.startLifetime = size;
		}
	}

	// Plays the pickme Animation on the banner if one exsits while taking care to make sure only one is played at a time.
	protected override IEnumerator playPickMeAnimation()
	{
		while (bannerAnimators.Count > 0)
		{
			int bannerToAddPickmeAnimation = Random.Range(0, bannerAnimators.Count);
			Animator bannerAnimator = bannerAnimators[bannerToAddPickmeAnimation];
			if (bannerAnimator != null)
			{
				bannerAnimator.Play("Bev01_BnBanner_Pickme");
			}
			yield return new WaitForSeconds(Random.Range(1.0f, 3.5f));
		}
	}

	public override void beginPortalReveals(GameObject objClicked)
	{
		// Audio reveal on click
		if (GameState.game.keyName.Contains("lls"))
		{
			Audio.play("LWRevealBonus");
		}
		else if (GameState.game.keyName.Contains("oz00"))
		{
			Audio.play("ECportal_knock");
			Audio.play("summarywhoosh1");
			Audio.play("dmwellthatsmorelikeit", 1f, 0.0f, 0.5f);
		}
		else if (GameState.game.keyName.Contains("oz02"))
		{
			Audio.play("wwportal_pick_bonus");
		}
		else
		{
			//Stop the music that was playing before so it doesn't replay in the reveal.
			Audio.switchMusicKey("");
			Audio.play(Audio.soundMap("bonus_portal_reveal_bonus"));
		}

		disableAllBannerClicks();
		StopCoroutine("playPickMeAnimation");
		// Set up the extra camera for the reveal.
		GameObject cameraRoot = GameObject.Find("0 Camera");
		extraCamera.transform.parent = cameraRoot.transform;
		extraCamera.transform.localPosition = Vector3.zero;
		extraCamera.gameObject.SetActive(true);
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			CommonGameObject.setLayerRecursively(bannerTextObjects[i], LayerMask.NameToLayer("NGUI_UNDERLAY"));
		}
		this.StartCoroutine(doBeginPortalReveals(objClicked, doAnimateBannerAndRemove));
	}

	private IEnumerator doAnimateBannerAndRemove(GameObject banner)
	{
		// Play a special sound if this was the first banner revealed.
		if (firstPick)
		{
			if (_outcome.isChallenge)
			{
				Audio.play(REVEAL_PICKEM);
			}
			else if(_outcome.isGifting)
			{
				Audio.play(REVEAL_FS);
			}
			firstPick = false;
		}

		Animator bannerAnimator = banner.GetComponent<Animator>();

		// Play the reveal animation and wait for it to finish.
		if (bannerAnimator != null)
		{
			bannerAnimator.Play("Bev01_BnBanner_Reveal");
			bannerAnimators.Remove(bannerAnimator);
			yield return new WaitForSeconds(TIME_FOR_REVEAL);
		}
		else
		{
			Debug.LogWarning("There was no Animator on the banner to animate.");
		}
		Destroy(banner);
	}

	protected override void AnimateBannerFlyout(GameObject banner)
	{
		StartCoroutine(doAnimateBannerAndRemove(banner));
	}

	// Once the bonus starts we want to make the bonus symbol go back to the right position.
	protected override void beginBonus()
	{
		base.beginBonus();
		if (bonusSymbol != null)
		{
			bonusSymbol.animator.activate(true);
		}
		if (oilSpoutEffect != null)
		{
			Destroy(oilSpoutEffect);
		}
		extraCamera.transform.parent = this.gameObject.transform;
		extraCamera.gameObject.SetActive(false);

	}
}
