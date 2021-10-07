using UnityEngine;
using System.Collections;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class DtwWheel : TICoroutineMonoBehaviour
{
	public GameObject wheel;
	
	public Transform bonusWheelSprite;
	public int bonusWhiteSliceIndex;
	
	public Transform multiplierWheelSprite;
	public int multiplierWhiteSliceIndex;
	
	public FacebookFriendInfo friendInfo;
	
	public GameObject pieSliceAnchor;
	public GameObject pieSlicePrefab;
	
	[System.NonSerialized] public Transform wheelSprite;
	[System.NonSerialized] public int whiteSliceIndex;
	
	public void SetAsBonusWheel()
	{
		wheelSprite = bonusWheelSprite;
		whiteSliceIndex = bonusWhiteSliceIndex;
		
		multiplierWheelSprite.gameObject.SetActive(false);
	}
	
	public void SetAsMultiplierWheel()
	{
		wheelSprite = multiplierWheelSprite;
		whiteSliceIndex = multiplierWhiteSliceIndex;
		
		bonusWheelSprite.gameObject.SetActive(false);
	}
}
