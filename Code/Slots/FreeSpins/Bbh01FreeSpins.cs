using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bbh01FreeSpins : FreeSpinGame
{

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
	
	[SerializeField] private UILabel freespinTextOutline = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent freespinTextOutlineWrapperComponent = null;

	public LabelWrapper freespinTextOutlineWrapper
	{
		get
		{
			if (_freespinTextOutlineWrapper == null)
			{
				if (freespinTextOutlineWrapperComponent != null)
				{
					_freespinTextOutlineWrapper = freespinTextOutlineWrapperComponent.labelWrapper;
				}
				else
				{
					_freespinTextOutlineWrapper = new LabelWrapper(freespinTextOutline);
				}
			}
			return _freespinTextOutlineWrapper;
		}
	}
	private LabelWrapper _freespinTextOutlineWrapper = null;
	
	[SerializeField] private Animator featureAnimator = null;

	[SerializeField] private GameObject sheenPrefab;
	private List<GameObject> availableSheens = new List<GameObject>();
	private int[] featureReels = new int[] {2, 3};
	private string baseMusicKey = "";

	private const float TIME_PLAY_SHEEN = 0.75f;

	private const string RESPIN_BANNER_ANIM_NAME = "BBH01_Reel_Reel respins banner";	// Name for the animation of the banner coming in

	private const string SYMBOL_LOCK_SOUND = "LockSymbolBBH01";
	private const string FREE_SPIN_SUMMARY_VO = "FreespinSummaryVOBBH01";
	private const string SYMBOL_MAJOR_INIT_SOUND = "MajorSymbolInitBBH01";
	private const string RESPIN_MUSIC = "RespinBgBBH01";

	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		changeFreeSpinTextMessage(Localize.text("free_spins"));
	}

	/// Set what the free spin text says
	private void changeFreeSpinTextMessage(string message)
	{
		freespinTextWrapper.text = message;
		freespinTextOutlineWrapper.text = message;
	}

	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickySymbolName, int row)
	{
		Audio.play(SYMBOL_LOCK_SOUND);
		if (symbol.serverName != stickySymbolName)
		{
			symbol.mutateTo(stickySymbolName);
		}
		// Add a stuck symbol
		SlotSymbol newSymbol = createStickySymbol(stickySymbolName, symbol.index, symbol.reel);
		StartCoroutine(playSheen(newSymbol.animator.gameObject));
		yield return null;
	} 

	/// Allows any sort of cleanup that may need to happen on the symbol animator
    protected override void preReleaseStickySymbolAnimator(SymbolAnimator animator)
    {
    	CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);
    }

	private IEnumerator playSheen(GameObject parent)
	{
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
		if (hasReevaluationSpinsRemaining)
		{
			Audio.play(SYMBOL_MAJOR_INIT_SOUND);
			baseMusicKey =  Audio.defaultMusicKey;
			Audio.switchMusicKeyImmediate(RESPIN_MUSIC);
		}

		if (triggeredFeature())
		{
			SlotReel[] reelArray = engine.getReelArray();

			reelArray[featureReels[0]].visibleSymbols[0].animator.addRenderQueue(100);

			reelArray[featureReels[0]].visibleSymbols[0].animateOutcome(onRespinSymbolAnimFinished);
		}
		else
		{
			base.reelsStoppedCallback();
		}
	}

	/// Proceed with the rest of the respin feature now that the symbol has animated
	private void onRespinSymbolAnimFinished(SlotSymbol sender)
	{
		StartCoroutine(doRespinFeature());
	}

	/// Handle the start of the respin feature
	private IEnumerator doRespinFeature()
	{
		yield return StartCoroutine(playFeatureTextAnimation(featureAnimator));

		base.reelsStoppedCallback();
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

		if (freespinTextWrapper != null && freespinTextOutlineWrapper != null)
		{
			changeFreeSpinTextMessage(message);
		}

		yield return StartCoroutine(base.startNextReevaluationSpin());
	}

	protected override IEnumerator handleReevaluationReelStop()
	{
		if (!hasReevaluationSpinsRemaining)
		{
			Audio.switchMusicKeyImmediate(baseMusicKey, 0);
		}
		yield return StartCoroutine(base.handleReevaluationReelStop());
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
	public IEnumerator playFeatureTextAnimation(Animator animator)
	{
		if (animator != null)
		{
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

	protected override void gameEnded()
	{
		Audio.play(FREE_SPIN_SUMMARY_VO);
		base.gameEnded();
	}
}

