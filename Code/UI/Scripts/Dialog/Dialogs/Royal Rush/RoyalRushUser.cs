using System;
using UnityEngine;

// Just holds relevant info for a rusher.
// Cannot be stored in the scorMap in SocialMember becuase there can be
// multiple RoyalRush instances running at the same time.
public class RoyalRushUser
{
	public SocialMember member = null;
	public long score;
	public int position;
	
	public string zid { get { return member.zId; } }
	public string fbid { get { return member.id; } }
	public string name { get { return member.firstName; } }
	public string photoURL { get { return member.getImageURL; } }
	public long achievementScore{ get { return member.achievementScore; } }
	
	public RoyalRushUser(JSON data = null)
	{
		if (data != null)
		{
			member = CommonSocial.findOrCreate(
				zid: data.getString("id", ""),
				fbid: data.getString("fb_id", "-1"),
				firstName: data.getString("name", "Guest"),
				imageUrl: data.getString("photo_url", ""),
				achievementScore:data.getLong("achievement_score", -1));
			
			score = data.getLong("score", 0);
			position = data.getInt("competition_rank", 0);

		}
	}

	public override string ToString()
	{
		return member.ToString();
	}
}
