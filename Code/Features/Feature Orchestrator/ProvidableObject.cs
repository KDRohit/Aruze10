using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

namespace FeatureOrchestrator
{
	//Base class for all compinents, dataobjects
	public abstract class ProvidableObject
	{
		public JSON jsonData;
		public string keyName;
		protected string featureName = "";
		
		private static bool log = true;
		
		protected const string FEATURE_NAME = "featureName";

		private static Dictionary<string, System.Type> typesDict;

		public ProvidableObject(string keyname, JSON json)
		{
			this.keyName = keyname;
			jsonData = json;
			featureName = json.getString(FEATURE_NAME, "");
		}
		
		public ProvidableObject()
		{
		}

		public static ProvidableObject createInstance(string keyname, string className, JSON json)
		{
			if (typesDict.TryGetValue(className, out System.Type type))
			{
				System.Reflection.MethodInfo method = type.GetMethod("createInstance");
				if (method != null)
				{
					ProvidableObject newInstance = (ProvidableObject) method.Invoke(null, new object[]{keyname, json});
					return newInstance;
				}

				typesDict.Remove(className); //This class has already failed. Remove from the dictionary so we don't bother trying to use it again later
				Debug.LogWarningFormat("{0} doesn't implement a createInstance function. Returning null", className);
			}

			return null;
		}

		public static void buildMapping()
		{
			if (typesDict != null)
			{
				Debug.LogWarning("Mapping is already built. This function shouldn't be called multiple times");
				return;
			}
			
			typesDict = new Dictionary<string, System.Type>();
			System.Reflection.Assembly asm = ReflectionHelper.GetAssemblyByName(ReflectionHelper.ASSEMBLY_NAME_MAP[ReflectionHelper.ASSEMBLIES.PRIMARY]);

			if (asm != null)
			{
				foreach (System.Type type in asm.GetTypes())
				{
					if (type.IsSubclassOf(typeof(ProvidableObject)))
					{
						typesDict[type.Name] = type;
					}
				}
			}
		}
	}
}
