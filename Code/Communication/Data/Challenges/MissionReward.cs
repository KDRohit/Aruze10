using UnityEngine;

public class MissionReward : ChallengeReward
{
	public string game;
	public string packIndex;
	public string packType;
	public string cardPackKeyName;
	public string image;
	
	public MissionReward(JSON data = null) : base (data)
	{
	}

	public MissionReward(RewardType typeOfReward, long quantity)
	{
		type = typeOfReward;
		amount = quantity;
	}
	protected override void parse(JSON data)
	{
		type = getTypeFromString(data.getString("definition", string.Empty));
		//need to use temp variable because amount is a property
		long temp = 0;
		if (long.TryParse(data.getString("count", ""), out temp))
		{
			amount = temp;
		}
		else
		{
			amount = 0;
		}
		game = data.getString("games", null);
		packIndex = data.getString("pack_index", string.Empty);
		packType = data.getString("pack_type", string.Empty);
		cardPackKeyName = data.getString("card_pack_key", string.Empty);
		image = data.getString("image", string.Empty);
	}
}
