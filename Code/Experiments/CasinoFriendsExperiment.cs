using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CasinoFriendsExperiment : EueActiveDiscoveryExperiment
{
	public CasinoFriendsExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		base.init(data);
		NetworkFriends.instance.friendLimit = getEosVarWithDefault(data, "friend_limit", NetworkFriends.DEFAULT_FRIEND_LIMIT);
		NetworkFriends.instance.pendingRequestLimit = getEosVarWithDefault(data, "pending_request_limt", NetworkFriends.DEFAULT_FRIEND_REQUEST_LIMIT);
		NetworkFriends.instance.toasterCooldown = getEosVarWithDefault(data, "toaster_cooldown", NetworkFriends.DEFAULT_TOASTER_COOLDOWN);
		NetworkFriends.instance.sugggestionDisplayLimit = getEosVarWithDefault(data, "suggestions_display_limit", NetworkFriends.DEFAULT_MAX_SUGGESTED_FRIENDS);
	}

	public override void reset()
	{
		base.reset();
	}
}
