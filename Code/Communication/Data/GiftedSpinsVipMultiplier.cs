/*
  Class Name: GiftedSpinsVipMultiplier.cs
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Description: Manages the Gifted Spins Vip Multiplier Accessors.
  Feature-flow: 

  This feature is controlled via EOS on both server and client side.
  EOS Experiment: gifted_spins_vip_multiplier
  
  The MOTD is controlled via the motd system, and gated via the EOS experiment.

  The MOTD grants the user gifted free spins when they click play. This causes an action
  "play_vip_gifted_freespin_game" (PlayerAction.cs) to be sent up to the server.

  The event "vip_gifted_freespin_outcome" (here) then comes down with the outcome for the game.
  
  Once that has come down, the timestamp key "vip_gifted_freespin_ts" will be sent down
  in player.my_timestamps so that we know not to send it again.
  
*/

using UnityEngine;
using System.Collections;

public static class GiftedSpinsVipMultiplier
{
	public static string freeSpinGame = "gen09"; // Hard-coded as per PM request for now.
	public static string timestampValue = "";

	// The current multiplier for the player based on their new vip level;
	public static long playerMultiplier = 1;
	public const string TIMESTAMP_KEY = "vip_gifted_freespin_ts";

	public const string BADGE_PREFAB_PATH = "Features/Gifted Spins VIP Multiplier/Prefabs/Bonus Badge";
	
	public static void giftGrantCallback(JSON response)
	{
		/*GiftChestItem item = new GiftChestItem();
		item.slotsGameKey = freeSpinGame;
		item.bonusGameType = "gifting";
		item.outcomeJSON = response;
		MOTDFramework.queueCallToAction(item);*/
	}
}
