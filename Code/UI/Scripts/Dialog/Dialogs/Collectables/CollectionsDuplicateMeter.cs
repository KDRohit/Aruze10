using UnityEngine;
using System.Collections;

public class CollectionsDuplicateMeter : MonoBehaviour 
{
	public LabelWrapperComponent starProgressLabel;
	public GameObject progressSprite;
	public Transform starTarget;
	public GameObject starMeterToolTip;
	public Transform starParent;

	public ButtonHandler toolTipButton;

	private int currentValue = 0;
	private int maxValue = 0;
	private string packId = "";

	private float maxSpriteLength = 0f;
	private CollectableAlbum currentAlbum = null;

	public void init(CollectableAlbum currentAlbum, string packId = "", bool wasStarPackDataFound = false, string source = "")
	{
		if (currentAlbum == null)
		{
			return;
		}

		this.currentAlbum = currentAlbum;
		if(toolTipButton != null)
		{
			toolTipButton.registerEventDelegate(onMeterClicked);
		}
		maxSpriteLength = progressSprite.transform.localScale.x;

		currentValue = currentAlbum.currentDuplicateStars;
		maxValue = currentAlbum.maxStars;

		if (!wasStarPackDataFound)
		{
			if (currentValue >= maxValue && Dialog.instance.currentDialog != null && !string.IsNullOrEmpty(Dialog.instance.currentDialog.userflowKey))
			{
				Userflows.flowStart(Dialog.instance.currentDialog.userflowKey);
				Userflows.logError("User was missing star pack data but had enough stars!","missing_starpack_data");
				Userflows.addExtraFieldToFlow("missing_starpack_data", "card_pack_id", packId);
				Userflows.addExtraFieldToFlow("missing_starpack_data", "source", source);
			}
		}
		
		if(starProgressLabel != null)
		{
			starProgressLabel.text = string.Format("{0}/{1}", currentValue, maxValue);
		}
		float meterProgress = ((float)currentValue/(float)maxValue) * maxSpriteLength;
		progressSprite.transform.localScale = new Vector3(meterProgress, progressSprite.transform.localScale.y, 1.0f);
	}

	public void addToStarMeter(int amountToAdd, int finalStarCount)
	{
		if (currentValue == 0 && Collectables.showStarMeterToolTip && starMeterToolTip != null)
		{
			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "dupes_tooltip",
				family: packId,
				genus: "view");
			starMeterToolTip.SetActive(true);
			Collectables.showStarMeterToolTip = false;
		}

		currentValue += amountToAdd;
		starProgressLabel.text = string.Format("{0}/{1}", currentValue, maxValue);
		float meterProgress = ((float)currentValue/(float)maxValue) * maxSpriteLength;
		progressSprite.transform.localScale = new Vector3 (meterProgress, progressSprite.transform.localScale.y, 1.0f);
	}

	public void resetMeter()
	{

	}

	private void onMeterClicked(Dict args = null)
	{
		if(starMeterToolTip != null)
		{
			if (!starMeterToolTip.activeSelf)
			{
				starMeterToolTip.SetActive(true);
				StatsManager.Instance.LogCount(counterName:"dialog",
					kingdom: "hir_collection",
					phylum: "dupes_tooltip",
					family: packId,
					genus: "click");
			}
		}
	}
}
