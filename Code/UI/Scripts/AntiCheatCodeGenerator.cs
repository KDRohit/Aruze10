using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiCheatCodeGenerator : MonoBehaviour 
{
	public List<UISprite> month = new List<UISprite>();
	public List<UISprite> day = new List<UISprite>();
	public List<UISprite> hours = new List<UISprite>();
	public List<UISprite> minutes = new List<UISprite>();
	public List<UISprite> winnings = new List<UISprite>();

	public float monthGap = 35.0f;
	private float gapMultiplier = 0.0f;

	// Use this for initialization
	void Start () 
	{
		System.DateTime dtDateTime = System.DateTime.Now;

		Color monthColor = Color.blue;
		int seasonValue = dtDateTime.Month - 9;

		if (dtDateTime.Month < 4)
		{
			monthColor = Color.green;
			seasonValue = dtDateTime.Month;
		}
		else if (dtDateTime.Month < 7)
		{
			monthColor = Color.white;
			seasonValue = dtDateTime.Month - 3;
		}
		else if (dtDateTime.Month < 10)
		{
			monthColor = Color.red;
			seasonValue = dtDateTime.Month - 6;
		}

		gapMultiplier = month.Count-1;
		int	bitValue = 1;
		foreach (UISprite monthBit in month)
		{
			CommonTransform.addX(monthBit.transform, gapMultiplier * monthGap);

			if (bitValue == 4)
			{
				monthBit.color = getRandomColor();
				monthBit.alpha = Random.Range(0,100)/100.0f;
			}
			else
			{
				monthBit.color = monthColor;
				if ((seasonValue & bitValue) == 0)
				{
					monthBit.alpha = .5f;
				}			
			}

			bitValue = bitValue << 1;
			gapMultiplier--;
		}

		int dayValue = dtDateTime.Day;
		Color dayColor = Color.white;

		if (dayValue >= 30)
		{
			dayValue = dayValue - 30;
			dayColor = Color.red;
		}
		else if (dayValue >= 20)
		{
			dayValue = dayValue - 20;
			dayColor = Color.blue;
		}
		else if (dayValue >= 10)
		{
			dayValue = dayValue - 10;
			dayColor = Color.green;
		}

		gapMultiplier = day.Count - 1;
		bitValue = 1;
		foreach (UISprite dayBit in day)
		{
			CommonTransform.addX(dayBit.transform, gapMultiplier * monthGap);
			dayBit.color = dayColor;

			if ((dayValue & bitValue) == 0)
			{
				dayBit.alpha = .5f;
			}

			bitValue = bitValue << 1;
			gapMultiplier--;
		}

		// hours and minutes
		int spriteIndex = 0;
		gapMultiplier = hours.Count - 1;
		spriteIndex = outputDigits(dtDateTime.Hour, spriteIndex, hours);
		gapMultiplier = minutes.Count - 1;
		spriteIndex = 0;
		spriteIndex = outputDigits(dtDateTime.Minute, spriteIndex, minutes);


		long payout = BonusGameManager.instance.finalPayout/100;
		long billions = payout/1000000000L;
		payout -= billions * 1000000000L;
		long millions = payout/1000000L;
		payout -= millions * 1000000L;
		long thousands = payout/1000L;

		gapMultiplier = winnings.Count - 1;
		spriteIndex = 0;

		spriteIndex = outputDigits(billions, spriteIndex, winnings);
		spriteIndex = outputDigits(thousands, spriteIndex, winnings);
		spriteIndex = outputDigits(millions, spriteIndex, winnings);

	}

	private Color getRandomColor()
	{
		switch (Random.Range(1,5))
		{
			case 1:
				return Color.red;
			case 2:
				return Color.blue;
			case 3:
				return Color.green;
			case 4:
				return Color.cyan;
			case 5:
				return Color.magenta;
			default:
				return Color.white;					
		}
	}

	private int outputDigits(long sourceNumber, int spriteIndex, List<UISprite> spriteList)
	{
		for (int i = 0; i < 3; i++)
		{
			if (spriteIndex < spriteList.Count)
			{
				CommonTransform.addX(spriteList[spriteIndex].transform, gapMultiplier * monthGap);
				gapMultiplier--;

				setDigitSprite(sourceNumber, spriteIndex, spriteList);
				sourceNumber = sourceNumber/10L;
			}

			spriteIndex++;
		}

		return spriteIndex;
	}

	private void setDigitSprite(long sourceNumber, int spriteIndex, List<UISprite> spriteList)
	{
		long n = sourceNumber % 10L;
		Color winColor = Color.white;

		if (n > 7)
		{
			winColor = Color.red;
		}
		else if (n > 5)
		{
			winColor = Color.magenta;
		}
		else if (n > 3)
		{
			winColor = Color.blue;
		}
		else if (n > 1)
		{
			winColor = Color.green;
		}
		else
		{
			winColor = Color.white;
		}

		if (spriteIndex < spriteList.Count)
		{
			spriteList[spriteIndex].color = winColor;

			if ((n & 1L) > 0)
			{
				spriteList[spriteIndex].alpha = .5f;
			}
		}
	}
}
