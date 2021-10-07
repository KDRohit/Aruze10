using TMPro;
using UnityEngine;

public enum ElitePointsDisplayType
{
	Coins = 0,
	RichPass,
	Spins,
}

public class ElitePointsListItem : MonoBehaviour
{
	[SerializeField] private ObjectSwapper stateSwapper;
	[SerializeField] private ObjectSwapper itemSwapper;
	[SerializeField] private TextMeshPro descriptionLabel;
	[SerializeField] private TextMeshPro buttonLabel;
	[SerializeField] private TextMeshPro claimedLabel;
	[SerializeField] private ButtonHandler actionButton;

	private ElitePointsDisplayType itemType;
	private EliteDialog.onActionButtonClickDelegate callback;
	
	public void setup(ElitePointsDisplayType itemType, EliteDialog.onActionButtonClickDelegate callback)
	{
		this.itemType = itemType;
		this.callback = callback;
		actionButton.registerEventDelegate(performAction);
		stateSwapper.setState("unclaimed");
		setupDisplay(itemType);
	}

	private void setupDisplay(ElitePointsDisplayType type)
	{
		switch (type)
		{
			case ElitePointsDisplayType.Coins:
				descriptionLabel.text = Localize.text("elite_points_from_purchase");
				itemSwapper.setState("coins");
				buttonLabel.text = "Shop Now";
				break;
			case ElitePointsDisplayType.RichPass:
				descriptionLabel.text = Localize.text("elite_points_from_richpass");
				itemSwapper.setState("richpass");
				buttonLabel.text = "Go Play";
				break;
			case ElitePointsDisplayType.Spins:
				itemSwapper.setState("spins");
				if (EliteManager.dailySpinCount >= EliteManager.maxSpinRewards * EliteManager.spinRewardThreshold)
				{
					stateSwapper.setState("claimed");
					descriptionLabel.text = Localize.text("elite_spins_limit");
					EliteManager.rolloverTimer.registerLabel(claimedLabel);
				}
				else
				{
					descriptionLabel.text = Localize.text("elite_points_from_spins_new", EliteManager.spinRewardThreshold);
				}
				buttonLabel.text = "Go Spin";
				break;
		}
	}

	private void performAction(Dict args = null)
	{
		Audio.play("ButtonConfirm");
		switch (itemType)
		{
			case ElitePointsDisplayType.Coins:
				StatsElite.logPointsListItemClicked("purchase_points");
				BuyCreditsDialog.showDialog();
				break;
			case ElitePointsDisplayType.RichPass:
				StatsElite.logPointsListItemClicked("rp_points");
				if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
				{
					RichPassFeatureDialog.showDialog(CampaignDirector.richPass);
				}
				break;
			case ElitePointsDisplayType.Spins:
				StatsElite.logPointsListItemClicked("spin_points");
				break;
		}

		if (callback != null)
		{
			callback();
		}
	}
}
