using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
The base class for BBH01 (big buck hunter), a game that has a respin feature, and one pickem game.
The respin is triggered when the middle 2 reels are completely filled with a major
    4 4
3 3 4 4 3 3
3 3 4 4 3 3 
3 3 4 4 3 3
**/
public class Bbh01 : SlotBaseGame
{
	[SerializeField] private Animator featureAnimator = null;

	[SerializeField] private GameObject sheenPrefab;
	private List<GameObject> availableSheens = new List<GameObject>();
	private int[] featureReels = new int[] {2, 3};
	private string baseMusicKey = "";

	private const float TIME_PLAY_SHEEN = 0.75f;

	private const string SYMBOL_LOCK_SOUND = "LockSymbolBBH01";
	private const string SYMBOL_MAJOR_INIT_SOUND = "MajorSymbolInitBBH01";
	private const string PREWIN_BASE = "PrewinBaseBBH01";
	private const string RESPIN_MUSIC = "RespinBgBBH01";
	private const string RESPIN_BANNER_ANIM_NAME = "BBH01_Reel_Reel respins banner";	// Name for the animation of the banner coming in

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

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		if (hasReevaluationSpinsRemaining)
		{
			Audio.play(SYMBOL_MAJOR_INIT_SOUND);
			baseMusicKey =  Audio.defaultMusicKey;
			Audio.switchMusicKeyImmediate(RESPIN_MUSIC);
		}

		if (triggeredFeature())
		{
			Audio.play(PREWIN_BASE);
			engine.getVisibleSymbolsAt(featureReels[0])[0].animator.addRenderQueue(100);
			
			// finishing the spin will happen in the callback
			engine.getVisibleSymbolsAt(featureReels[0])[0].animateOutcome(onRespinSymbolAnimFinished);
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
}
