using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class AchievementsDisplayBanner : MonoBehaviour 
{
	[SerializeField] private string rarity = "";
	[SerializeField] private TextMeshPro percentText;
	[SerializeField] private List<GameObject> percentObjects;


	public string getRarity()
	{
		return rarity;
	}
	
	public void init(float percentValue)
	{
		bool isEnabled = percentValue >= 0.1;  //we only show one decimal place so make sure we have at least this much
		enablePercentText(isEnabled);

		if (isEnabled && percentText != null)
		{
			int percent = System.Convert.ToInt32(percentValue); //drop the decimal
			float decimalValue = Mathf.Abs((float)percentValue - percent);
			if (decimalValue < 0.1f)
			{
				//don't show decimal
				percentText.text = Localize.text("top_percent", percent);
				
			}
			else
			{
				percentText.text = Localize.text("top_percent_with_decimal", percentValue);
			}
		}

	}

	private void enablePercentText(bool isEnabled)
	{
		if (null == percentObjects)
		{
			return;
		}

		for(int i=0; i<percentObjects.Count; ++i)
		{
			if (percentObjects[i] == null)
			{
				continue;
			}

			percentObjects[i].SetActive(isEnabled);
		}
	}
	
}
