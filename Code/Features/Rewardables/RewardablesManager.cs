using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Zynga.Core.Util;

namespace Com.Rewardables
{
	public class RewardablesManager : IResetGame
	{
		private class BatchCallbackData
		{
			public string keyField;
			public HashSet<OnBatchRewardDelegate> callbacks;

			public BatchCallbackData(string key)
			{
				keyField = key;
				callbacks = new HashSet<OnBatchRewardDelegate>();
			}
		}
		private static readonly List<Rewardable> pendingRewards = new List<Rewardable>();
		private static readonly List<Rewardable> consumedRewards = new List<Rewardable>();

		public delegate void OnBatchRewardDelegate(string id, List<Rewardable> rewardables);

		private static readonly Dictionary<string, BatchCallbackData> batchRewardCallbacks = new Dictionary<string, BatchCallbackData>();
		

		// Reward grant event/delegate
		public delegate void OnRewardDelegate(Rewardable rewardable);
		private static event OnRewardDelegate onRewardEvent;
		

		// Reward grant failed event/delegate
		public delegate void OnRewardFailedDelegate();
		private static event OnRewardFailedDelegate onRewardFailedEvent;

		private static readonly Dictionary<string, Type> REWARDABLES_MAP = new Dictionary<string, Type>();
		

		/// <summary>
		/// Called from FeatureInit
		/// </summary>
		public static void init()
		{
			populateMap();
			registerEvents();
		}

		/// <summary>
		/// Populates the map of rewardable types
		/// </summary>
		public static void populateMap()
		{
			foreach(Assembly asm in ReflectionHelper.GameCodeAssemblies)
			{
				Type[] types = asm.GetTypes();
				foreach (Type type in types)
				{
					//ignore structs and other value data types -- faster check then is assignable
					if (!type.IsClass() || type.IsAbstract)
					{
						continue;
					}
					
					if (typeof(Rewardable).IsAssignableFrom(type))
					{
						Rewardable r = Activator.CreateInstance(type) as Rewardable;
						REWARDABLES_MAP.Add(r.type, type);
					}
				}
			}
		}

		/// <summary>
		/// Register for server events
		/// </summary>
		private static void registerEvents()
		{
			Server.registerEventDelegate("reward_granted", onRewardGranted, true);
			Server.registerEventDelegate("rp_reward_granted", onRPRewardGranted, true);
			Server.registerEventDelegate("reward_grant_failed", onRewardGrantFailed, true);
			Server.registerEventDelegate("rp_reward_grant_failed", onRewardGrantFailed, true);
			Server.registerEventDelegate("rp_repeatable_reward_granted", onRPRewardGranted, true);
			Server.registerEventDelegate("rp_repeatable_reward_grant_failed", onRewardGrantFailed, true);
		}

		/// <summary>
		/// Remove server event handling
		/// </summary>
		private static void unregisterEvents()
		{
			Server.unregisterEventDelegate("reward_granted", onRewardGranted, true);
			Server.unregisterEventDelegate("rp_reward_granted", onRPRewardGranted, true);
			Server.unregisterEventDelegate("reward_grant_failed", onRewardGrantFailed, true);
			Server.unregisterEventDelegate("rp_reward_grant_failed", onRewardGrantFailed, true);
			Server.unregisterEventDelegate("rp_repeatable_reward_granted", onRPRewardGranted, true);
			Server.unregisterEventDelegate("rp_repeatable_reward_grant_failed", onRewardGrantFailed, true);
		}

		/// <summary>
		/// Add a method to the reward created event
		/// </summary>
		/// <param name="handler"></param>
		public static void addEventHandler(OnRewardDelegate handler)
		{
			onRewardEvent -= handler;
			onRewardEvent += handler;
		}

		

		/// <summary>
		/// Remove a method from the reward created event
		/// </summary>
		/// <param name="handler"></param>
		public static void removeEventHandler(OnRewardDelegate handler)
		{
			onRewardEvent -= handler;
		}
		
		/// <summary>
		/// Add a method to the reward created event
		/// </summary>
		/// <param name="handler"></param>
		public static void addBatchEventHandler(string rewardType, string groupBy, OnBatchRewardDelegate handler)
		{
			BatchCallbackData data = null;
			if (!batchRewardCallbacks.TryGetValue(rewardType, out data))
			{
				data = new BatchCallbackData(groupBy);
				batchRewardCallbacks.Add(rewardType, data);
			}
			else if (data.keyField != groupBy)
			{
				Debug.LogError("Cannot group field by new value");
			}

			if (!data.callbacks.Contains(handler))
			{
				data.callbacks.Add(handler);
			}
		}

		/// <summary>
		/// Remove a method from the reward created event
		/// </summary>
		/// <param name="handler"></param>
		public static void removeBatchEventHandler(string rewardType, OnBatchRewardDelegate handler)
		{
			BatchCallbackData data = null;
			if (batchRewardCallbacks.TryGetValue(rewardType, out data))
			{
				data.callbacks.Remove(handler);
				if (data.callbacks.IsEmpty())
				{
					batchRewardCallbacks.Remove(rewardType);
				}
			}
		}

		/// <summary>
		/// Add a method to the reward grant failed event
		/// </summary>
		/// <param name="handler"></param>
		public static void addFailEventHandler(OnRewardFailedDelegate handler)
		{
			onRewardFailedEvent -= handler;
			onRewardFailedEvent += handler;
		}

		/// <summary>
		/// Remove a method from the reward grant failed event
		/// </summary>
		/// <param name="handler"></param>
		public static void removeFailEventHandler(OnRewardFailedDelegate handler)
		{
			onRewardFailedEvent -= handler;
		}

		private static void dispatchRewardEvent(Rewardable rewardable)
		{
			if (onRewardEvent != null)
			{
				onRewardEvent(rewardable);
			}
		}
		private static void dispatchRewardFailedEvent()
		{
			if (onRewardFailedEvent != null)
			{
				onRewardFailedEvent();
			}
		}
		

		/// <summary>
		/// Handles the reward grant event
		/// </summary>
		/// <param name="data"></param>
		public static void onRewardGranted(JSON data)
		{
			JSON grantedData = data.getJSON("grant_data");
			if (grantedData == null)
			{
				return;
			}

			JSON[] rewardables = grantedData.getJsonArray("rewardables");
			List<Rewardable> batchList = new List<Rewardable>();
			if (rewardables != null && rewardables.Length > 0)
			{
				string type = grantedData.getString("reward_type", "");
				bool batchEvents = batchRewardCallbacks.ContainsKey(type);
				
				for (int i = 0; i < rewardables.Length; ++i)
				{
					string rewardType = rewardables[i].getString("reward_type", "");

					if (!string.IsNullOrEmpty(rewardType))
					{
						Rewardable rewardable = createRewardFromType(rewardType, rewardables[i]);
						if (rewardable != null)
						{
							if (batchEvents)
							{
								batchList.Add(rewardable);	
							}
							else
							{
								dispatchRewardEvent(rewardable);	
							}
						}
					}
				}
				
				BatchCallbackData batchData = null;
				if (batchRewardCallbacks.TryGetValue(type, out batchData))
				{
					string key = grantedData.getString(batchData.keyField, "");
					foreach (OnBatchRewardDelegate func in batchData.callbacks)
					{
						if (func != null)
						{
							func.Invoke(key, batchList);
						}
					}
				}
			}
			else
			{
				string rewardType = grantedData.getString("reward_type", "");

				Rewardable rewardable = createRewardFromType(rewardType, grantedData);

				if (rewardable != null)
				{
					dispatchRewardEvent(rewardable);
				}
			}
		}

		/// <summary>
		/// Handles reward failure
		/// </summary>
		/// <param name="data"></param>
		public static void onRewardGrantFailed(JSON data)
		{
			string error = data.getString("error_msg", data.getString("message", ""));
			if (!string.IsNullOrEmpty(error))
			{
				Debug.LogError(error);
			}

			dispatchRewardFailedEvent();
		}

		/// <summary>
		/// RICH PASS SPECIFIC, SHOULD BE DEPRECATED TO USE REWARD_GRANTED EVENT !! DO NOT EXTEND THIS FUNCTIONALITY !!
		/// </summary>
		/// <param name="data"></param>
		public static void onRPRewardGranted(JSON data)
		{
			if (data == null)
			{
				Debug.LogWarning("Invalid reward");
				return;
			}
			
			JSON grantedData = data.getJSON("grant_data");
			if (grantedData == null)
			{
				return;
			}

			RewardRichPass reward = new RewardRichPass();
			reward.init(data);

			dispatchRewardEvent(reward);

			// just going to consume this right away because I don't see a central way this is currently handled for rich pass
			reward.consume();
		}

		/// <summary>
		/// Creates, initializes, and adds a rewardable to the pending rewards list
		/// </summary>
		/// <param name="rewardType">Server driven type value</param>
		/// <param name="rewardData">Data associated with the reward, needed for Rewardable.init()</param>
		/// <returns></returns>
		public static Rewardable createRewardFromType(string rewardType, JSON rewardData)
		{
			Rewardable rewardable = null;
			if (REWARDABLES_MAP.ContainsKey(rewardType))
			{
				rewardable = (Rewardable)Activator.CreateInstance(REWARDABLES_MAP[rewardType]);

				if (rewardable != null)
				{
					pendingRewards.Add(rewardable);
					rewardable.init(rewardData);
				}
				else
				{
					Debug.LogError("RewardablesManager: No class associated with: " + rewardType);
				}
			}
			else
			{
				Debug.LogError("RewardablesManager: Unable to handle reward type: " + rewardType);
			}

			return rewardable;
		}

		/// <summary>
		/// Called from Rewardable classes once they have been used/activated/consumed
		/// </summary>
		/// <param name="rewardable"></param>
		internal static void consumeReward(Rewardable rewardable)
		{
			pendingRewards.Remove(rewardable);
			consumedRewards.Add(rewardable);
		}

		// Implements IResetGame
		public static void resetStaticClassData()
		{
			pendingRewards.Clear();
			consumedRewards.Clear();
			REWARDABLES_MAP.Clear();
			unregisterEvents();
			onRewardEvent = null;
			onRewardFailedEvent = null;
			batchRewardCallbacks.Clear();
		}
	}
}