using UnityEngine;
using System.Collections;

/**
Module for games with feature reels (like pb01 and kendra01) where getting a TR on the feature reel triggers a TW replacement feature
*/
public class FeatureReelTWMutateModule : SlotModule 
{
	[SerializeField] protected MultiplierPayBoxDisplayModule multiplierPayBoxModule = null;	// This module can tell what feature was triggered
	[SerializeField] private GameObject twAnimObjPrefab = null;								// Prefab to make the TW effects from for the TW feature
	[SerializeField] private bool isPlayingAnimSoundOnReveal = true;						// Flag to control if teh anim sound plays at reveal as well as for each symbol changed
	[SerializeField] private bool isPlayingFeatureAnimationOnReveal = false;                // Controls if the feature animation is triggered at the same time as the reveal sounds go off	

	[SerializeField] private float TW_EFFECT_ANIM_LENGTH;
	[SerializeField] private float TW_ANIM_STAGGER_TIME;
	[SerializeField] private float WAIT_AFTER_TW_REVEAL_BEFORE_START = 0.5f;				// Mild wait after the TW reveal and sounds play before transforming the other symbols

	private GameObjectCacher freeTwAnimCache = null;						// Caches the TW effects so they can be reused
	private int numTwAnimsGoing = 0;										// Tracks how many TW symbol animations are going right now

	protected const string TW_ANIM_SOUND_KEY = "symbol_animation_TW";
	protected const string TW_SYMBOL_REVEAL_SOUND_KEY = "TW_symbol_reveal";
	protected const string TW_SYMBOL_VO_SOUND_KEY = "TW_symbol_vo";
	protected const string TW_SYMBOL_RESOLVE_SOUND_KEY = "TW_symbol_resolve";

	public override void Awake()
	{
		freeTwAnimCache = new GameObjectCacher(this.gameObject, twAnimObjPrefab);

		base.Awake();
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Go through the mutations from the spin and see if there is one for this type of mutation.
		if (multiplierPayBoxModule.getCurrentFeatureEnum() == MultiplierPayBoxDisplayModule.MultiplierPayBoxFeatureEnum.TR)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		Audio.play(Audio.soundMap(TW_SYMBOL_REVEAL_SOUND_KEY));
		Audio.play(Audio.soundMap(TW_SYMBOL_VO_SOUND_KEY));

		if (isPlayingFeatureAnimationOnReveal)
		{
			StartCoroutine(multiplierPayBoxModule.playBoxDisplayAnimation());
		}

		if (isPlayingAnimSoundOnReveal)
		{
			playTWAnimSound();
		}

		yield return new TIWaitForSeconds(WAIT_AFTER_TW_REVEAL_BEFORE_START);

		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int i = 0; i < reelArray.Length; ++i)
		{
			SlotReel reel = reelArray[i];

			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (symbol.name.Contains("-TW"))
				{
					StartCoroutine(doTwSymbolAnimaiton(symbol));
					yield return new TIWaitForSeconds(TW_ANIM_STAGGER_TIME);
				}
			}
		}

		// wait for animations to finish before exiting this coroutine
		while (numTwAnimsGoing != 0)
		{
			yield return null;
		}

		Audio.play(Audio.soundMap(TW_SYMBOL_RESOLVE_SOUND_KEY));
	}

	/// Handle playing the TW reveal sound here, can override if you want to do a very custom sound
	protected virtual void playTWAnimSound()
	{
		Audio.play(Audio.soundMap(TW_ANIM_SOUND_KEY));
	}

	/// Perform a tween to move the TW anim over the symbol which will change to a wild
	private IEnumerator doTwSymbolAnimaiton(SlotSymbol symbol)
	{
		GameObject twAnimEffect = freeTwAnimCache.getInstance();

		if (twAnimEffect != null)
		{
			numTwAnimsGoing++;

			playTWAnimSound();

			twAnimEffect.transform.parent = symbol.gameObject.transform.parent;
			// ensure that x adjustments don't shift the tw population animaitons
			Vector3 targetLocalPos = symbol.gameObject.transform.localPosition - symbol.info.positioning;
			targetLocalPos.x = 0;
			// just going to spawn it on top of the symbol 
			twAnimEffect.transform.localPosition = targetLocalPos;
			//Scale the object so it doesn't get scaled when reparented
			twAnimEffect.transform.localScale = Vector3.one;

			twAnimEffect.SetActive(true);

			yield return new TIWaitForSeconds(TW_EFFECT_ANIM_LENGTH);

			symbol.mutateTo("TW-WD");
			symbol.showWild();

			// Reset animation back to beginning. Prevents flicker if the animation begins with GO deactivated or with scale 0.
			Animator animator = twAnimEffect.GetComponent<Animator>();
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
			animator.Play(currentAnimatorStateInfo.fullPathHash, -1, 0.0f);
			// Need to wait a frame to update animator before saving to cache
			yield return null;

			twAnimEffect.SetActive(false);

			freeTwAnimCache.releaseInstance(twAnimEffect);

			numTwAnimsGoing--;
		}
		else
		{
			// even if something happens and the effect doesn't load still swap the symbol
			symbol.mutateTo("TW-WD");
			symbol.showWild();
		}
	}
}
