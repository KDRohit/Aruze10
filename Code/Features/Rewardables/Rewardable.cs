using UnityEngine;
using System.Collections;
using FeatureOrchestrator;

namespace Com.Rewardables
{
	public abstract class Rewardable : BaseDataObject
	{
		/// <summary>
		/// Server data associated with this rewardable
		/// </summary>
		public JSON data { get; protected set; }
		public string feature { get; protected set; }

		/// <summary>
		/// Reward type associated with this rewardable (server driven matched value)
		/// </summary>
		public abstract string type { get; }

		/// <summary>
		/// Sets the data
		/// </summary>
		/// <param name="data"></param>
		public virtual void init(JSON data)
		{
			this.data = data;
			this.feature = data.getString("feature_name", "");
		}

		/// <summary>
		/// Completes the reward process
		/// </summary>
		public virtual void consume()
		{
			RewardablesManager.consumeReward(this);
		}

		public Rewardable() : base()
		{
		}
		
		public override void updateValue(JSON json)
		{
			init(json);
		}
		
		public static Rewardable createInstance(string keyname, JSON json)
		{
			string type = json.getString("reward_type", "");
			Rewardable rewardable = RewardablesManager.createRewardFromType(type, json);
			rewardable.jsonData = json;
			rewardable.keyName = keyname;
			rewardable.featureName = json.getString(FEATURE_NAME, "");
			return rewardable;
		}
	}
}