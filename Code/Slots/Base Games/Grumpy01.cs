// Based on code from Bev02

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grumpy01 : SlotBaseGame
{
	[SerializeField] private string RESPIN_BANNER_ANIM_NAME = "anim";
	[SerializeField] private UILabel featureText = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent featureTextWrapperComponent = null;

	public LabelWrapper featureTextWrapper
	{
		get
		{
			if (_featureTextWrapper == null)
			{
				if (featureTextWrapperComponent != null)
				{
					_featureTextWrapper = featureTextWrapperComponent.labelWrapper;
				}
				else
				{
					_featureTextWrapper = new LabelWrapper(featureText);
				}
			}
			return _featureTextWrapper;
		}
	}
	private LabelWrapper _featureTextWrapper = null;
	
	[SerializeField] private LabelWrapperComponent featureWrapperText = null;
	[SerializeField] bool isFeatureTextNumberOnly = false; // tells if the feature text is the number, or the number plus something like "additional respins"
	[SerializeField] private Animator featureAnimator = null;

	[SerializeField] private GameObject sheenPrefab;
	private List<GameObject> availableSheens = new List<GameObject>();
	private int[] featureReels = new int[] {2, 3};
	private bool hadStickySymbol = false;

	[SerializeField] private string SMALL_RESPIN_SYMBOL_LOCK = "respin_symbol_lock_1x1";
	[SerializeField] private int BIG_RESPIN_LOCK_AUDIO_CHECK_REELID = -1;
	private const string BIG_RESPIN_SYMBOL_LOCK = "respin_symbol_lock";
	private const string RESPIN_BG_MUSIC = "respin_music";
	[SerializeField] private string RESPIN_START_SOUND = "respin_start_animation";
	private const string RESPIN_INIT_VO = "respin_init_vo";
	[SerializeField] private string RESPIN_INIT_VO_PREFIX = "";
	[SerializeField] private string RESPIN_INIT_VO_POSTFIX = "";
	[SerializeField] private List<string> respinVoSymbols;     // used to map symbol names to index value when building audio key strings

	[SerializeField] private float TIME_PLAY_SHEEN = 0.75f;
	[SerializeField] private float OUTCOME_ANIMATION_DELAY = 1.5f;
	[SerializeField] private float RESPIN_FEATURE_TEXT_DELAY = 1.0f;

	private bool hadStickyOnLastReevalSpin;
	private bool[,] stickySymbolMap = new bool[6,6];
	private bool featureAudioIsTriggered;
	private bool hasPlayedSmallSymbolLockSoundOnce = false; // don't double up symbol lock sounds, only play a single time
	private bool hasPlayedSmallSymbolAnimateSoundOnce = false; // don't double up symbol animate sounds, only play a single time
	[SerializeField] private bool PLAY_SMALL_SYMBOL_LOCK_ON_LANDING = false;
	[SerializeField] private bool PLAY_BIG_SYMBOL_LOCK_ON_LANDING = false;
	[SerializeField] private float BIG_SYMBOL_LOCK_AUDIO_DELAY = 0.0f;
	[SerializeField] private string BIG_SYMBOL_ANIMATE_AUDIO = "respin_symbol_animate";
	[SerializeField] private float BIG_SYMBOL_ANIMATE_AUDIO_DELAY = 0.0f;
	[SerializeField] private string SMALL_SYMBOL_ANIMATE_AUDIO = "wild_symbol_animate";
	[SerializeField] private float SMALL_SYMBOL_ANIMATE_AUDIO_DELAY = 0.0f;

	[SerializeField] private int SYMBOL_STICKY_LAYER = Layers.ID_SLOT_REELS_OVERLAY; // when a large symbol sticks on the reels, move to this layer.
	private string backgroundMusicRestoreKey = "";
	private GameObject flattenedBigFeatureSymbol;

	protected override void slotStartedEventCallback(JSON data)
	{
		base.slotStartedEventCallback(data);

		if (engine != null)
		{
			// link the reels with mega symbols on them for this first spin before the linking is setup from the data
			engine.linkReelsInLinkedReelsOverride(engine.getSlotReelAt(featureReels[0]), engine.getSlotReelAt(featureReels[1]));
		}
	}

	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickySymbolName, int row)
	{
		hadStickySymbol = true;
		// Add a stuck symbol
		SlotSymbol newSymbol = createStickySymbol(stickySymbolName, symbol.index, symbol.reel);
		newSymbol.animator.playOutcome (symbol);
		StartCoroutine(playSheen(newSymbol.animator.gameObject));

		if (PLAY_SMALL_SYMBOL_LOCK_ON_LANDING)
		{
			stickySymbolMap[symbol.reel.reelID - 1, row] = true;
		}

		if (reevaluationSpinsRemaining == 0)
		{
			// this is so we know if we need to do a final animation sequence in the handleStickySymbols override
			hadStickyOnLastReevalSpin = true;
		}

		yield return null;
	}

	/// Allows any sort of cleanup that may need to happen on the symbol animator
	protected override void preReleaseStickySymbolAnimator(SymbolAnimator animator)
	{
		CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);

		// Also reset the big symbol that may have been stickied by the feature.
		// It's a little inefficient to do this here because it will do this for
		// every sticky symbol, but this is the only place that gets called to
		// release sticky symbols and this seems reasonable instead of modifying
		// ReelGame to add another hook.
		SlotSymbol bigFeatureSymbol = engine.getVisibleSymbolsAt(featureReels[0])[0];
		if (bigFeatureSymbol != null)
		{
			CommonGameObject.setLayerRecursively(bigFeatureSymbol.gameObject, Layers.ID_SLOT_REELS);
		}

		if (flattenedBigFeatureSymbol != null)
		{
			CommonGameObject.setLayerRecursively(flattenedBigFeatureSymbol, Layers.ID_SLOT_REELS);
		}
	}

	private IEnumerator playSheen(GameObject parent)
	{		
		if (!PLAY_SMALL_SYMBOL_LOCK_ON_LANDING && Audio.canSoundBeMapped(SMALL_RESPIN_SYMBOL_LOCK))
		{
			Audio.play(Audio.soundMap(SMALL_RESPIN_SYMBOL_LOCK));
		}

		// play associated audio while symbol animates
		if (!hasPlayedSmallSymbolAnimateSoundOnce)
		{
			Audio.playSoundMapOrSoundKeyWithDelay(SMALL_SYMBOL_ANIMATE_AUDIO, SMALL_SYMBOL_ANIMATE_AUDIO_DELAY);
			hasPlayedSmallSymbolAnimateSoundOnce = true;
		}

		GameObject sheen = null;
		if (availableSheens.Count > 0)
		{
			// Remove from the back because it's cheaper.
			sheen = availableSheens[availableSheens.Count - 1];
			availableSheens.RemoveAt(availableSheens.Count - 1);
		}
		else
		{
			sheen = CommonGameObject.instantiate(sheenPrefab) as GameObject;
		}
		if (sheen != null)
		{
			Animator[] animators = sheen.GetComponentsInChildren<Animator>(true);
			foreach (Animator animator in animators)
			{
				if (animator != null)
				{
					animator.speed = 1.0f;	// Set the speed of the animator to 1, because deactivating symbols stops it.
				}
			}

			CommonGameObject.setLayerRecursively(sheen, sheenPrefab.layer);
			sheen.gameObject.SetActive(true);
			sheen.transform.parent = parent.transform;
			sheen.transform.localPosition = Vector3.zero;
			yield return new TIWaitForSeconds(TIME_PLAY_SHEEN);
			sheen.gameObject.SetActive(false);
			availableSheens.Add(sheen);
		}
	}

	private IEnumerator playLargeBonusAnimator()
	{
		SlotSymbol[] bonusSymbols = engine.getVisibleSymbolsAt(featureReels[0]);
		bonusSymbols[0].animateOutcome();
		yield return new TIWaitForSeconds(OUTCOME_ANIMATION_DELAY);
	}

	protected override IEnumerator startNextReevaluationSpin()
	{
		int spinsRemaining = reevaluationSpinsRemaining - 1;
		string message = "";
		if (spinsRemaining == 0)
		{
			message = Localize.text("last_spin");
		}
		else if (spinsRemaining == 1)
		{
			message = Localize.text("1_spin_remaining");
		}
		else
		{
			message = Localize.text("{0}_spins_remaining", reevaluationSpinsRemaining - 1);
		}

		if (BonusSpinPanel.instance != null && FreeSpinGame.instance != null)
		{
			BonusSpinPanel.instance.messageLabel.text = message;
		}
		else
		{
			SpinPanel.instance.setMessageText(message);
		}
		if (hadStickySymbol)
		{
			StartCoroutine(playLargeBonusAnimator());
			yield return new TIWaitForSeconds(OUTCOME_ANIMATION_DELAY);
			hadStickySymbol = false;
		}

		hasPlayedSmallSymbolLockSoundOnce = false; // reset small lock flag
		hasPlayedSmallSymbolAnimateSoundOnce = false; // reset small animate flag
		yield return StartCoroutine(base.startNextReevaluationSpin());
	}

	public override IEnumerator handleStickySymbols(Dictionary<int, Dictionary<int, string>> stickySymbols)
	{
		yield return StartCoroutine(base.handleStickySymbols(stickySymbols));

		if (reevaluationSpinsRemaining == 0)
		{
			// now we have a chance to do final animations before paylines start
			if (hadStickyOnLastReevalSpin)
			{
				yield return StartCoroutine(playLargeBonusAnimator());
				hadStickyOnLastReevalSpin = false;
			}

			if (PLAY_SMALL_SYMBOL_LOCK_ON_LANDING)
			{
				stickySymbolMap = new bool[6,6];
			}
		}
	}	

	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		if (triggeredFeature())
		{
			if (!featureAudioIsTriggered && stoppedReel.reelID == BIG_RESPIN_LOCK_AUDIO_CHECK_REELID)
			{
				playBigSymbolLockAudio();
			}
			
			if (PLAY_SMALL_SYMBOL_LOCK_ON_LANDING)
			{
				if (stoppedReel.reelID-1 != featureReels[0] && stoppedReel.reelID-1 != featureReels[1])
				{
					string nameToMatch = engine.getVisibleSymbolsAt(featureReels[0])[0].shortServerName;
					int i = stoppedReel.visibleSymbols.Length-1;
					foreach (SlotSymbol slotSymbol in stoppedReel.visibleSymbols)
					{
						if ((slotSymbol.shortServerName == nameToMatch || slotSymbol.shortServerName == "WD") && !stickySymbolMap[stoppedReel.reelID-1,i])
						{
							stickySymbolMap[stoppedReel.reelID-1,i] = true;
							if (!hasPlayedSmallSymbolLockSoundOnce)
							{
								Audio.play(Audio.soundMap(SMALL_RESPIN_SYMBOL_LOCK));
								hasPlayedSmallSymbolLockSoundOnce = true;
							}

						}
						i--;
					}	
				}
			}		
		}

		yield return  StartCoroutine(base.handleSpecificReelStop(stoppedReel));
	}	
			
	protected override IEnumerator handleReevaluationReelStop()
	{
		yield return StartCoroutine(base.handleReevaluationReelStop());
		if (!hasReevaluationSpinsRemaining && !string.IsNullOrEmpty(backgroundMusicRestoreKey) && !Audio.isPlaying(Audio.soundMap(backgroundMusicRestoreKey)))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(backgroundMusicRestoreKey));
			featureAudioIsTriggered = false;
		}
	}

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());

	}
	
	private IEnumerator reelsStoppedCoroutine()
	{
		flattenedBigFeatureSymbol = null;
		if (triggeredFeature())
		{	
			string voStr = RESPIN_INIT_VO;
			if (respinVoSymbols != null && respinVoSymbols.Count > 0)
			{
				voStr = symbolToRespinIntroVO();
			}
			Audio.playSoundMapOrSoundKeyWithDelay(voStr, 2.5f);

			SlotSymbol bigFeatureSymbol = engine.getVisibleSymbolsAt(featureReels[0])[0];

			if (bigFeatureSymbol != null)
			{
				// We need the flattened symbols gameObject because if it gets animated in a payline or somewhere,
				// it will get swapped out for an unflattened version from the symbol cache and we need to be able
				// to set this back to the reel layer when it spins off.
				flattenedBigFeatureSymbol = bigFeatureSymbol.gameObject;
				CommonGameObject.setLayerRecursively(bigFeatureSymbol.gameObject, SYMBOL_STICKY_LAYER);

				if (PLAY_BIG_SYMBOL_LOCK_ON_LANDING)
				{
					playBigSymbolLockAudio();

					Audio.playSoundMapOrSoundKeyWithDelay(BIG_SYMBOL_ANIMATE_AUDIO, BIG_SYMBOL_ANIMATE_AUDIO_DELAY);
					yield return StartCoroutine(bigFeatureSymbol.playAndWaitForAnimateOutcome());
				}
			}
			else
			{
				Debug.LogError("No big symbol found for feature - skipping animation!");
			}

			yield return StartCoroutine(playFeatureTextAnimation(featureTextWrapper, featureAnimator));
		}
		base.reelsStoppedCallback();

		yield break;
	}

	// builds audio key based on feature symbol names position in respinVoSymbols, gen26 uses {M1,M2,M3,M4}
	private string symbolToRespinIntroVO()
	{
		string voStr = RESPIN_INIT_VO_PREFIX;

		string nameToMatch = engine.getVisibleSymbolsAt(featureReels[0])[0].shortName;

		int i = 0;
		foreach (string symbolName in respinVoSymbols)
		{
			i++;
			if (nameToMatch.Contains(symbolName))
			{
				voStr += i;
				break;
			}			
		}

		voStr += RESPIN_INIT_VO_POSTFIX;

		return voStr;
	}

	private bool triggeredFeature()
	{
		string nameToMatch = engine.getVisibleSymbolsAt(featureReels[0])[0].shortName;
		for (int i = 0; i < featureReels.Length; i++)
		{
			foreach (SlotSymbol slotSymbol in engine.getVisibleSymbolsAt(featureReels[i]))
			{
				if (slotSymbol.shortName != nameToMatch)
				{
					// a symbol in this reel doesn't match the feature trigger, so NOT triggered
					return false;
				}
			}
		}
		return true;
	}

	/// Play the feature text when a feature is acquired
	public IEnumerator playFeatureTextAnimation(LabelWrapper featureText, Animator animator)
	{
		SpinPanel.instance.slideInPaylineMessageBox();

		if (featureText != null)
		{
			if (isFeatureTextNumberOnly)
			{
				featureText.text = reevaluationSpinsRemaining.ToString();
			}
			else
			{
				featureText.text = Localize.text("{0}_locking_respins_awarded", reevaluationSpinsRemaining);
			}
		}

		if (featureWrapperText != null)
		{
			if (isFeatureTextNumberOnly)
			{
				featureWrapperText.text = reevaluationSpinsRemaining.ToString();
			}
			else
			{
				featureWrapperText.text = Localize.text("{0}_locking_respins_awarded", reevaluationSpinsRemaining);
			}
		}

		if (animator != null)
		{
			if (BIG_RESPIN_LOCK_AUDIO_CHECK_REELID == -1)
			{
				playBigSymbolLockAudio();
			}

			Audio.switchMusicKeyImmediate(Audio.soundMap(RESPIN_BG_MUSIC));
			if (RESPIN_FEATURE_TEXT_DELAY > 0.0f)
			{
				yield return new TIWaitForSeconds(RESPIN_FEATURE_TEXT_DELAY);
			}
			Audio.playSoundMapOrSoundKey(RESPIN_START_SOUND);

			yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, RESPIN_BANNER_ANIM_NAME));
		}
	}

	private void playBigSymbolLockAudio()
	{
		// save the restore music key here because BIG_RESPIN_SYMBOL_LOCK will abort the music audio
		// and depending on settings this can happen at different times.		
		backgroundMusicRestoreKey =  Audio.defaultMusicKey;
		Audio.playSoundMapOrSoundKeyWithDelay(BIG_RESPIN_SYMBOL_LOCK, BIG_SYMBOL_LOCK_AUDIO_DELAY);
		featureAudioIsTriggered = true;
	}
}

