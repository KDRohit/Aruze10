using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	/*
	 * Proton data class responsible for EOS data.
	 * Allows a config to define a EosVariables object so eos data is accessible by other objects in the same config
	 */
	public class EosVariables : BaseDataObject
	{
		private Dictionary<string, object> eosVariablesMap = new Dictionary<string, object>();
		public EosVariables(string keyName, JSON json) : base(keyName, json)
		{
			string experimentName = json.getString("experiment", "");
			experimentName = experimentName.Substring(ExperimentWrapper.HIR_EXPERIMENTS_PREFIX.Length);
			EosExperiment experiment = ExperimentManager.GetEosExperiment(experimentName);

			if (experiment != null)
			{
				System.Type expType = experiment.GetType();
				System.Reflection.PropertyInfo[] properties = expType.GetProperties();
				for (int i = 0; i < properties.Length; i++)
				{
					EosAttribute attribute = System.Attribute.GetCustomAttribute(properties[i], typeof(EosAttribute), true) as EosAttribute;
					if (attribute != null)
					{
						object obj = properties[i].GetValue(experiment);
						eosVariablesMap.Add(attribute.name, obj);
					}
				}
			}
		}
		
		//EOS Variables don't change post-login currently but updateValue has to be implemented because its abstract
		public override void updateValue(JSON json)
		{
#if !ZYNGA_PRODUCTION
			Debug.LogError("EOS Variables don't currently support updating post-login");
#endif
		}

		public override bool tryReplaceString(string propertyKey, out string result)
		{
			if (eosVariablesMap.TryGetValue(propertyKey, out object obj))
			{
				result = System.Convert.ToString(obj);
				return true;
			}
			result = "";
			return false;
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new EosVariables(keyname, json);
		}
	}
}
