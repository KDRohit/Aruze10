using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;// For array.Contains

/**
 * WildBannermultiReelReplacementModule.cs
 * Module used in gen13 base and freespins for vertical wild feature
 * Can be used in any game with vertical wilds where you want to instantiated effects and banners each time
 * Author: Nick Reynolds
 * Based on Pb01MultiReelReplacementModule.cs
 */ 
public class DeprecatedWildBannerMultiReelReplacementModule : MultiReelReplacementModule
{
	[Header("Intro Animation")]
	[SerializeField] protected bool shouldInstantiateIntro = true;
	[SerializeField] protected GameObject introPrefab;	
	[SerializeField] protected Vector3 INTRO_OFFSET;
	[SerializeField] protected string INTRO_ANIMATION_NAME;
	[SerializeField] protected string INTRO_TEASER_ANIMATION_NAME;
	[SerializeField] protected string[] MULTIPLE_INTRO_ANIMATION_NAMES;
	[SerializeField] protected string[] MULTIPLE_INTRO_TEASER_ANIMATION_NAMES;
	[SerializeField] protected float INTRO_WAIT_TIME;
	[SerializeField] protected float INTRO_DELAY_BEFORE_HIDE = 0.0f;
	[SerializeField] protected string INTRO_SOUND_BG;
	[SerializeField] protected float INTRO_SOUND_BG_DELAY;
	[SerializeField] protected string INTRO_FADE_SOUND;
	[SerializeField] protected float INTRO_FADE_SOUND_DELAY;
	[SerializeField] protected string INTRO_SOUND;
	[SerializeField] protected float INTRO_SOUND_DELAY;
	[SerializeField] protected string INTRO_SOUND_VO;
	[SerializeField] protected float INTRO_SOUND_VO_DELAY;
	[SerializeField] protected string TEASER_INTRO_SOUND;
	[SerializeField] protected float TEASER_INTRO_SOUND_DELAY;
	[SerializeField] protected string TEASER_INTRO_SOUND_VO;
	[SerializeField] protected float TEASER_INTRO_SOUND_VO_DELAY;
	[SerializeField] protected float EXTERNAL_INTRO_WAIT;
	[SerializeField] protected bool SHOULD_DO_FADE_BEFORE_INTRO_ANIM;

	[Header("Wild Banner Effect")]	
	// first 2 arrays act like a dictionary, mapping symbol names to the symbol banner that should be created
	[SerializeField] protected string[] bannerSymbolNames;
	[SerializeField] protected GameObject[] bannerSymbolPrefabs;
	[SerializeField] protected GameObject prePlaceWildBannerEffectPrefab;
	[SerializeField] protected float prePlaceAnimationLength;
	[SerializeField] protected GameObject wildBannerEffectPrefab;
	protected GameObjectCacher[] bannerSymbolCachers;
	protected GameObjectCacher prePlaceBannerEffectCacher;
	protected GameObjectCacher bannerEffectCacher;
	[SerializeField] protected GameObject[] wildBannerParents;
	[SerializeField] protected Vector3[] wildBannerScalesOnReels;
	[SerializeField] protected Vector3 PRE_PLACE_WILD_BANNER_EFFECT_OFFSET;
	[SerializeField] protected Vector3 WILD_BANNER_EFFECT_OFFSET;
	[SerializeField] protected Vector3 WILD_BANNER_SYMBOL_OFFSET;
	[SerializeField] protected float WILD_BANNER_ANIMATION_SPEED = 1.0f; // override if you need to slow this down for some reason
	[SerializeField] protected string[] WILD_BANNER_ANIMATION_NAMES;
	[SerializeField] protected string WILD_BANNER_PAYLINE_ANIMATION_NAME;
	[SerializeField] protected float TIME_BETWEEN_BANNERS;

	// some banners might have different animation lengths per animation
	[SerializeField] protected float[] WAIT_BEFORE_CREATING_SYMBOL_BANNERS;
	[SerializeField] protected float[] WAIT_BEFORE_DESTROYING_EFFECTS;
	[SerializeField] protected Dictionary<string, GameObject> bannerSymbolsBySymbolName;
	[SerializeField] protected bool shouldDestroyBannersOnEndPaylines;
	[SerializeField] private List<FadeObject> fadeObjects;
	[SerializeField] private float FADE_IN_TIME;
	[SerializeField] private float FADE_OUT_TIME;
	
	// If you got multiple banner in once spin, then only play the sound on the first banner.
	// For example, if each banner plays a VO collection, this will prevent it from playing multiple VOs at the same time.
	[SerializeField] protected bool shouldOnlyPlayFirstBannerSound = false;
	[SerializeField] protected bool shouldMutateSymbolsBehindBanners = false;

	protected int numWildBanners = 0;
	protected GameObject[] wildBanners;
	protected Dictionary<GameObject, GameObjectCacher> instantiatedBanners = new Dictionary<GameObject, GameObjectCacher>();
	protected string[] VERTICAL_WILD_INIT_KEYS = {
		"basegame_vertical_wild_init",
		"freespin_vertical_wild_init"
	};
	[SerializeField] protected float VERTICAL_WILD_PRE_PLACE_SOUND_DELAY;
	//This is used to determine whether we should be playing reveal sound 1 or 2
	protected int revealIndex = 0;
	protected string[,] VERTICAL_WILD_PRE_PLACE_KEYS = {
		{"basegame_vertical_wild_prereveal1", "basegame_vertical_wild_prereveal2"},
		{"freespin_vertical_wild_prereveal1", "freespin_vertical_wild_prereveal2"}
	};
	[SerializeField] protected float VERTICAL_WILD_INIT_SOUND_DELAY;
	protected string[] VERTICAL_WILD_REVEAL_KEYS = {
		"basegame_vertical_wild_reveal",
		"freespin_vertical_wild_reveal"
	};
	[SerializeField] protected float VERTICAL_WILD_REVEAL_SOUND_DELAY;
	protected string[] VERTICAL_WILD_REVEAL_VO_KEYS = {
		"basegame_vertical_wild_reveal_vo",
		"freespin_vertical_wild_reveal_vo"
	};
	[SerializeField] protected float VERTICAL_WILD_REVEAL_VO_SOUND_DELAY;
	protected int gameType = 0; // 0 = base game, 1 = free spin. Find this when feature begins by checking which game is active.

	[SerializeField] private bool playIntroOnEachReel = false;
	[SerializeField] private float TIME_BETWEEN_INTRO_ANIMATIONS;
	[SerializeField] private float TIME_BEFORE_TEASER_ANIMATIONS_START;				// Used to stagger teasers from intro animations
	[SerializeField] private bool shouldPlayTeasers = false;
	private bool[] teasersPlaying = null;

	public override void Awake()
	{
		base.Awake();
		bannerSymbolCachers = new GameObjectCacher[bannerSymbolPrefabs.Length];
		for (int i = 0; i < bannerSymbolPrefabs.Length; i++)
		{
			bannerSymbolCachers[i] = new GameObjectCacher(this.gameObject, bannerSymbolPrefabs[i]);
		}
		bannerEffectCacher = new GameObjectCacher(this.gameObject, wildBannerEffectPrefab);
		prePlaceBannerEffectCacher = new GameObjectCacher(this.gameObject, prePlaceWildBannerEffectPrefab);

		teasersPlaying = new bool[wildBannerParents.Length];
		wildBanners = new GameObject[wildBannerParents.Length];
	}
	
	private IEnumerator waitThenPlayMusic()
	{
		yield return new TIWaitForSeconds(INTRO_SOUND_BG_DELAY);
		Audio.playMusic(Audio.soundMap(INTRO_SOUND_BG));
	}

	private IEnumerator hideOrDestroyIntro(GameObject introGameObject)
	{
		// Don't waste frames if this is zero
		if(INTRO_DELAY_BEFORE_HIDE > 0.0f)
		{
			yield return new TIWaitForSeconds(INTRO_DELAY_BEFORE_HIDE);
		}
		if(shouldInstantiateIntro)
		{
			Destroy(introGameObject);
		}
		else
		{
			introGameObject.SetActive(false);
		}
	}

	// This will kickoff at the same as the Fase In intro
	public virtual IEnumerator doIntro()
	{
		if(Audio.canSoundBeMapped(INTRO_SOUND_BG))
		{
			if (INTRO_SOUND_BG_DELAY > .01f)
			{
				StartCoroutine(waitThenPlayMusic());
			}
			else
			{
				Audio.playMusic(Audio.soundMap(INTRO_SOUND_BG), INTRO_SOUND_BG_DELAY);
			}
		}

		Audio.play(Audio.soundMap(INTRO_SOUND), 1.0f, 0.0f, INTRO_SOUND_DELAY);
		Audio.play(Audio.soundMap(INTRO_SOUND_VO), 1.0f, 0.0f, INTRO_SOUND_VO_DELAY);

		GameObject introGameObject = null;
		if (introPrefab != null)
		{
			if(shouldInstantiateIntro)
			{
				introGameObject = CommonGameObject.instantiate(introPrefab) as GameObject;
			}
			else
			{
				introGameObject = introPrefab;
			}

			introGameObject.transform.parent = reelGame.gameScaler.transform;
			introGameObject.transform.localPosition = INTRO_OFFSET;
			introGameObject.transform.localScale = Vector3.one;
			introGameObject.SetActive(true);		

			Animator animator = introGameObject.GetComponent<Animator>();
			if (animator != null)
			{
				animator.Play(INTRO_ANIMATION_NAME);
			}
		}
		yield return new TIWaitForSeconds(INTRO_WAIT_TIME);

		StartCoroutine(hideOrDestroyIntro(introGameObject));
	}

	// This will kickoff at the same as the Fase In intro
	public virtual IEnumerator doReelSpecificIntro()
	{
		if (featureMutation.type == "multi_reel_advanced_replacement")
		{
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{
					int reelID = featureMutation.mutatedReels[i][j];

					StartCoroutine(playIntroAnimationAt(reelID));
					yield return new TIWaitForSeconds(TIME_BETWEEN_INTRO_ANIMATIONS);
				}
			}
		}
		else
		{
			for (int i = 0; i < featureMutation.reels.Length; i++)
			{
				int reelID = featureMutation.reels[i];
				StartCoroutine(playIntroAnimationAt(reelID));
				yield return new TIWaitForSeconds(TIME_BETWEEN_INTRO_ANIMATIONS);
			}
		}
	}

	private IEnumerator playTeasers()
	{
		List<int> teaserReels = new List<int>();

		// Teaser reels are reels that are not feature mutations.
		for (int reelID = 0; reelID < wildBannerParents.Length; reelID++)
		{
			if (!featureMutation.reels.Contains(reelID))
			{
				teaserReels.Add(reelID);
			}
		}

		// Always play at least one teaser, but don't always play all of them.
		// Do it by randomly removing some teasers from the list.
		int numToRemove = Random.Range(2, teaserReels.Count);
		for (int iRemove = 0; iRemove < numToRemove; iRemove++)
		{
			int indexToRemove = Random.Range(0, teaserReels.Count);
			teaserReels.RemoveAt(indexToRemove);
		}

		for (int iTeaser = 0; iTeaser < teaserReels.Count; iTeaser++)
		{
			int reelID = teaserReels[iTeaser];
			teasersPlaying[reelID] = true;
		}

		yield return new WaitForSeconds(TIME_BEFORE_TEASER_ANIMATIONS_START);

		// Play the teasers.
		for (int iTeaser = 0; iTeaser < teaserReels.Count; iTeaser++)
		{
			int reelID = teaserReels[iTeaser];
			StartCoroutine(playIntroAnimationAt(reelID, true));
			yield return new WaitForSeconds(TIME_BETWEEN_INTRO_ANIMATIONS);
			teasersPlaying[reelID] = false;
		}
	}

	private IEnumerator playIntroAnimationAt(int reelID, bool isTeaser = false)
	{
		string animationName = INTRO_ANIMATION_NAME;
		string teaserAnimationName = INTRO_TEASER_ANIMATION_NAME;

		if(MULTIPLE_INTRO_ANIMATION_NAMES != null && MULTIPLE_INTRO_ANIMATION_NAMES.Length > 1)
		{
			int index = Random.Range(0, MULTIPLE_INTRO_ANIMATION_NAMES.Length);
			animationName = MULTIPLE_INTRO_ANIMATION_NAMES[index];
		}

		if(MULTIPLE_INTRO_TEASER_ANIMATION_NAMES != null && MULTIPLE_INTRO_TEASER_ANIMATION_NAMES.Length > 1)
		{
			int index = Random.Range(0, MULTIPLE_INTRO_TEASER_ANIMATION_NAMES.Length);
			teaserAnimationName = MULTIPLE_INTRO_TEASER_ANIMATION_NAMES[index];		
		}

		if(Audio.canSoundBeMapped(INTRO_SOUND_BG))
		{
			if (INTRO_SOUND_BG_DELAY > .01f)
			{
				StartCoroutine(waitThenPlayMusic());
			}
			else
			{
				Audio.playMusic(Audio.soundMap(INTRO_SOUND_BG), INTRO_SOUND_BG_DELAY);
			}
		}
		if (isTeaser)
		{
			Audio.play(Audio.soundMap(TEASER_INTRO_SOUND), 1.0f, 0.0f, TEASER_INTRO_SOUND_DELAY);
			Audio.play(Audio.soundMap(TEASER_INTRO_SOUND_VO), 1.0f, 0.0f, TEASER_INTRO_SOUND_VO_DELAY);
		}
		else
		{
			Audio.play(Audio.soundMap(INTRO_SOUND), 1.0f, 0.0f, INTRO_SOUND_DELAY);
			Audio.play(Audio.soundMap(INTRO_SOUND_VO), 1.0f, 0.0f, INTRO_SOUND_VO_DELAY);
		}

		GameObject introGameObject = null;
		if (introPrefab)
		{
			if (shouldInstantiateIntro)
			{
				introGameObject = CommonGameObject.instantiate(introPrefab) as GameObject;
			}
			else
			{
				introGameObject = introPrefab;
			}

			introGameObject.transform.parent = wildBannerParents[reelID].transform;
			introGameObject.transform.localPosition = INTRO_OFFSET;
			introGameObject.transform.localScale = Vector3.one;
			introGameObject.SetActive(true);	

			Animator animator = introGameObject.GetComponent<Animator>();
			if (animator != null)
			{
				if (isTeaser)
				{
					animator.Play(teaserAnimationName);
				}
				else
				{
					animator.Play(animationName);
				}
			}
		}
		yield return new TIWaitForSeconds(INTRO_WAIT_TIME);

		StartCoroutine(hideOrDestroyIntro(introGameObject));
	}

	private IEnumerator fadeInFeatureObjects()
	{
		Audio.play(Audio.soundMap(INTRO_FADE_SOUND), 1.0f, 0.0f, INTRO_FADE_SOUND_DELAY);
		iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", FADE_IN_TIME, "onupdate", "updateAlphaValue"));
		yield return new TIWaitForSeconds(FADE_IN_TIME);
	}

	private IEnumerator fadeOutFeatureObjects()
	{
		iTween.ValueTo(gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "time", FADE_OUT_TIME, "onupdate", "updateAlphaValue"));
		yield return new TIWaitForSeconds(FADE_OUT_TIME);
	}
	
	public void updateAlphaValue(float value)
	{
		foreach (FadeObject fadeObject in fadeObjects)
		{
			CommonGameObject.alphaGameObject(fadeObject.gameObject, fadeObject.startAmount + ((fadeObject.finishAmount - fadeObject.startAmount) * value));
		}
	}
	
	// when reels are spinning, bring up the effects & banner
	public override IEnumerator executePreReelsStopSpinning()
	{
		if (reelGame is FreeSpinGame)
		{
			gameType = 1;
		}
		else
		{
			gameType = 0;
		}
		
		numWildBanners = 0;
		System.Array.Clear(wildBanners, 0, wildBanners.Length);

		if (SHOULD_DO_FADE_BEFORE_INTRO_ANIM)
		{
			yield return StartCoroutine(fadeInFeatureObjects());
		}
		// Animated Intro
		if (playIntroOnEachReel)
		{
			StartCoroutine(doReelSpecificIntro());
		}
		else
		{
			StartCoroutine(doIntro());
		}

		if (shouldPlayTeasers)
		{
			StartCoroutine(playTeasers());
		}

		if (!SHOULD_DO_FADE_BEFORE_INTRO_ANIM)
		{
			yield return StartCoroutine(fadeInFeatureObjects());
		}
		
		yield return new TIWaitForSeconds(EXTERNAL_INTRO_WAIT);
		Audio.play(Audio.soundMap(VERTICAL_WILD_REVEAL_VO_KEYS[gameType]), 1.0f, 0.0f, VERTICAL_WILD_REVEAL_VO_SOUND_DELAY);


		if (featureMutation.type == "multi_reel_advanced_replacement")
		{
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{
					int reelID = featureMutation.mutatedReels[i][j];
					string symbol = featureMutation.mutatedSymbols[i];

					StartCoroutine(playWildBannerAt(reelID, getBannerFromSymbolName(symbol)));
					yield return new TIWaitForSeconds(TIME_BETWEEN_BANNERS);
					numWildBanners++;
				}
			}
		}
		else
		{
			for (int i = 0; i < featureMutation.reels.Length; i++)
			{
				int reelID = featureMutation.reels[i];
				StartCoroutine(playWildBannerAt(reelID, getBannerFromSymbolName(featureMutation.symbol)));
				yield return new TIWaitForSeconds(TIME_BETWEEN_BANNERS);
				numWildBanners++;
			}
		}

		while (teasersPlaying.Contains(true))
		{
			yield return null;
		}

		yield return StartCoroutine(fadeOutFeatureObjects());

	}

	// make sure symbols don't animate
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		StartCoroutine(base.executeOnReelsStoppedCallback());
		
		if (featureMutation == null)
		{
			Debug.LogError("Trying to execute module on invalid data.");
			yield break;
		}
		if (featureMutation.type == "multi_reel_advanced_replacement")
		{
			for (int i = 0; i < featureMutation.mutatedReels.Length; i++)
			{
				for (int j = 0; j < featureMutation.mutatedReels[i].Length; j++)
				{
					foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(featureMutation.mutatedReels[i][j]))
					{						
						// skip animaitons so we don't see the wild animations going on under the banner
						if (symbol.name != featureMutation.mutatedSymbols[i])
						{
							symbol.mutateTo(featureMutation.mutatedSymbols[i]);
						}
						symbol.skipAnimationsThisOutcome();
					}
				}
			}
		}
		else
		{
			foreach (int reelID in featureMutation.reels)
			{
				foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
				{
					// skip animaitons so we don't see the wild animations going on under the banner
					if (symbol.name != featureMutation.symbol)
					{
						symbol.mutateTo(featureMutation.symbol);
					}
					symbol.skipAnimationsThisOutcome();
				}
			}
		}
	}

	
	// We use a semi-hacky thing to treat 2 arrays like a dictionary, since dictionaries aren't serialized in Unity
	private GameObjectCacher getBannerFromSymbolName(string symbolName)
	{
		int index = 0;
		for (index=0; index < bannerSymbolNames.Length; index++)
		{
			if (bannerSymbolNames[index] == symbolName)
			{
				break;
			}
		}
		
		if (index >= bannerSymbolPrefabs.Length)
		{
			Debug.LogError("Symbol definition was not found");
			return null;
		}
		
		return (bannerSymbolCachers[index]);
	}

	// create effects and symbol banner, in that order
	protected virtual IEnumerator playWildBannerAt(int reelID, GameObjectCacher bannerPrefab)
	{
		int bannerIndex = numWildBanners % WILD_BANNER_ANIMATION_NAMES.Length;
		GameObject preBannerEffect = null;
		GameObject bannerEffect = null;

		//This is for any game that need to play an effect before placing the expanding the wild banners
		if (prePlaceWildBannerEffectPrefab != null)
		{
			preBannerEffect = prePlaceBannerEffectCacher.getInstance();
			preBannerEffect.SetActive(true);
			preBannerEffect.transform.parent = wildBannerParents[reelID].transform;
			preBannerEffect.transform.localPosition = PRE_PLACE_WILD_BANNER_EFFECT_OFFSET;
			preBannerEffect.transform.localScale = Vector3.one;
			if (wildBannerScalesOnReels != null && wildBannerScalesOnReels.Length > reelID)
			{
				preBannerEffect.transform.localScale = wildBannerScalesOnReels[reelID];
			}

			yield return new TIWaitForSeconds(prePlaceAnimationLength);

			//We've played both sounds we still have more reveals, reset index
			if(revealIndex > 1)
			{
				revealIndex = 0;
			}
			Audio.play(Audio.soundMap(VERTICAL_WILD_PRE_PLACE_KEYS[gameType, revealIndex]), 1.0f, 0.0f, VERTICAL_WILD_PRE_PLACE_SOUND_DELAY);

			//make sure we play the next sound on the next pre place
			++revealIndex;
		}
		
		if (wildBannerEffectPrefab != null) 
		{
			bannerEffect = bannerEffectCacher.getInstance();
			bannerEffect.SetActive (true);
			bannerEffect.transform.parent = wildBannerParents[reelID].transform;
			bannerEffect.transform.localPosition = WILD_BANNER_EFFECT_OFFSET;
			bannerEffect.transform.localScale = Vector3.one;
			if (wildBannerScalesOnReels != null && wildBannerScalesOnReels.Length > reelID)
			{
				bannerEffect.transform.localScale = wildBannerScalesOnReels[reelID];
			}

			bannerEffect.GetComponent<Animator> ().Play (WILD_BANNER_ANIMATION_NAMES [bannerIndex]);
			bannerEffect.GetComponent<Animator> ().speed = WILD_BANNER_ANIMATION_SPEED;
		}
		
		if (!shouldOnlyPlayFirstBannerSound || (bannerIndex == 0))
		{
			Audio.play(Audio.soundMap(VERTICAL_WILD_INIT_KEYS[gameType]), 1.0f, 0.0f, VERTICAL_WILD_INIT_SOUND_DELAY);
		}
		
		yield return new TIWaitForSeconds(WAIT_BEFORE_CREATING_SYMBOL_BANNERS[bannerIndex]); // 1st wait

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
		{
			if (symbol.name != featureMutation.symbol)
			{
				if(featureMutation.symbol != "")
				{
					symbol.mutateTo(featureMutation.symbol);
				}
			}
		}

		// Determine if there is a specific numWildBanners reveal sound or just generic
		string revealSoundKey = string.Format("{0}{1}", VERTICAL_WILD_REVEAL_KEYS[gameType], numWildBanners + 1);
		bool specificKeyExists = Audio.canSoundBeMapped(revealSoundKey);

		if (specificKeyExists)
		{
			Audio.play(Audio.soundMap(revealSoundKey), 1.0f, 0.0f, VERTICAL_WILD_REVEAL_SOUND_DELAY);
		}
		else
		{
			Audio.play(Audio.soundMap(VERTICAL_WILD_REVEAL_KEYS[gameType]), 1.0f, 0.0f, VERTICAL_WILD_REVEAL_SOUND_DELAY);
		}

		GameObject bannerSymbol = bannerPrefab.getInstance();
		bannerSymbol.SetActive(true);
		// This forces the default animation to play from the beginning instead of playing a frame where it was left off on the last spin
		bannerSymbol.GetComponent<Animator>().Update(0.0f);
		bannerSymbol.transform.parent = wildBannerParents[reelID].transform;
		bannerSymbol.transform.localPosition = WILD_BANNER_SYMBOL_OFFSET;
		bannerSymbol.transform.localScale = Vector3.one;
		if (wildBannerScalesOnReels != null && wildBannerScalesOnReels.Length > reelID)
		{
			bannerSymbol.transform.localScale = wildBannerScalesOnReels[reelID];
		}

		instantiatedBanners.Add(bannerSymbol, bannerPrefab);
		wildBanners[reelID] = bannerSymbol;

		yield return new TIWaitForSeconds(WAIT_BEFORE_DESTROYING_EFFECTS[bannerIndex]); // 2nd wait

		if (wildBannerEffectPrefab != null) 
		{
			bannerEffect.transform.localScale = Vector3.one;
			bannerEffectCacher.releaseInstance(bannerEffect);
		}

		if (prePlaceBannerEffectCacher != null && preBannerEffect != null)
		{
			prePlaceBannerEffectCacher.releaseInstance(preBannerEffect);
		}
	}

	// destroy all banners at start of next spin
	private void cleanUp()
	{
		foreach (KeyValuePair<GameObject, GameObjectCacher> cachedBanner in instantiatedBanners)
		{
			cachedBanner.Key.transform.localScale = Vector3.one;
			cachedBanner.Value.releaseInstance(cachedBanner.Key);
		}

		instantiatedBanners.Clear();
		System.Array.Clear(wildBanners, 0, wildBanners.Length);
	}
	
	// executeOnPreSpin() section
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		cleanUp();
		yield break;
	}

	public override bool needsToExecuteOnPaylineDisplay()
	{
		if (!string.IsNullOrEmpty(WILD_BANNER_PAYLINE_ANIMATION_NAME))
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		for (int i = reelGame.spotlightReelStartIndex; i < lineWin.symbolMatchCount + reelGame.spotlightReelStartIndex; i++)
		{
			if (wildBanners[i] != null)
			{
				StartCoroutine(CommonAnimation.playAnimAndWait(wildBanners[i].GetComponent<Animator>(), WILD_BANNER_PAYLINE_ANIMATION_NAME));
			}
		}
		yield break;
	}

	// executeAfterPaylinesCallback() section
	public override bool needsToExecuteAfterPaylines()
	{
		return shouldDestroyBannersOnEndPaylines; // freespins games will need this
	}
	
	public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		cleanUp();
		yield break;
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (shouldMutateSymbolsBehindBanners && wildBanners[symbol.reel.reelID - 1] != null)
		{
			return true;
		}
		return false;
	}
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		string debugName = symbol.debugName;
		string debug = "Mutated " + debugName;

		if (symbol.name != featureMutation.symbol && featureMutation.symbol != "")
		{
			symbol.mutateTo(featureMutation.symbol);
		}
		symbol.debugName = debugName;
		symbol.debug = debug;
	}

	[System.Serializable]
	private class FadeObject
	{
		public GameObject gameObject = null;
		public float startAmount = 0.0f;
		public float finishAmount = 1.0f;
	}
}
