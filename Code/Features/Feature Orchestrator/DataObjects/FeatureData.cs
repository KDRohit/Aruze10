using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class FeatureData : BaseDataObject
	{
		public long seedValueFree { get; private set; }
		public long seedValuePremium { get; private set; }
		public long seedValue { get; private set; }

		public string variant { get; private set; }
		
		public FeatureData(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override void updateValue(JSON json)
		{
			if (json == null)
			{
				return;
			}

			// set all properties from the config. 
			//properties = props;
			jsonData = json;

			seedValueFree = json.getLong("seedValueFreeBG", 0);
			seedValuePremium = json.getLong("seedValueDeluxeBG", 0);
			variant = json.getString("variant", "");
			seedValue = json.getLong("seed_value", 0);
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new FeatureData(keyname, json);
		}
	}
}
