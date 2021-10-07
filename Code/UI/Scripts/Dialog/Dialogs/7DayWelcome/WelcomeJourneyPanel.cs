using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WelcomeJourneyPanel : MonoBehaviour
{

	[SerializeField] private GameObject coinParent;
	[SerializeField] private TextMeshPro dayNumLabel;
	[SerializeField] private TextMeshPro dayAwardLabel;
	[SerializeField] private ClickHandler clickHandler;

	private GameObject coinStack = null;

	public void init(GameObject coinPrefab, int day, int award, bool collected, ClickHandler.onClickDelegate onClick)
	{
		if (coinPrefab != null)
		{
			coinStack = CommonGameObject.instantiate(coinPrefab, coinParent.transform) as GameObject;
			if (coinStack != null)
			{
				//rename so animation lines up
				int cloneIndex = coinStack.name.IndexOf("(Clone)");
				if (cloneIndex >= 0)
				{
					coinStack.name = coinStack.name.Substring(0, cloneIndex);
				}
			}
			else
			{
				Debug.LogErrorFormat("WelcomeJourneyPanel.cs -- init -- instantiated a null object");
			}
		}

		if (clickHandler != null)
		{
			if (onClick != null)
			{
				clickHandler.enabled = true;
				clickHandler.registerEventDelegate(onClick);
			}
			else
			{
				clickHandler.enabled = false;
			}
		}

		//text
		string dayFormat = Localize.text("welcome_journey_day");
		dayNumLabel.text = string.Format(dayFormat, day.ToString());
		dayAwardLabel.text = CreditsEconomy.convertCredits(award);


	}

	private void OnDestroy()
	{
		//remove delegates
		if (null != clickHandler)
		{
			clickHandler.unregisterEventDelegate(null, true);
		}
	}
}
