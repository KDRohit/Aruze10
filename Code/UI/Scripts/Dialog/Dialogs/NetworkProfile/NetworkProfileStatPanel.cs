using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NetworkProfileStatPanel : MonoBehaviour
{

	// All SKUs
	public TextMeshPro totalWinLabel;
	public TextMeshPro highestLevelLabel;
	public TextMeshPro lastOnlineLabel;
	public TextMeshPro favoriteMachineLabel;
	public TextMeshPro biggestWinLabel;

	// HIR
	public TextMeshPro dailyRaceLabel;
	public TextMeshPro installTimeLabel;	
	
	// Wonka
	public TextMeshPro gobstopperWinsLabel;
	public TextMeshPro puzzlesCompletedLabel;
	
	// Woz
	public TextMeshPro singleChallengesCompleteLabel;
	public TextMeshPro jackpotsWonLabel;
	public TextMeshPro totalChallengesCompleteLabel;

	public void setLabels(Dictionary<string, string> stats)
	{
		if (stats == null)
		{
			// If there are no stats then just set everything to -- to avoid NRE from accessing it.
		    SafeSet.labelText(totalWinLabel, "--");
			SafeSet.labelText(highestLevelLabel, "--");
			SafeSet.labelText(installTimeLabel, "--");
			SafeSet.labelText(lastOnlineLabel, "--");
			SafeSet.labelText(favoriteMachineLabel, "--"); // Previously the only "--"
			SafeSet.labelText(biggestWinLabel, "--");
			SafeSet.labelText(dailyRaceLabel, "--");
			SafeSet.labelText(gobstopperWinsLabel, "--");
			SafeSet.labelText(puzzlesCompletedLabel, "--");
			SafeSet.labelText(singleChallengesCompleteLabel, "--");
			SafeSet.labelText(jackpotsWonLabel, "--");
			SafeSet.labelText(totalChallengesCompleteLabel, "--");
			return;
		}

		setLabel(totalWinLabel, "sku_total_win", stats, "--", true);
		setLabel(highestLevelLabel, "sku_highest_level", stats, "--", true);
		setLabel(favoriteMachineLabel, "sku_favorite_machine", stats, "--");
		setLabel(biggestWinLabel, "sku_biggest_win", stats, "--", true);
		setLastOnlineLabel(lastOnlineLabel, "sku_last_online", stats);

		// HIR Only
		setLabel(dailyRaceLabel, "daily_race_wins_30_days", stats, "--", true);
		setLabel(installTimeLabel, "sku_install_time", stats, "--");

		// Wonka Only
		setLabel(gobstopperWinsLabel, "gobstopper_wins", stats, "--", true);
		setLabel(puzzlesCompletedLabel, "puzzles_complete", stats, "--", true);
		
		// Woz Only
		setLabel(singleChallengesCompleteLabel, "single_challenges_complete", stats, "--", true);
		setLabel(jackpotsWonLabel, "jackpots_won", stats, "--", true);
		setLabel(totalChallengesCompleteLabel, "total_challenges_complete", stats, "--", true);
	}
	
	private void setLabel(TextMeshPro label, string key, Dictionary<string, string> stats, string defaultValue, bool shouldFormatAsNumber = false)
	{
		string text = (stats.ContainsKey(key) && !string.IsNullOrEmpty(stats[key])) ? stats[key] : defaultValue;
		if (shouldFormatAsNumber && text != "--")
		{
			long number = 0;
			if (long.TryParse(text, out number))
			{
				if (key == "sku_biggest_win")
				{
					text = CommonText.formatNumberAbbreviated(number, 1); // 1 being the number of decimals we want.
				}
				else
				{
					text = CommonText.formatNumber(number);
				}
			}
			else
			{
				Debug.LogErrorFormat("NetworkProfileStatPanel.cs -- setLabel -- attempted to parse {0} as long as failed", text);
				text = defaultValue;
			}
		}
		SafeSet.labelText(label, text);
	}
	
	private void setLastOnlineLabel(TextMeshPro label, string key, Dictionary<string, string> stats)
	{
		string text = "";
		long lastOnline = 0;
		if (stats.ContainsKey(key))
		{
			if (!long.TryParse(stats[key], out lastOnline))
			{
				lastOnline = 0;
			}
		}
		if (lastOnline > 0)
		{
			double seconds = System.Convert.ToDouble(lastOnline);
			System.DateTime lastOnlineTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			lastOnlineTime = lastOnlineTime.AddSeconds(seconds);
			System.TimeSpan difference = System.DateTime.UtcNow.Subtract(lastOnlineTime);
			if (difference.Days > 7)
			{
				text = Localize.text("more_than_one_week_ago");
			}
			else if (difference.Days > 0)
			{
				text = Localize.text("{0}_days_ago", difference.Days);
			}
			else if (difference.Hours > 0)
			{
				text = Localize.text("{0}_hours_ago", difference.Hours);
			}
			else if (difference.Minutes > 30)
			{
				text = Localize.text("{0}_minutes_ago", difference.Minutes);
			}
			else
			{
				text = Localize.textTitle("online_now");
			}
		}
		else
		{
		    text = "--";
		}
		SafeSet.labelText(label, text);
	}
	
	
}