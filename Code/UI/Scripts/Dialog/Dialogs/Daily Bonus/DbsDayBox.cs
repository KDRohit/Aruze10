using UnityEngine;
using System.Collections;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class DbsDayBox : TICoroutineMonoBehaviour
{
	public UISprite backgroundSprite;
	public TextMeshPro dayLabel;
	public TextMeshPro multiplierLabel;
	public UISprite checkmarkSprite;
	public GameObject animSpot;
	public GameObject throwAnchor;
	
	[System.NonSerialized] public int day;
	[System.NonSerialized] public long bonus;
	
	public Color dayLabelYesterdayColor = new Color(0.5f , 0.5f , 0.5f);
	public Color multiplierLabelYesterdayColor = new Color(0.25f , 0.25f , 0.25f);
	
	public Color dayLabelTodayColor = new Color(1f , 1f , 1f);
	public Color multiplierLabelTodayColor = new Color(1f , 1f, 1f);
	
	public Color dayLabelTomorrowColor = new Color(1f , 1f , 1f);
	public Color multiplierLabelTomorrowColor = new Color(0.5f , 0.5f , 0.5f);

	public static int today = 0;
	
	/// Called before calling init() for each day.
	public static void setToday()
	{
		//Allowing for an 8th day, which uses day 7 bonus.  This is used for every day after the max of 7 days.
		today = SlotsPlayer.instance.dailyBonusTimer.day <= 7 ? SlotsPlayer.instance.dailyBonusTimer.day : 8;
	}
	
	public void init(int iDay)
	{
		if (iDay + 1 < today)
		{
			dayLabel.color = dayLabelYesterdayColor;
			multiplierLabel.color = multiplierLabelYesterdayColor;
			
			backgroundSprite.spriteName = "Date Collected";
		}
		else if (iDay + 1 == today)
		{
			dayLabel.color = dayLabelTodayColor;
			multiplierLabel.color = multiplierLabelTodayColor;
			
			backgroundSprite.spriteName = "Date Active";
		}
		else
		{
			dayLabel.color = dayLabelTomorrowColor;
			multiplierLabel.color = multiplierLabelTomorrowColor;
			
			backgroundSprite.spriteName = "Date Inactive";
		}
		day = iDay;
		
		int xpLevel = SlotsPlayer.instance.socialMember.experienceLevel;
		
		dayLabel.text = Localize.textUpper("day_{0}", iDay + 1);
		
		bonus = GlobalTimer.findPayout("bonus", xpLevel, iDay + 1);
		multiplierLabel.text = CommonText.formatNumber(bonus);
				
		//3,4
		if (iDay + 1 >= today)
		{
			checkmarkSprite.gameObject.SetActive(false);
		}
	}
}
