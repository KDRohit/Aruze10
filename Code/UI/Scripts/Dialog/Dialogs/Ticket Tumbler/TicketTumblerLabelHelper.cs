using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TicketTumblerLabelHelper : MonoBehaviour 
{
	public List<TextMeshPro> prizeCredits = new List<TextMeshPro>();		

	public TextMeshPro 	eventTimeLeft;
	public TextMeshPro 	roundTimeLeft;
	public TextMeshPro 	roundLength;
	public TextMeshPro 	numTickets;
	public TextMeshPro 	numTickets2;
	public TextMeshPro 	vipLevel;
	public TextMeshPro 	ticketLabel;
	public TextMeshPro 	ticketEnteredLabel;
	public VIPIconHandler vipIconHandler;
	public bool alwaysZero;

	public void setCredits(long credits, bool hyperEconimize = true)
	{
		string creditStr;

		creditStr = hyperEconimize ? CreditsEconomy.convertCredits(credits) : CommonText.formatNumber(credits);

		foreach (TextMeshPro tm in prizeCredits)
		{
			SafeSet.labelText(tm, creditStr);
		}
	}

	public void setEventTimeLeft(GameTimerRange timer)
	{
		if (eventTimeLeft != null)
		{
			eventTimeLeft.text += "\n";
			timer.registerLabel(eventTimeLeft, GameTimerRange.TimeFormat.REMAINING, true);
		}
	}

	public void setRoundTimeLeft(GameTimerRange timer)
	{
		if (roundTimeLeft != null)
		{
			if (alwaysZero)
			{
				roundTimeLeft.text = "00:00";
			}
			else
			{
				timer.registerLabel(roundTimeLeft, GameTimerRange.TimeFormat.REMAINING, false);
			}
		}
	}

	public void setRoundLength(int totalTime)
	{
		if (roundLength != null)
		{
			roundLength.text = CommonText.formatNumber(totalTime);
		}
	}

	public void setTicketCountLabelsForRollup(int curCount, int maxCount)
	{
		SafeSet.labelText(numTickets, CommonText.formatNumber(curCount));
		SafeSet.labelText(numTickets2, CommonText.formatNumber(maxCount - curCount));
	}

	public void setNumTickets(int numTix)
	{
		if (numTickets != null)
		{
			numTickets.text = CommonText.formatNumber(numTix);
		}

		if (numTickets2 != null)
		{
			numTickets2.text = CommonText.formatNumber(numTix);
		}

		if (numTix == 1)
		{
			SafeSet.labelText(ticketLabel, Localize.text("ticket"));
			SafeSet.labelText(ticketEnteredLabel, Localize.text("ticket_entered"));
		}
		else
		{
			SafeSet.labelText(ticketLabel, Localize.text("tickets"));
			SafeSet.labelText(ticketEnteredLabel, Localize.text("tickets_entered"));
		}
	}

	public void setVipTier(VIPLevel myLevel)
	{
		if (myLevel != null)
		{
			if (vipIconHandler != null)
			{
				vipIconHandler.setLevel(myLevel.levelNumber);
			}

			if (vipLevel != null && myLevel.purchaseBonusPct > 0)
			{
				vipLevel.text = Localize.text("gem_prize{0}", myLevel.name);
			}
		}
	}		
}
