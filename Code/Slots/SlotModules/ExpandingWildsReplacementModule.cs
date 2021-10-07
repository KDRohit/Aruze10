using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpandingWildsReplacementModule : SlotModule
{
	public const string TRIGGERED_RESPINS_WITH_REPLACED_REELS = "triggered_respins_with_replaced_reels";
	public const string REPLACE_SYMBOL = "replace_symbol";
	private string replacementSymbolName;

	[SerializeField] AnimationListController.AnimationInformationList onReelsStoppedAnimationList = new AnimationListController.AnimationInformationList();
	[SerializeField] AnimationListController.AnimationInformationList onPreSpinAnimationList = new AnimationListController.AnimationInformationList();

	private List<Transform> onReelsStoppedAnimationListParents = new List<Transform>();

	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		bool isTriggerRespinsWithReplacedReels = false;

		List<SlotOutcome> reevalSpins = reelGame.reevaluationSpins;
		if (reevalSpins != null && reevalSpins.Count > 0)
		{
			isTriggerRespinsWithReplacedReels = reevalSpins[0].getType() == TRIGGERED_RESPINS_WITH_REPLACED_REELS;
		}

		return isTriggerRespinsWithReplacedReels;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (onReelsStoppedAnimationListParents.Count > 0)
		{
			int index = 0;
			foreach(AnimationListController.AnimationInformation animationInfo in onReelsStoppedAnimationList.animInfoList)
			{
				animationInfo.targetAnimator.transform.parent = onReelsStoppedAnimationListParents[index++];
			}
			onReelsStoppedAnimationListParents.Clear();
		}
		
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onPreSpinAnimationList));
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		List<SlotOutcome> reevalSpins = reelGame.reevaluationSpins;

		//safety check to ensure we have re-evaluation spins data to work with
		if (reevalSpins.Count < 1)
		{
			yield break;
		}

		SlotOutcome outcome = reevalSpins[0];
		replacementSymbolName = outcome.getOutcomeJsonValue(JSON.getStringStatic, REPLACE_SYMBOL, "");

		HashSet<int> reelIndices = outcome.getStaticReels();
		foreach (int reelIndex in reelIndices)
		{
			SlotSymbol[] symbols = reelGame.engine.getVisibleSymbolsAt(reelIndex);
			foreach (SlotSymbol symbol in symbols)
			{
				symbol.mutateTo(replacementSymbolName);
			}
		}
				
		foreach(AnimationListController.AnimationInformation animationInfo in onReelsStoppedAnimationList.animInfoList)
		{
			onReelsStoppedAnimationListParents.Add(animationInfo.targetAnimator.transform.parent);
			animationInfo.targetAnimator.transform.parent = BonusSpinPanel.instance.backgroundSpriteTransform;
			animationInfo.targetAnimator.transform.localPosition = Vector3.zero;
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onReelsStoppedAnimationList));
	}
}
