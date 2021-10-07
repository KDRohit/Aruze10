using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Hot01FreespinModule : LockingStickySymbolModule
{

	[SerializeField] protected GameObject stickyEffect;
	[SerializeField] protected GameObject[] freeSpinSparkleEffects;
	[SerializeField] private Animator freeSpinExplosion;
	[SerializeField] private GameObject freeSpinExplosionPrefab;
	[SerializeField] private int wildOverlayLayer =  Layers.ID_SLOT_OVERLAY; // Can override this if you get a request from art like the wild overlays are supposed to be under the frame
	[SerializeField] private string WILD_SYMBOL_LOCK_VO = "";
	[SerializeField] protected bool SHOULD_STAGGER_ADD_SPIN_EFFECT;

	private bool isWildVOPlayed = false;
	private bool isSpinPlusOneVOPlayed = false;
	private int freeSpinPlusOneSoundCount = 1;
	private bool isFirstStickySymbolPlayed = false;
	private GameObjectCacher stickyEffectCacher = null;
	private List<int> stickyWildsOnLastReelPositions = new List<int>();

	// "Constants"
	[SerializeField] protected float TIME_MOVE_SPARKLE = 1.0f;
	[SerializeField] private float STICKY_SYMBOL_REVEAL_ANIM_LENGTH = 1.033f;
	[SerializeField] private float EXPLOSION_DEACTIVATION_DELAY = 2.0f;
	private const string EXPLOSION_ANIMATION_NAME = "fsExplosion_anim";

	private const string WILD_SYMBOL_LOCK_SOUND_MAPPING = "freespin_wd_symbol_lock";
	private const string WILD_SYMBOL_LAND_SOUND_MAPPING = "freespin_wd_symbol_land";
	private const string FREESPIN_PLUS_ONE_TRAVEL_SOUND_MAPPING = "freespin_add_one_spin_travel";
	private const string FREESPIN_PLUS_ONE_ARRIVE_SOUND_MAPPING = "freespin_add_one_spin_arrive";
	private const string FREESPIN_PLUS_ONE_LAND_SOUND_MAPPING_PREFIX = "freespin_add_one_spin_land_";
	private const string FREESPIN_PLUS_ONE_VO_MAPPING = "freespin_add_one_spin_vo";

	public override void Awake()
	{
		base.Awake();
		stickySymbolsParent.layer = wildOverlayLayer;
		isStaggeringStickySymbols = false;
		stickyEffectCacher = new GameObjectCacher(this.gameObject, stickyEffect);
	}

	protected override IEnumerator grantFreeSpins(int numberOfFreespins, float grantDelay)
	{
		int spinsRemaining = reelGame.numberOfFreespinsRemaining; // just used in the last line for safety

		if (numberOfFreespins > 0)
		{
			SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(4);
			for (int position = 0; position < visibleSymbols.Length; position++)
			{
				if (visibleSymbols[position].isBonusSymbol && !stickyWildsOnLastReelPositions.Contains(position))
				{
					visibleSymbols[position].animateOutcome();
					moveSparkleToFreeSpinCount(freeSpinSparkleEffects[position]);
					if (SHOULD_STAGGER_ADD_SPIN_EFFECT)
					{
						yield return new TIWaitForSeconds(TIME_MOVE_SPARKLE);
						reelGame.numberOfFreespinsRemaining += 1; 
					}
				}
			}
		}

		if (!SHOULD_STAGGER_ADD_SPIN_EFFECT) 
		{
			yield return new TIWaitForSeconds (TIME_MOVE_SPARKLE);
		}

		reelGame.numberOfFreespinsRemaining = spinsRemaining + numberOfFreespins; // in the case where we added them 1 by 1 this is a safety check (since there was previously a bug with that)
	}

	protected virtual void animate(GameObject sparkleEffect)
	{
		Hashtable args = iTween.Hash("position", BonusSpinPanel.instance.spinCountLabel.transform.position,
		                             "time", TIME_MOVE_SPARKLE,
		                             "oncomplete", "onAnimationComplete",
		                             "oncompletetarget", gameObject,
		                             "oncompleteparams", sparkleEffect.gameObject);
		iTween.MoveTo(sparkleEffect, args);
	}

	private void onAnimationComplete(GameObject sparkleEffect)
	{
		sparkleEffect.transform.localPosition = Vector3.zero;
		sparkleEffect.SetActive(false);

		if (freeSpinExplosion != null)
		{
			freeSpinExplosion.transform.position = BonusSpinPanel.instance.spinCountLabel.transform.position;
			Audio.play(Audio.soundMap(FREESPIN_PLUS_ONE_ARRIVE_SOUND_MAPPING));
			freeSpinExplosion.Play(EXPLOSION_ANIMATION_NAME);
		}
		else if (freeSpinExplosionPrefab != null)
		{
			freeSpinExplosionPrefab.transform.position = BonusSpinPanel.instance.spinCountLabel.transform.position;
			freeSpinExplosionPrefab.SetActive(true);
			Audio.play(Audio.soundMap(FREESPIN_PLUS_ONE_ARRIVE_SOUND_MAPPING));
		}

		foreach (ParticleSystem ps in sparkleEffect.GetComponentsInChildren<ParticleSystem>(true))
		{
			if (ps != null)
			{
				ps.Clear();
			}
		}

		if (freeSpinExplosionPrefab != null)
		{
			StartCoroutine(deactivateSpinExplosion());
		}
	}

	private IEnumerator deactivateSpinExplosion()
	{
		yield return new TIWaitForSeconds(EXPLOSION_DEACTIVATION_DELAY);
		freeSpinExplosionPrefab.SetActive(false);
	}

	private void moveSparkleToFreeSpinCount(GameObject sparkleEffect)
	{
		if (!isSpinPlusOneVOPlayed)
		{
			Audio.play(Audio.soundMap(FREESPIN_PLUS_ONE_VO_MAPPING));
			isSpinPlusOneVOPlayed = true;
		}

		sparkleEffect.SetActive(true);
		Audio.play(Audio.soundMap(FREESPIN_PLUS_ONE_TRAVEL_SOUND_MAPPING));
		animate(sparkleEffect);
	}

	protected override IEnumerator changeSymbolToSticky(int reelID, int position, string name)
	{
		if (!isWildVOPlayed)
		{
			if (WILD_SYMBOL_LOCK_VO != "")
			{
				Audio.play(WILD_SYMBOL_LOCK_VO);
			}
			isWildVOPlayed = true;
		}

		// Make a new symbol.
		SlotSymbol newSymbol = new SlotSymbol(reelGame);
		SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelID);
		SlotSymbol symbolToSticky = visibleSymbols[position];
		int newSymbolIndex = symbolToSticky.index; // index will be different from position if on independent reel
		SlotReel reel = symbolToSticky.reel; 

		// Setup symbol with updated index
		newSymbol.setupSymbol(name + STICKY_SYMBOL_NAME_POSTFIX, newSymbolIndex, reel);

		// We need to set the local position here for independent reel games since engine.getSlotReelAt is returning
		// the actual reel object that is in that position, but the server is sending us a relative position. 
		newSymbol.transform.localPosition = reelGame.engine.getVisibleSymbolsAt(reelID)[position].transform.localPosition;
		newSymbol.gameObject.name = "sticky_" + name + " (" + reelID + ", " + position + ")";
		newSymbol.transform.parent = stickySymbolsParent.transform;
		CommonGameObject.setLayerRecursively(newSymbol.gameObject, wildOverlayLayer);
		newSymbol.animateOutcome();
		//Debug.LogWarning("new sticky symbol on reel: " + reelID + " and position: " + position);
		if (reelID == 4)
		{
			stickyWildsOnLastReelPositions.Add(position);
		}
		if (!isFirstStickySymbolPlayed)
		{
			// only play this sound once for the first locking coroutine, otherwise we multiply the sounds volume because we aren't staggering
			Audio.play(Audio.soundMap(WILD_SYMBOL_LOCK_SOUND_MAPPING));
			Audio.play(Audio.soundMap(WILD_SYMBOL_LAND_SOUND_MAPPING));
			isFirstStickySymbolPlayed = true;
		}

		currentStickySymbols.Add(newSymbol);
		// Add the effect to the sticky symbol.
		if (stickyEffect != null) 
		{
			GameObject stickyEffectObject = stickyEffectCacher.getInstance();
			stickyEffectObject.SetActive(true);
			stickyEffectObject.transform.localScale = stickyEffect.transform.localScale;
			stickyEffectObject.transform.parent = newSymbol.transform;
			stickyEffectObject.transform.localPosition = Vector3.zero;
			yield return new TIWaitForSeconds(STICKY_SYMBOL_REVEAL_ANIM_LENGTH);
			stickyEffectCacher.releaseInstance(stickyEffectObject);
		}
		else
		{
			yield return new TIWaitForSeconds (STICKY_SYMBOL_REVEAL_ANIM_LENGTH);
		}
	}

	public override IEnumerator executeOnPreSpin()
	{
		freeSpinPlusOneSoundCount = 1;
		isFirstStickySymbolPlayed = false;
		yield return StartCoroutine(base.executeOnPreSpin());
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return true;
	}

	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		// handle playing the free spin plus one land sounds
		foreach (string symbolName in stoppingReel.getFinalReelStopsSymbolNames())
		{
			if (SlotSymbol.isBonusSymbolFromName(symbolName) && !stickyWildsOnLastReelPositions.Contains(stoppingReel.position))
			{
				Audio.play(Audio.soundMap(FREESPIN_PLUS_ONE_LAND_SOUND_MAPPING_PREFIX + freeSpinPlusOneSoundCount));

				if (freeSpinPlusOneSoundCount <= 5)
				{
					freeSpinPlusOneSoundCount++;
				}
			}
		}
	}
}
