using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/* This class is responsible for all big win effects
 * fade in all graphics.
 * show big win rolloup anim
 * fade out to normal
 */
public class BigWinEffect : TICoroutineMonoBehaviour
{
	public UILabel amountLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent amountLabelWrapperComponent;
	//protected GameObject tapToSkipIcon;

	public bool isEnding
	{
		get;
		private set;
	}

	public LabelWrapper amountLabelWrapper
	{
		get
		{
			if (_amountLabelWrapper == null)
			{
				if (amountLabelWrapperComponent != null)
				{
					_amountLabelWrapper = amountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_amountLabelWrapper = new LabelWrapper(amountLabel);
				}
			}
			return _amountLabelWrapper;
		}
	}
	private LabelWrapper _amountLabelWrapper = null;
	
	public TextMeshPro amountTMPro;	// If used, amountLabel should be null.
	public GameObject wingsPrefab;
	public Color wingsColor;
	public bool skipHideGame = false;
	public bool skipFadeIn = false;
	[Tooltip("If the duration determined by waitForLongestAnimation exceeds this value, we will use this capped value instead.  The value can be increased if a big win has a longer minimum anim loop to show.")]
	public float waitForLongestAnimationDurationCap = 5.0f;

	public AnimationListController.AnimationInformationList introAnims; // intro anims, if defined will override the fading
	public AnimationListController.AnimationInformationList outroAnims; // outro anims, if defined will override the fading
	
	[HideInInspector] public bool isAnimating = false;
	[HideInInspector] public BigWinEndDelegate bigWinEndCallback;
	[HideInInspector] public long payout = 0;

	private List<ParticleSystem> _particleSystems;
	private GameObject _wings = null;

	public GameObject[] fadingObjectsToIgnore;
	private List<GameObject> fadingObjectsToIgnoreList = new List<GameObject>();
	private bool didFinishIntro;
	private Animator[] animators = null;
	private TICoroutine waitForAnimationsCoroutine;
	
	void Awake()
	{
		setupBigWin();
	}

	protected void setupBigWin()
	{
		isEnding = false;

		// Create wings if not at 4:3 aspect ratio (also includes top and bottom wings if necessary).
		if (wingsPrefab != null && NGUIExt.aspectRatio > 1.33f && NGUIExt.uiRoot != null)
		{
			_wings = CommonGameObject.instantiate(wingsPrefab) as GameObject;
			_wings.transform.parent = NGUIExt.uiRoot.transform;
			_wings.transform.localScale = Vector3.one;
			_wings.transform.position = new Vector3(0, 10000, 0);	// Make sure this is not actually behind the big win effect.
		}

		// Cameras aren't always consistent and we seem to have some minor problems
		// with just adding it like we do wings. Gonna comment out the tap to skip
		// icon until we can resolve it.
		//if (tapToSkipIcon == null)
		//{
		//	tapToSkipIcon = Resources.Load("Prefabs/Misc/Tap To Skip") as GameObject;
		//	fadingObjectsToIgnoreList.Add(tapToSkipIcon);
		//}

	
		//tapToSkipIcon = NGUITools.AddChild(NGUIExt.uiRoot.gameObject, tapToSkipIcon, false);
		//tapToSkipIcon.transform.position = new Vector3(0, 10000, 0);
		//CommonGameObject.setLayerRecursively(tapToSkipIcon, Layers.ID_BIG_WIN_2D);

		isAnimating = true;

		foreach (GameObject go in fadingObjectsToIgnore)
		{
			fadingObjectsToIgnoreList.Add(go);
		}

		fadingObjectsToIgnore = null;
		animators = GetComponentsInChildren<Animator>(true);
		ParticleSystem[] particleSystemsArray = gameObject.GetComponentsInChildren<ParticleSystem>();
		_particleSystems = new List<ParticleSystem>(particleSystemsArray);
		waitForAnimationsCoroutine = StartCoroutine(waitForLongestAnimation());
		
		// If on a slower device, skip the fading in.
		if (!MobileUIUtil.isSlowDevice && !skipFadeIn && (introAnims == null || introAnims.Count == 0))
		{
			foreach (Animator anim in animators)
			{
				anim.enabled = false;
			}
			didFinishIntro = false;
			updateFade(0);
			iTween.ValueTo(this.gameObject,
				iTween.Hash(
					"from", 0f,
					"to", 1f,
					"time", 1,
					"onupdate", "updateFade",
					"oncomplete", "fadeInFinished"
					)
			   );
		}
		else if (introAnims != null && introAnims.Count > 0)
		{
			// we have intro animation to do
			StartCoroutine(playIntroAnims());
		}
		else
		{
			// skip doing any intro
			didFinishIntro = true;
			hideGame();
		}
	}
	
	private IEnumerator waitForLongestAnimation()
	{
		float longestDuration = 0f;
		foreach (Animator animator in animators)
		{
			float duration = animator.GetCurrentAnimatorStateInfo(0).length;
			if (duration > longestDuration)
			{
				longestDuration = duration;
			}
		}

		// If the longest duration exceeds the cap, we'll set it to the cap.
		// This will ensure that if there is a really long animation for something
		// like an ambient effect it will not be forced to wait the full duration
		// of that animation.
		if (longestDuration >= waitForLongestAnimationDurationCap)
		{
			longestDuration = waitForLongestAnimationDurationCap;
		}
		
		yield return new WaitForSeconds(longestDuration);
	}

	private IEnumerator playIntroAnims()
	{
		if (introAnims != null && introAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnims));
		}

		didFinishIntro = true;
		hideGame();
	}

	private void fadeInFinished()
	{
		foreach (Animator anim in animators)
		{
			anim.enabled = true;
		}
		didFinishIntro = true;
		hideGame();
	}
	
	private void hideGame()
	{
		if (skipHideGame)
		{
			return;
		}
		// Hide the base game & the paylines.
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.hideGame(this.gameObject);
		}
	}

	private void showGame()
	{
		if (skipHideGame)
		{
			return;
		}
		// Show the base game & paylines.
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.showGame();
		}
	}

	public void setAmount(long value)
	{
		if (amountLabelWrapper != null)
		{
			amountLabelWrapper.text = CreditsEconomy.convertCredits(value);
		}
		if (amountTMPro != null)
		{
			amountTMPro.text = CreditsEconomy.convertCredits(value);
		}
	}
	
	// Called from SlotBaseGame when rollUp ends
	public virtual IEnumerator endBigWin()
	{
		isEnding = true;

		// We shouldn't end the big win until we have finished the intro
		while (!didFinishIntro)
		{
			yield return null;
		}

		if (waitForAnimationsCoroutine != null)
		{
			yield return waitForAnimationsCoroutine;
		}

		rollupComplete();
	}
	
	// stop all particle effects and fade all the materials
	public virtual void rollupComplete()
	{
		showGame();

		if (outroAnims != null && outroAnims.Count > 0)
		{
			// we have an outro anim to play instead of a fade
			StartCoroutine(playOutroAnims());
		}
		else if (MobileUIUtil.isSlowDevice)
		{
			// If on a slower device, skip the fading out.
			terminateAllAnimationsAndParticles();
			onOutroComplete();
		}
		else
		{
			terminateAllAnimationsAndParticles();
			StartCoroutine(fadeBigWinEffectOut(1.0f));
		}
	}

	// Fades the big win effect out, starting from current alpha values, then calls onOutroComplete()
	private IEnumerator fadeBigWinEffectOut(float duration)
	{
		yield return StartCoroutine(CommonGameObject.fadeGameObjectToFromCurrent(this.gameObject, 0.0f, duration));
		onOutroComplete();
	}

	// Stops all particle systems and animators so that the game can correctly fade them out
	private void terminateAllAnimationsAndParticles()
	{
		for (int i = 0; i < _particleSystems.Count; ++i)
		{
			if (_particleSystems[i] == null)
			{
				continue;
			}

			_particleSystems[i].Stop();
			CommonEffects.setEmissionEnable(_particleSystems[i], false);
		}

		foreach (Animator anim in animators)
		{
			Destroy(anim);
		}
	}

	// handle playing the outro animations which serve in place of fading out
	private IEnumerator playOutroAnims()
	{
		if (outroAnims != null && outroAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnims));
		}

		onOutroComplete();
	}
	
	// fades out all the materials and labels
	public void updateFade(float value)
	{
		if (_wings != null)
		{
			CommonGameObject.alphaGameObject(_wings, value);
		}
		CommonGameObject.alphaGameObject(gameObject, value, fadingObjectsToIgnoreList);
		
		if (amountLabelWrapper != null)
		{
			amountLabelWrapper.alpha = value;
		}
		if (amountTMPro != null)
		{
			amountTMPro.alpha = value;
		}
	}
	
	// when fadeout ends, set state to isAnimating=false and call the bigWinEndCallback
	public void onOutroComplete()
	{
		Destroy(_wings);
		_wings = null;
		isAnimating = false;
		bigWinEndCallback(payout);
	}
}

public delegate void BigWinDelegate(long payout, bool isSettingStartingAmountToPayout);
public delegate void BigWinEndDelegate(long payout);
