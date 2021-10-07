using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Class specific to credit reveals on pick items
 */
public class PickingGameCreditPickItem : PickingGameBasePickItemAccessor
{
	// Allows the use of more than one PickingGameCreditPickItem tagged for the modules that will use that version,
	// if the specific version isn't defined then it you will use the basic Default version
	public enum CreditsPickItemType
	{
		Default = 0,
		Advance = 1,
		IncreasePicks = 2,
		Multiplier = 3,
		Jackpot = 4,
		Bad = 5,
		Gameover = 6
	}

	public LabelWrapperComponent creditLabel;
	public LabelWrapperComponent grayCreditLabel;
	public LabelWrapperComponent creditLabelAbbreviated;
	public LabelWrapperComponent grayCreditLabelAbbreviated;

	public CreditsPickItemType creditsPickItemType = CreditsPickItemType.Default;
	public bool isVerticalText = false;
	public string localizationKey = "";

	private long currentCreditsValue = 0; // track the stored credits value so that a rollup can be performed on the label

	public GameObject getLabelGameObject()
	{
		if (creditLabel != null)
		{
			return creditLabel.gameObject;
		}
		else if (creditLabelAbbreviated != null)
		{
			return creditLabelAbbreviated.gameObject;
		}

		Debug.LogError("PickingGameCreditPickItem.getLabelGameObject() - Trying to get game object but neither creditLabel nor creditLabelAbbreviated are set, returning NULL!");
		return null;
	}
	
	//Sets credit labels for revealed & leftovers
	public virtual void setCreditLabels(long credits)
	{
		string creditText = CreditsEconomy.convertCredits(credits, !isVerticalText);
		if (!string.IsNullOrEmpty(localizationKey))
		{
			creditText = Localize.text(localizationKey, creditText);
		}
		if (isVerticalText)
		{
			creditText = CommonText.makeVertical(creditText);
		}

		if (creditLabel != null)
		{
			creditLabel.text = creditText;
		}

		if (grayCreditLabel != null)
		{
			grayCreditLabel.text = creditText;
		}

		string abbreviatedText = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credits, 2, shouldRoundUp: false);

		if (creditLabelAbbreviated != null)
		{
			creditLabelAbbreviated.text = abbreviatedText;
		}

		if (grayCreditLabelAbbreviated != null)
		{
			grayCreditLabelAbbreviated.text = abbreviatedText;
		}

		currentCreditsValue = credits;
	}

	public Transform getCreditLabelTransform()
	{
		if (creditLabel != null)
		{
			return creditLabel.transform;
		}
		else if (creditLabelAbbreviated != null)
		{
			return creditLabelAbbreviated.transform;
		}
		else
		{
			return null;
		}
	}

	public IEnumerator rollupOnCreditsLabels(long startingCredits, long endingCredits)
	{
		List<TICoroutine> rollupCoroutines = new List<TICoroutine>();

		if (creditLabel != null)
		{
			rollupCoroutines.Add(StartCoroutine(SlotUtils.rollup(startingCredits, endingCredits, creditLabel)));
		}
		else if (creditLabelAbbreviated != null)
		{
			rollupCoroutines.Add(StartCoroutine(SlotUtils.rollup(startingCredits, endingCredits, creditLabelAbbreviated)));
		}

		if (grayCreditLabel != null)
		{
			rollupCoroutines.Add(StartCoroutine(SlotUtils.rollup(startingCredits, endingCredits, grayCreditLabel)));
		}
		else if (grayCreditLabelAbbreviated != null)
		{
			rollupCoroutines.Add(StartCoroutine(SlotUtils.rollup(startingCredits, endingCredits, grayCreditLabelAbbreviated)));
		}

		if (rollupCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(rollupCoroutines));
		}
	}

	public IEnumerator rollupOnCreditsLabels(long endingCredits)
	{
		yield return StartCoroutine(rollupOnCreditsLabels(currentCreditsValue, endingCredits));
	}

	// Search for the PickingGameCreditPickItem with the matching type, will fallback to trying to return Default if that type doesn't exist
	// (i.e. some games will use the same data for all credits, but some may have different labels)
	public static PickingGameCreditPickItem getPickingGameCreditsPickItemForType(GameObject gameObject, CreditsPickItemType type)
	{
		PickingGameCreditPickItem[] creditsPickItemArray = gameObject.GetComponents<PickingGameCreditPickItem>();
		PickingGameCreditPickItem defaultCreditsPickItem = null;

		foreach (PickingGameCreditPickItem creditsPickItem in creditsPickItemArray)
		{
			if (creditsPickItem.creditsPickItemType == type)
			{
				return creditsPickItem;
			}
			else if (creditsPickItem.creditsPickItemType == CreditsPickItemType.Default)
			{
				defaultCreditsPickItem = creditsPickItem;
			}
		}

		return defaultCreditsPickItem;
	}
}
