using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Used for the offer picking games
public class PickingGameOfferPickItem : PickingGameBasePickItemAccessor
{	
	public AnimationListController.AnimationInformation declinedState;	
	public AnimationListController.AnimationInformation acceptedState;
	public LabelWrapperComponent valueLabel;
	public LabelWrapperComponent valueShadowLabel;
	[Tooltip("List of labels to set credit value on, valueLabel and valueShadowLabel will be added to this list")]
	public List<LabelWrapperComponent> valueLabelList = new List<LabelWrapperComponent>();

	protected override void Awake()
	{
		base.Awake();

		if (Application.isPlaying)
		{
			if (valueLabel != null)
			{
				valueLabelList.Add(valueLabel);
			}

			if (valueShadowLabel != null)
			{
				valueLabelList.Add(valueShadowLabel);
			}
		}
	}

	//This will hold the value that we use to map between rounds
	private long creditValue;	
	public long offer
	{
		get { return creditValue; }
		set { creditValue = value; }
	}

	public void setValueLabels(long credits, bool isAbbreviating = false)
	{
		for (int i = 0; i < valueLabelList.Count; i++)
		{
			LabelWrapperComponent currentLabel = valueLabelList[i];

			if (currentLabel != null)
			{
				if (isAbbreviating)
				{
					currentLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credits, 1, shouldRoundUp: false);
				}
				else
				{
					currentLabel.text = CreditsEconomy.convertCredits(credits);
				}
			}
		}
	}
}
