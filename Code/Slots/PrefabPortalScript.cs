using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BannerEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;
using TMPro;

public class PrefabPortalScript : PortalScript
{
	public GameObject backgroundAnchor;
	public GameObject backgroundPrefab;
	private GameObject backgroundObject;
	
	public string portalRevealAnimationName;
	public string freeSpinRevealAnimationName;
	public string challengeRevealAnimationName;
	public string creditRevealAnimationName;
	public string freeSpinNotPickedAnimationName;
	public string challengeNotPickedAnimationName;
	public string creditNotPickedAnimationName;
	// custom banner fade out animations
	public string freeSpinRevealFadeAnimationName;
	public string challengeRevealFadeAnimationName;
	public string creditRevealFadeAnimationName;
	public string freeSpinNotPickedFadeAnimationName;
	public string challengeNotPickedFadeAnimationName;
	public string creditNotPickedFadeAnimationName;

	public string pickMeAnimationName;
	public string idleAnimationName;

	public Vector3 bannerPositionOffset = new Vector3();
	public Vector3 bannerScale = new Vector3(1.0f, 1.0f, 1.0f);

	[SerializeField] private float DELAY_BETWEEN_BANNER_REVEALS = 1.0f;
	[SerializeField] private float MIN_PICKME_WAIT_TIME = 2.0f;
	[SerializeField] private float MAX_PICKME_WAIT_TIME = 3.0f;
	
	public float REVEAL_BONUS_VO_DELAY = 0.0f;
	public float REVEAL_BONUS_SOUND_DELAY = 0.0f;

	public List<string> creditLabelPaths;

	protected const string REVEAL_BONUS_SOUND_KEY = "bonus_portal_reveal_bonus";
	protected const string REVEAL_BONUS_VO_KEY = "bonus_portal_reveal_bonus_vo";
	protected const string PICKEM_TRANSITION_SOUND_KEY = "bonus_portal_transition_picking";
	protected const string FREESPINS_TRANSITION_SOUND_KEY = "bonus_portal_transition_freespins";

	[SerializeField] private float TRANSITION_ANIM_SOUND_DELAY = 0.0f;
	[SerializeField] private bool needsToPlayTransitionAnimationSounds = false;

	// Our start to getting a portal to display
	public override void beginPortal(GameObject[] bannerRoots, SlotBaseGame.BannerInfo[] banners, GameObject bannerOverlay, SlotOutcome outcome, long multiplier)
	{
		base.beginPortal(bannerRoots, banners, bannerOverlay, outcome, multiplier);
		
		// Modify banner scale and position
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			bannerObjects[i].transform.localPosition = bannerObjects[i].transform.localPosition + bannerPositionOffset;
			bannerObjects[i].transform.localScale = bannerScale;
			
			getAnimatorForBanner(bannerObjects[i]).Play(portalRevealAnimationName);
		}
		
		if (!string.IsNullOrEmpty(portalRevealAnimationName))
		{
			for (int i = 0; i < bannerObjects.Count; i++)
			{
				getAnimatorForBanner(bannerObjects[i]).Play(portalRevealAnimationName);
			}
		}
		
		if (!string.IsNullOrEmpty(pickMeAnimationName))
		{
			StartCoroutine("playPickMeAnimation");
		}
		
		if (backgroundPrefab != null)
		{
			if (backgroundAnchor != null)
			{
				backgroundObject = CommonGameObject.instantiate(backgroundPrefab) as GameObject;
				backgroundObject.transform.parent = backgroundAnchor.transform;
				
				backgroundObject.transform.localPosition = backgroundPrefab.transform.localPosition;
				backgroundObject.transform.localScale = backgroundPrefab.transform.localScale;
			}
			else
			{
				Debug.LogError(
					"You have to set the background anchor in order to use the background prefab.  " +
					"It instantiates the background at the anchor.");
			}
		}
	}

	// First call done by SlotBaseGame that will hide the obj clicked, and show the appropriate reveal.
	public override void beginPortalReveals(GameObject objClicked)
	{
		disableAllBannerClicks();

		this.StartCoroutine(doBeginPortalReveals(objClicked, null));
	}

	protected override IEnumerator doBeginPortalReveals(GameObject objClicked, RevealDelegate revealDelegate)
	{
		if (!string.IsNullOrEmpty(pickMeAnimationName))
		{
			StopCoroutine("playPickMeAnimation");
		}

		revealedObjects = new List<GameObject>();
		//It is possible to send the text object clicked on since clicking on the object itself may be impossible from a 
		//perspective camera, so we will check to see if we got a text object, and if so, grab the associated revealObject.
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			GameObject bannerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Text");
			if (objClicked.transform.parent.gameObject == bannerObjects[i] || objClicked == bannerText)
			{
				_revealedIndex = i;
			}
		}
		yield return null;
		bool letModulePlayRevealSounds = false;
		foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
		{
			if (module.needsToLetModulePlayPortalRevealSounds())
			{
				letModulePlayRevealSounds = true;
				yield return StartCoroutine(module.executeOnPlayPortalRevealSounds(_outcome));
			}
		}
		if (_outcome.isGifting)
		{
			if (!letModulePlayRevealSounds)
			{
				playFreespinRevealAudio ();
			}
			getAnimatorForBanner(bannerObjects[_revealedIndex]).Play(freeSpinRevealAnimationName);
			spinsAdded = true;
		}
		else if (_outcome.isChallenge)
		{
			if (!letModulePlayRevealSounds)
			{
				playChallengeRevealAudio ();
			}
			getAnimatorForBanner(bannerObjects[_revealedIndex]).Play(challengeRevealAnimationName);
			bonusAdded = true;
		}
		else
		{
			getAnimatorForBanner(bannerObjects[_revealedIndex]).Play(creditRevealAnimationName);

			string creditText = getCreditsBannerText(true);
			foreach(string s in creditLabelPaths)
			{
				// handle both UILabel and TMPro
				Transform bannerTarget = bannerObjects[_revealedIndex].transform.Find(s);
				assignCreditLabel(bannerTarget, creditText);
			}
		}

		if (!letModulePlayRevealSounds)
		{
			Audio.playWithDelay(Audio.soundMap(REVEAL_BONUS_SOUND_KEY), REVEAL_BONUS_SOUND_DELAY);
			Audio.playWithDelay(Audio.soundMap(REVEAL_BONUS_VO_KEY), REVEAL_BONUS_VO_DELAY);
		}
		revealedObjects.Add(bannerObjects[_revealedIndex]);
		Invoke("revealBanners", DELAY_BETWEEN_BANNER_REVEALS);
	}

	// Plays the pickme Animation on the banner if one exsits while taking care to make sure only one is played at a time.
	protected override IEnumerator playPickMeAnimation()
	{
		// wait before doing the first pick me animation
		yield return new WaitForSeconds(Random.Range(MIN_PICKME_WAIT_TIME, MAX_PICKME_WAIT_TIME));

		// reset this when starting this coroutine, in case a pick me was happening the last time the portal finished
		playingPickMeAnimation = false;

		int lastPlayedBanner = -1;
		while (true)
		{
			if (!playingPickMeAnimation)
			{
				int bannerToAddPickmeAnimation = Random.Range(0, bannerObjects.Count);

				if (lastPlayedBanner != -1 && bannerToAddPickmeAnimation == lastPlayedBanner)
				{
					bannerToAddPickmeAnimation++;
					if (bannerToAddPickmeAnimation >= bannerObjects.Count)
					{
						bannerToAddPickmeAnimation = 0;
					}
				}

				Audio.play(Audio.soundMap("bonus_portal_pickme"));
				getAnimatorForBanner(bannerObjects[bannerToAddPickmeAnimation]).Play(pickMeAnimationName);
				playingPickMeAnimation = true;
				yield return new WaitForSeconds(Random.Range(MIN_PICKME_WAIT_TIME, MAX_PICKME_WAIT_TIME));
				playingPickMeAnimation = false;
                if (!string.IsNullOrEmpty(idleAnimationName))
                {
                    getAnimatorForBanner(bannerObjects[bannerToAddPickmeAnimation]).Play(idleAnimationName);
                }
                lastPlayedBanner = bannerToAddPickmeAnimation;
			}
			yield return null;
		}
		
	}


	//don't do anything here
	protected override void setCorrectLabel(SlotBaseGame.BannerTextInfo banner, LabelWrapper header, LabelWrapper center, LabelWrapper footer)
	{
	}

	/// Allowing for the animator to be not at the root of the banner game object
	private Animator getAnimatorForBanner(GameObject banner)
	{
		// first check at the root of the banner object
		Animator bannerAnimator = banner.GetComponent<Animator>();

		if (bannerAnimator == null)
		{
			// couldn't find anything at root, so try in children
			bannerAnimator = banner.GetComponentInChildren<Animator>();
		}

		if (bannerAnimator == null)
		{
			Debug.LogError("PrefabPortalScript:getAnimatorForBanner() - Couldn't find Animator!");
		}

		return bannerAnimator;
	}

	// Assign a credit label to a UILabel or a TMPro component
	private void assignCreditLabel(Transform target, string creditText)
	{
		UILabel creditLabel = target.GetComponent<UILabel>();
		if (creditLabel != null)
		{
			creditLabel.text = creditText;
		}
		else
		{
			TextMeshPro tmpCreditLabel = target.GetComponent<TextMeshPro>();
			if(tmpCreditLabel != null)
			{
				tmpCreditLabel.text = creditText;
			}
		}
	}


	// Invoked twice to make sure revealed banners display the appropriate banners.
	// If 'bannerObjects.Count' objects have been added, we start the game process, otherwise, we show the last banner.
	protected override void revealBanners()
	{
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			if (bannerObjects[i] != null && i != _revealedIndex && !revealedObjects.Contains(bannerObjects[i]))
			{

				if (_outcome.isChallenge)
				{	
					if (!spinsAdded)
					{
						getAnimatorForBanner(bannerObjects[i]).Play(freeSpinNotPickedAnimationName);
						spinsAdded = true;
					}
					else
					{
						getAnimatorForBanner(bannerObjects[i]).Play(creditNotPickedAnimationName);

						string creditText = getCreditsBannerText(true);
						foreach(string s in creditLabelPaths)
						{
							// handle both UILabel and TMPro
							Transform bannerTarget = bannerObjects[i].transform.Find(s);
							assignCreditLabel(bannerTarget, creditText);
						}
					}
				}
				else if (_outcome.isGifting)
				{
					if (!bonusAdded)
					{
						getAnimatorForBanner(bannerObjects[i]).Play(challengeNotPickedAnimationName);
						bonusAdded = true;
					}
					else
					{
						getAnimatorForBanner(bannerObjects[i]).Play(creditNotPickedAnimationName);

						string creditText = getCreditsBannerText(true);
						foreach(string s in creditLabelPaths)
						{
							// handle both UILabel and TMPro
							Transform bannerTarget = bannerObjects[i].transform.Find(s);
							assignCreditLabel(bannerTarget, creditText);
						}
					}
				}
				else if (_outcome.isCredit)
				{
					if (!bonusAdded)
					{
						getAnimatorForBanner(bannerObjects[i]).Play(challengeNotPickedAnimationName);
						bonusAdded = true;
					}
					else
					{
						getAnimatorForBanner(bannerObjects[i]).Play(freeSpinNotPickedAnimationName);
						spinsAdded = true;
					}
				}

				revealedObjects.Add(bannerObjects[i]);
				break;
			}
		}
		
		if (revealedObjects.Count == bannerObjects.Count)
		{
			Invoke("beginBonus", BEGIN_BONUS_DELAY_TIME);
		}
		else
		{
			Invoke("revealBanners", 1.0f);
		}

		Audio.play(Audio.soundMap("bonus_portal_reveal_others"));
	}

	// fade out portal banners with custom animations assigned by type
	protected override IEnumerator fadePortalBanners()
	{
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			if (bannerObjects[i] != null)
			{
				Animator bannerAnimator = getAnimatorForBanner(bannerObjects[i]);
				AnimatorStateInfo currentBannerState = bannerAnimator.GetCurrentAnimatorStateInfo(0);

				// test current banner revealed state by animation (enums only yield initial states)
				// play fadeout animation for the revealed banner
				if (currentBannerState.IsName(challengeRevealAnimationName) && _outcome.isChallenge)
				{
					bannerAnimator.Play(challengeRevealFadeAnimationName);
				}
				else if (currentBannerState.IsName(challengeNotPickedAnimationName) && !_outcome.isChallenge)
				{
					bannerAnimator.Play(challengeNotPickedFadeAnimationName);
				}
				else if (currentBannerState.IsName(freeSpinRevealAnimationName) && _outcome.isGifting)
				{
					bannerAnimator.Play(freeSpinRevealFadeAnimationName);
				}
				else if (currentBannerState.IsName(freeSpinNotPickedAnimationName) && !_outcome.isGifting)
				{
					bannerAnimator.Play(freeSpinNotPickedFadeAnimationName);
				}
				else if (currentBannerState.IsName(creditRevealAnimationName) && _outcome.isCredit)
				{
					bannerAnimator.Play(creditRevealFadeAnimationName);
				}
				else if (currentBannerState.IsName(creditNotPickedAnimationName) && !_outcome.isCredit)
				{
					bannerAnimator.Play(creditNotPickedFadeAnimationName);
				}

			}
		}
		yield return StartCoroutine(base.fadePortalBanners());
	}
	
	protected override void destroyPortalBanners()
	{
		if (backgroundObject != null)
		{
			GameObject.Destroy(backgroundObject);
		}
		
		base.destroyPortalBanners();

		bool letModulePlayTransitionSounds = false;
		foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
		{
			if (module.needsToLetModulePlayPortalTransitionSounds())
			{
				letModulePlayTransitionSounds = true;
				StartCoroutine(module.executeOnPlayPortalTransitionSounds(_outcome));
			}
		}
		if (needsToPlayTransitionAnimationSounds && !letModulePlayTransitionSounds)
		{
			playTransitionAnimationSounds();
		}
	}

	protected void playTransitionAnimationSounds()
	{
		if (_outcome.isChallenge)
		{
			if (Audio.canSoundBeMapped(PICKEM_TRANSITION_SOUND_KEY))
			{
				Audio.playWithDelay(Audio.soundMap(PICKEM_TRANSITION_SOUND_KEY), TRANSITION_ANIM_SOUND_DELAY);
			}
		}
		else if (_outcome.isGifting)
		{
			if (Audio.canSoundBeMapped(FREESPINS_TRANSITION_SOUND_KEY))
			{
				Audio.playWithDelay(Audio.soundMap(FREESPINS_TRANSITION_SOUND_KEY), TRANSITION_ANIM_SOUND_DELAY);
			}
		}
	}

}
