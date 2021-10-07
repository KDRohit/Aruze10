using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Hopefully this class can be shared for any future clones similar to Bev02, Grumpy01
public class StickyRespinBaseGame : SlotBaseGame
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
	
	[SerializeField] private Animator featureAnimator = null;

	[SerializeField] private GameObject sheenPrefab;
	private List<GameObject> availableSheens = new List<GameObject>();
	private int[] featureReels = new int[] {2, 3};
	private bool hadStickySymbol = false;

	private const string SMALL_RESPIN_SYMBOL_LOCK = "respin_symbol_lock_1x1";
	private const string BIG_RESPIN_SYMBOL_LOCK = "respin_symbol_lock";
	private const string RESPIN_BG_MUSIC = "respin_music";
	private const string RESPIN_START_SOUND = "respin_start_animation";
	private const string RESPIN_INIT_VO = "respin_init_vo";
	private const string BASEGAME_BG_MUSIC = "reelspin_base";

	[SerializeField] private const float TIME_PLAY_SHEEN = 0.75f;
	[SerializeField] private const float OUTCOME_ANIMATION_DELAY = 1.5f;
	[SerializeField] private const float RESPIN_FEATURE_TEXT_DELAY = 1.0f;

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
		else if (sheenPrefab != null)
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
		SlotSymbol[] bonusSymbols = engine.getVisibleSymbolsAt (featureReels [0]);
		bonusSymbols [0].animateOutcome ();
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
		yield return StartCoroutine(base.startNextReevaluationSpin());
	}
			
	protected override IEnumerator handleReevaluationReelStop()
	{
		yield return StartCoroutine(base.handleReevaluationReelStop());
		if (!hasReevaluationSpinsRemaining && !Audio.isPlaying(Audio.soundMap(BASEGAME_BG_MUSIC)))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BASEGAME_BG_MUSIC));
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
			Audio.play(Audio.soundMap(RESPIN_INIT_VO), 1f, 0f, 2.5f);
			engine.getVisibleSymbolsAt(featureReels[0])[0].animator.addRenderQueue(100);
			yield return StartCoroutine(playFeatureTextAnimation(featureTextWrapper, featureAnimator));
		}
		base.reelsStoppedCallback();

		yield break;
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
		//featureText.text = Localize.text("{0}_locking_respins_awarded", reevaluationSpinsRemaining);
		SpinPanel.instance.slideInPaylineMessageBox();

		if (animator != null)
		{
			Audio.play(Audio.soundMap(BIG_RESPIN_SYMBOL_LOCK));
			Audio.switchMusicKeyImmediate(Audio.soundMap(RESPIN_BG_MUSIC));
			yield return new TIWaitForSeconds(RESPIN_FEATURE_TEXT_DELAY);
			Audio.play(Audio.soundMap(RESPIN_START_SOUND));
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
}

