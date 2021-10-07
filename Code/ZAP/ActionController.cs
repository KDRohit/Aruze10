using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	public static class ActionController
	{
		private static Dictionary<string, JObject> values;
		private static Dictionary<string, Action> actions;

		public static void deserialize()
		{
			values = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(File.ReadAllText(@"Assets/Code/ZAP/Actions.json"));
			actions = new Dictionary<string, Action>();

			foreach (KeyValuePair<string, JObject> entry in values)
			{
				switch (entry.Value["action"].ToString())
				{
					case "click":
						NGUIButtonClickAction click = JsonConvert.DeserializeObject<NGUIButtonClickAction>(entry.Value.ToString());
						actions.Add(entry.Key, click);
						break;
					case "randomButton":
						RandomNGUIButtonClickAction randomButton = JsonConvert.DeserializeObject<RandomNGUIButtonClickAction>(entry.Value.ToString());
						actions.Add(entry.Key, randomButton);
						break;						
					case "keyPress":
						KeyPressAction keyPress = JsonConvert.DeserializeObject<KeyPressAction>(entry.Value.ToString());
						actions.Add(entry.Key, keyPress);
						break;
					case "randomClick":
						RandomClickAction randomClick = JsonConvert.DeserializeObject<RandomClickAction>(entry.Value.ToString());
						actions.Add(entry.Key, randomClick);
						break;
				}
			}
		}

		public static IEnumerator doAction(string key, params GameObject[] args)
		{
			Debug.LogFormat("ZAPLOG -- doing action : {0}", key);
			yield return RoutineRunner.instance.StartCoroutine(actions[key].doAction(args));
		}

		public static IEnumerator doAction(Action action, params GameObject[] args)
		{
			Debug.LogFormat("ZAPLOG -- doing action with key: {0}", action.name);
			yield return RoutineRunner.instance.StartCoroutine(action.doAction(args));
		}

		public static Action getAction(string key)
		{
			if (actions.ContainsKey(key))
			{
				return actions[key];
			}
			else
			{
				Debug.LogErrorFormat("ZAPLOG -- ActionController.cs -- getAction() -- failed to find registered action for key: {0}", key);
				return null;
			}

		}

		public static void addDynamicButton(string key, string buttonId)
		{
			if (actions.ContainsKey(key))
			{
				throw new System.Exception(string.Format("ZAPLOG -- Trying to register an action: {0} that is already registerd.", key));
			}
			NGUIButtonClickAction click = new NGUIButtonClickAction(buttonId);
			actions.Add(key, click);
		}

		public static void removeDynamicButton(string key)
		{
			if (actions.ContainsKey(key))
			{
				actions.Remove(key);
			}
		}
	}
#endif
}
