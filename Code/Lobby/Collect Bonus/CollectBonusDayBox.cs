using UnityEngine;
using System.Collections;
using TMPro;
public class CollectBonusDayBox : TICoroutineMonoBehaviour 
{
	public TextMeshPro dayCount;
	public TextMeshPro score;
	public TextMeshPro scoreInactive;

	// Just the word "day"
	public TextMeshPro dayText;
	public GameObject calendarSprite;

	private Color textBlueColor = new Color(0.32f, 0.61f, 0.79f, 0.5f);
	private const float STREAK_ALPHA = 0.5f;
	
	public void init(int dayNum)
	{
		dayCount.text = CommonText.formatNumber(dayNum + 1);

		// If we should darken this
		if (SlotsPlayer.instance.dailyBonusTimer.day - 1 < dayNum)
		{
			// We have the darkened sprite layered behind it, so this is just the 
			// easiest way to show it.
			calendarSprite.SetActive(false);

			score.color = textBlueColor;
			dayText.color = textBlueColor;
		} 
        // The 6 here is because the array of day boxes is 0 based... so really it's a 7  
        // to denote the amount of days in the week
        else if (SlotsPlayer.instance.dailyBonusTimer.day - 1 > dayNum && dayNum != 6)
        {
            CommonGameObject.alphaGameObject(calendarSprite, STREAK_ALPHA);
            dayText.alpha = STREAK_ALPHA;
            score.alpha = STREAK_ALPHA;
        }
	}
}
