using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseCustomBonusSpinPanelModule : SlotModule
{
	[SerializeField] private LabelWrapperComponent spinRemainingLabel;
	[SerializeField] private LabelWrapperComponent winningsAmountLabel;
	[SerializeField] private LabelWrapperComponent wagerAmountLabel;

	[SerializeField] private BoxCollider2D customSpinPanelBounds;

	//Store these so we can reset them once the bonus game is over
	private TMPro.TextMeshPro defaultSpinCountLabel;
	private TMPro.TextMeshPro defaultWinningsAmountLabel;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		// safely populate the spin panel labels
		populateLabel(spinRemainingLabel, BonusSpinPanel.instance.spinCountLabel.text);
		populateLabel(winningsAmountLabel, BonusSpinPanel.instance.winningsAmountLabel.text);
		populateLabel(wagerAmountLabel, CommonText.formatNumber(CreditsEconomy.multipliedCredits(reelGame.multiplier * reelGame.slotGameData.baseWager)));

		defaultSpinCountLabel = BonusSpinPanel.instance.spinCountLabel;
		defaultWinningsAmountLabel = BonusSpinPanel.instance.winningsAmountLabel;

		// reassign the labelwrapper component textmesh pro labels to the BonusSpinPanel instance labels.
		if (spinRemainingLabel.tmProLabel != null)
		{
			BonusSpinPanel.instance.spinCountLabel = spinRemainingLabel.tmProLabel;
		}

		if (winningsAmountLabel != null)
		{
			BonusSpinPanel.instance.winningsAmountLabel = winningsAmountLabel.tmProLabel;
		}
	}

	public override bool needsToExecuteOnFreespinGameEnd ()
	{
		return true;
	}

	public override IEnumerator executeOnFreespinGameEnd ()
	{
		BonusSpinPanel.instance.spinCountLabel = defaultSpinCountLabel;
		BonusSpinPanel.instance.winningsAmountLabel = defaultWinningsAmountLabel;
		yield break;
	}

	public override bool needsToUseACustomSpinPanelSizer ()
	{
		return customSpinPanelBounds != null;
	}

	public override BoxCollider2D getCustomSpinPanelSizer ()
	{
		return customSpinPanelBounds;
	}

	// safe populates labels
	private void populateLabel(LabelWrapperComponent labelWrapper, string labelText)
	{
		if(labelWrapper != null && labelText != null)
		{
			labelWrapper.text = labelText;
		}
	}
}

