using UnityEngine;
using System.Collections;
using TMPro;

public class GameOverlayFeatureDisplayMysteryGift : GameOverlayFeatureDisplay
{
	[SerializeField] private GameObject increasedChanceParent;
	[SerializeField] private ClickHandler increasedChanceButton;

	public override void init()
	{
		increasedChanceParent.SetActive( isIncreasedChanceActive );
		increasedChanceButton.clearAllDelegates();
		increasedChanceButton.registerEventDelegate(increasedChanceClicked);
		base.init();
	}

	public override bool shouldShow
	{
		get
		{
			switch (featureType)
			{
				case FeatureType.MYSTERY_GIFT:
					return SpinPanel.instance != null &&
						SpinPanel.instance.shouldShowMysteryGift &&
						!SpinPanel.instance.isShowingCollectionOverlay;
				case FeatureType.BIG_SLICE:
					return SpinPanel.instance != null &&
						SpinPanel.instance.shouldShowBigSlice &&
						!SpinPanel.instance.isShowingCollectionOverlay;
				default:
					return false;
			}
		}
	}

	public override void setButtons(bool isEnabled)
	{
		increasedChanceButton.enabled = isEnabled;
	}

	private bool isIncreasedChanceActive
	{
		get
		{
			switch (featureType)
			{
				case FeatureType.MYSTERY_GIFT:
					return MysteryGift.isIncreasedMysteryGiftChance;
				case FeatureType.BIG_SLICE:
					return MysteryGift.isIncreasedBigSliceChance;
				default:
					return false;
			}
		}
	}	
	
	private void increasedChanceClicked(Dict args = null)
	{
		switch (featureType)
		{
			case FeatureType.MYSTERY_GIFT:
				IncreaseMysteryGiftChanceMOTD.showDialog();
				break;
			case FeatureType.BIG_SLICE:
				IncreaseBigSliceChanceMOTD.showDialog();
				break;
			default:
				break;
		}
	}

	// TODO -- is this still needed?
	private void bigSliceClicked()
	{
		MOTDFramework.showMOTD("motd_big_slice");
	}
}

