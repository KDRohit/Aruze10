using UnityEngine;
using System.Collections;
using TMPro;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class DtwText : TICoroutineMonoBehaviour
{
	public TextMeshPro valueLabel;
	
	public Color blackColor = new Color(0,0,0);
	public Color redColor = new Color(255,0,0);
	
	public void setCredits(int iSlice,int credits,int iWhiteSlice)
	{
		// credits = 2931; // test thousands label		
		valueLabel.text = CommonText.makeVertical(CreditsEconomy.convertCredits((long)credits, false).Trim());
		placeText(iSlice,iWhiteSlice);
	}
	
	public void setMultiplier(int iSlice,int multiplier,int iWhiteSlice)
	{
		valueLabel.text = multiplier.ToString() + "\nX";

		placeText(iSlice,iWhiteSlice);
	}
	
	public void placeText(int iSlice,int iWhiteSlice)
	{
		const int numSlices = 8;
		
		if (iSlice == iWhiteSlice)
		{
			valueLabel.color = redColor;
			//valueLabel.material = blackColor;
		}
		
		gameObject.transform.localEulerAngles = new Vector3(0f , 0f , 360f*iSlice/numSlices);
	}
}
