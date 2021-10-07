using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StickyRespinFreeSpins : FreeSpinGame
{
	[SerializeField] private string RESPIN_BANNER_ANIM_NAME = "anim";
	[SerializeField] private UILabel freespinText = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent freespinTextWrapperComponent = null;

	public LabelWrapper freespinTextWrapper
	{
		get
		{
			if (_freespinTextWrapper == null)
			{
				if (freespinTextWrapperComponent != null)
				{
					_freespinTextWrapper = freespinTextWrapperComponent.labelWrapper;
				}
				else
				{
					_freespinTextWrapper = new LabelWrapper(freespinText);
				}
			}
			return _freespinTextWrapper;
		}
	}
	private LabelWrapper _freespinTextWrapper = null;
	
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

	private const string DEFAULT_BG_MUSIC = "freespin";
	[SerializeField] private string SMALL_RESPIN_SYMBOL_LOCK = "respin_symbol_lock_1x1";
	private const string BIG_RESPIN_SYMBOL_LOCK = "respin_symbol_lock";
	[SerializeField] private string RESPIN_BG_MUSIC = "respin_music";
	[SerializeField] private string RESPIN_START_SOUND = "respin_start_animation";
	private const string RESPIN_INIT_VO = "respin_init_vo";
	[SerializeField] private string RESPIN_INIT_VO_PREFIX = "";
	[SerializeField] private string RESPIN_INIT_VO_POSTFIX = "";
	[SerializeField] private List<string> respinVoSymbols;     // used to map symbol names to index value when building audio key strings

	[SerializeField] private float TIME_PLAY_SHEEN = 0.75f;
	[SerializeField] private float OUTCOME_ANIMATION_DELAY = 1.5f;
	[SerializeField] private float RESPIN_FEATURE_TEXT_DELAY = 1.0f;

	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());

		if (!Audio.isPlaying (Audio.soundMap(DEFAULT_BG_MUSIC)))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(DEFAULT_BG_MUSIC));
		}
	}

	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickySymbolName, int row)
	{
		hadStickySymbol = true;
		// Add a stuck symbol
		SlotSymbol newSymbol = createStickySymbol(stickySymbolName, symbol.index, symbol.reel);
		newSymbol.animator.playOutcome (symbol);
		if (sheenPrefab != null)
		{
			StartCoroutine(playSheen(newSymbol.animator.gameObject));
		}
		yield return null;
	}

	/// Allows any sort of cleanup that may need to happen on the symbol animator
    protected override void preReleaseStickySymbolAnimator(SymbolAnimator animator)
    {
    	CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);
    } 

	private IEnumerator playSheen(GameObject parent)
	{
		if (Audio.canSoundBeMapped (SMALL_RESPIN_SYMBOL_LOCK))
		{
			Audio.play (Audio.soundMap(SMALL_RESPIN_SYMBOL_LOCK));
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

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	private IEnumerator reelsStoppedCoroutine()
	{
		if (triggeredFeature())
		{
			string voStr = RESPIN_INIT_VO;
			if (respinVoSymbols != null && respinVoSymbols.Count > 0)
			{
				voStr = symbolToRespinIntroVO();
			}
			Audio.playSoundMapOrSoundKeyWithDelay(voStr, 2.5f);

			engine.getReelArray()[featureReels[0]].visibleSymbols[0].animator.addRenderQueue(100);

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

		if (freespinTextWrapper != null)
		{
			freespinTextWrapper.text = message;
		}

		if (hadStickySymbol)
		{
			StartCoroutine(playLargeBonusAnimator());
			yield return new TIWaitForSeconds(OUTCOME_ANIMATION_DELAY);
			hadStickySymbol = false;
		}
		yield return StartCoroutine(base.startNextReevaluationSpin());
	}

	protected override IEnumerator handleReevaluationReelStop()
	{
		yield return StartCoroutine(base.handleReevaluationReelStop());

		if (!hasReevaluationSpinsRemaining)
		{
			/// reset the message at the top of the slot
			if (freespinTextWrapper != null)
			{
				freespinTextWrapper.text = Localize.text("free_spins");
			}
		}
	}

	private bool triggeredFeature()
	{
		SlotReel[] reelArray = engine.getReelArray();

		string nameToMatch = reelArray[featureReels[0]].visibleSymbols[0].shortName;
		for (int i = 0; i < featureReels.Length; i++)
		{
			SlotReel reel = reelArray[featureReels[i]];
			foreach (SlotSymbol slotSymbol in reel.visibleSymbols)
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
		BonusSpinPanel.instance.slideInPaylineMessageBox();

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
			Audio.play(Audio.soundMap(BIG_RESPIN_SYMBOL_LOCK));

			Audio.switchMusicKeyImmediate(Audio.soundMap(RESPIN_BG_MUSIC));
			if (RESPIN_FEATURE_TEXT_DELAY > 0.0f)
			{
				yield return new TIWaitForSeconds(RESPIN_FEATURE_TEXT_DELAY);
			}
			Audio.playSoundMapOrSoundKey(RESPIN_START_SOUND);

			animator.Play(RESPIN_BANNER_ANIM_NAME);
			while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(RESPIN_BANNER_ANIM_NAME))
			{
				yield return null;
			}
			while (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(RESPIN_BANNER_ANIM_NAME))
			{
				yield return null;
			}
		}
	}

	private IEnumerator playLargeBonusAnimator()
	{
		SlotSymbol[] bonusSymbols = engine.getVisibleSymbolsAt (featureReels [0]);
		bonusSymbols [0].animateOutcome ();
		yield return new TIWaitForSeconds(OUTCOME_ANIMATION_DELAY);
	}
}

