using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure for invite rewards. Duh.
*/

public class InviteRewards  : IResetGame
{
	public int tier;						///< currently 3 tiers: tier 1, 2, 3
	public int inviteCount;					///< number of invites this tier starts at. So tier 1 = 0
	public int nextTierInviteCount = 0;		///< number of invites required to reach the next tier. If highest tier, this is 0.
	public int creditAmount;				///< Amount of credits awarded when fulfilling this tier.
	public int vipPoints;					///< VIP points awarded when fulfilling this tier.
	
	// A dictionary of invite reward tiers, from global data.
	private static Dictionary<int, InviteRewards> _all = new Dictionary<int, InviteRewards>();	
	
	// Number of accepted credits this player has accepted. initialized from player data. affects reward schedule.
	public static int acceptedInvites = 0;

	// Number of incentive invites from this player that has been accepted by a friemd
	public static int acceptedIncentiveInvites = 0;

	// Number of invite incentivizes this player has claimed today. initialized from player data
	public static int incentivizeClaimsAccepted = 0;
	
	// Number of remaining invite incentivizes this player can claim today. initialized from player data
	public static int incentivizeClaimsRemaining = 0;
	
	public static int tierCount = 0;	///< The number of tiers there are.
			
	public InviteRewards(JSON item) 
	{			
		// Assuming all these fields are populated.
		tier = item.getInt("tier", 0);
		inviteCount = item.getInt("invite_count", 0);
		creditAmount = item.getInt("credit_amount", 0);
		vipPoints = item.getInt("vip_points", 0	);
		
		if (_all.ContainsKey(tier))
		{
			Debug.LogWarning("Duplicate InviteRewards tier: " + tier);
		}
		else
		{
			_all.Add(tier, this);
			tierCount++;
					
			if (tierCount > 1)
			{
				find(tierCount - 1).nextTierInviteCount = inviteCount;
			}
		}
	}

	/// factory method populating the all dictionary with classes of invite reward tiers from global data.
	public static void populateAll(JSON[] items)
	{
		foreach (JSON item in items)
		{
			new InviteRewards(item);
		}

		Server.registerEventDelegate("incentivized_invites_update", onIncentivizedInvitesUpdate);
	}
	
	public static InviteRewards find(int key)
	{
		if (_all.ContainsKey(key))
		{
			return _all[key];
		}
		return null;
	}
	
	/// Returns the InviteRewards at the current acceptedInvites value.
	public static InviteRewards getCurrentTier()
	{
		foreach (KeyValuePair<int, InviteRewards> kvp in _all)
		{
			InviteRewards inviteReward = kvp.Value;
			if (acceptedInvites < inviteReward.nextTierInviteCount)
			{
				return inviteReward;
			}
		}
		
		// Return the highest tier if the above doesn't find one.
		return _all[tierCount];
	}

	public static void onIncentivizedInvitesUpdate(JSON data)
	{
		if (data != null)
		{
			int newAcceptedIncentiveInvites = data.getInt("accepted_invite_count", 0);
			acceptedIncentiveInvites = System.Math.Max(acceptedIncentiveInvites, newAcceptedIncentiveInvites);
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<int, InviteRewards>();	
		acceptedInvites = 0;
		tierCount = 0;	///< The number of tiers there are.
	}	
}
