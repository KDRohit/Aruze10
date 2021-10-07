using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
The base class for Bev02, a game that has a respin feature, and one pickem game.
The respin is triggered when the middle 2 reels are completely filled with a major
    4 4
3 3 4 4 3 3
3 3 4 4 3 3 
3 3 4 4 3 3
**/
public class Bev02 : SlotBaseGame
{
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
	private string baseMusicKey = "";

	private const float TIME_PLAY_SHEEN = 0.75f;

	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickySymbolName, int row)
	{
		hadStickySymbol = true;
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
			Audio.play("LockSymbolBev02");
			baseMusicKey =  Audio.defaultMusicKey;
			Audio.switchMusicKeyImmediate("RespinBev02");
			//yield return new TIWaitForSeconds(TIME_PLAY_SHEEN);
			hadStickySymbol = false;
		}
		yield return StartCoroutine(base.startNextReevaluationSpin());
	}

	protected override IEnumerator handleReevaluationReelStop()
	{
		yield return StartCoroutine(base.handleReevaluationReelStop());
		Audio.switchMusicKeyImmediate(baseMusicKey);
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
			Audio.play("PrewinBaseBev02");
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
		featureText.text = Localize.text("{0}_locking_respins_awarded", reevaluationSpinsRemaining);

		if (animator != null)
		{
			animator.Play("respin_banner");
			while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("respin_banner"))
			{
				yield return null;
			}
			while (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("respin_banner"))
			{
				yield return null;
			}
		}
	}
}

