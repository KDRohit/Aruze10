using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Basic module you can attach to a sliding freespin game where each value time you get a respin
The multipler goes up by one until it reachs some max.

Original Author: Leo Schnee
*/
public class MultiplierFreespinSlidingGameModule : BasicSlidingGameModule 
{

	[SerializeField] private UILabel multiplerLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent multiplerLabelWrapperComponent;

	public LabelWrapper multiplerLabelWrapper
	{
		get
		{
			if (_multiplerLabelWrapper == null)
			{
				if (multiplerLabelWrapperComponent != null)
				{
					_multiplerLabelWrapper = multiplerLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplerLabelWrapper = new LabelWrapper(multiplerLabel);
				}
			}
			return _multiplerLabelWrapper;
		}
	}
	private LabelWrapper _multiplerLabelWrapper = null;
	
	[SerializeField] private Animator multiplerAnimator;
	[SerializeField] private int[] multiplierOrder = new int[]{1,2,3};

	private int currentMultiplerIndex = 0;
	[SerializeField] private string MULTIPLIER_ANIMATOR_NAME = "";
	[SerializeField] protected float END_GAME_DELAY = 0.5f; // Time to wait after a spin if we're in freespins or autospinning
	private const string ALL_WINS_MULTIPLIER_LOC_STRING = "all_wins_{0}x";
	private const string MULTIPLIER_ADVANCE_SOUND_KEY = "sliding_freespins_multiplier_flourish";


	public override void Awake()
	{
		base.Awake();
		if (!(reelGame is FreeSpinGame))
		{
			Debug.LogError("MultiplierFreespinSlidingGameModule only works for freespin games. Destroying.");
			Destroy(this);
		}
	}

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		((FreeSpinGame)reelGame).endlessMode = true;
		BonusSpinPanel.instance.spinCountLabel.text = CommonText.formatNumber(reelGame.numberOfFreespinsRemaining);
		if (multiplerLabelWrapper != null)
		{
			multiplerLabelWrapper.text = Localize.text(ALL_WINS_MULTIPLIER_LOC_STRING, CommonText.formatNumber(multiplierOrder[currentMultiplerIndex]));
		}
		base.executeOnSlotGameStartedNoCoroutine(reelSetDataJson);		
	}
	/// Calls the IEnumerator that will call the game specific sliding functions.
	public override bool needsToExecuteOnReelsSlidingCallback()
	{
		return true;
	}

	/// Handle sliding of the reels
	public override IEnumerator executeOnReelsSlidingCallback()
	{
		yield return StartCoroutine(base.executeOnReelsSlidingCallback());

		Audio.play(Audio.soundMap(MULTIPLIER_ADVANCE_SOUND_KEY));
		if (multiplerLabelWrapper != null)
		{
			multiplerLabelWrapper.text = Localize.text(ALL_WINS_MULTIPLIER_LOC_STRING, CommonText.formatNumber(multiplierOrder[currentMultiplerIndex]));
		}
		if (multiplerAnimator != null)
		{
			multiplerAnimator.Play(MULTIPLIER_ANIMATOR_NAME);
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());
		int subOutcomeCount = reelGame.outcome.getSubOutcomesReadOnly().Count;
		if (subOutcomeCount == 0)
		{
			reelGame.numberOfFreespinsRemaining--;
			BonusSpinPanel.instance.spinCountLabel.text = CommonText.formatNumber(reelGame.numberOfFreespinsRemaining);
			currentMultiplerIndex = 0;
			if (reelGame.numberOfFreespinsRemaining == 0)
			{				
				yield return StartCoroutine(playCustomGameEndedSound());
				yield return new TIWaitForSeconds(END_GAME_DELAY);
			}
		}
		else
		{
			if (currentMultiplerIndex < multiplierOrder.Length - 1)
			{
				currentMultiplerIndex++;
			}
		}
	}

	protected virtual IEnumerator playCustomGameEndedSound()
	{
		// It will play freespin_summary_vo when the bonus Summary comes up in the normal flow.
		yield break;
	}
}

