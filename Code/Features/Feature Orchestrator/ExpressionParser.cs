using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ExpressionParser
	{
		private FeatureConfig featureConfig;
		private ProvidableObjectConfig providableObjectConfig;
		private Dictionary<string, object> payload;
		private Dictionary<string, object> replaceObjects;

		public ExpressionParser(FeatureConfig config, ProvidableObjectConfig configToParse)
		{
			featureConfig = config;
			replaceObjects = new Dictionary<string, object>();
			providableObjectConfig = configToParse;
		}
		
		public void parse(ProvidableObject objectToParse, Dictionary<string, object> payload = null)
		{
			this.payload = payload;
			if (providableObjectConfig.properties == null)
			{
				return;
			}
			
			foreach (KeyValuePair<string, object> property in providableObjectConfig.properties)
			{
				if (property.Value is string stringToParse)
				{
					object replaceObj = getReplacementObject(property.Key, stringToParse, getExpressions(stringToParse));
					if (replaceObj != null)
					{
						replaceObjects.Add(property.Key, replaceObj);
					}
				}
				else if (property.Value is List<object> propsArray)
				{
					object[] replacementObjects = new object[propsArray.Count];
					for (int i = 0; i < propsArray.Count; i++)
					{
						if (propsArray[i] is string valueString)
						{
							object replaceObj = getReplacementObject(property.Key, valueString, getExpressions(valueString));
							if (replaceObj != null)
							{
								replacementObjects[i] = replaceObj;
							}
							else
							{
								//Put original string back in if it didn't have any values to parse out of it
								replacementObjects[i] = valueString;
							}
						}
						else
						{
							//If its not a string then keep just add the item as-is
							replacementObjects[i] = propsArray[i];
						}
					}

					replaceObjects.Add(property.Key, replacementObjects);
				}
			}

			foreach (KeyValuePair<string, object> kvp in replaceObjects)
			{
				objectToParse.jsonData.jsonDict[kvp.Key] = kvp.Value;
			}
		}

		//An example of an expression  "Reach level <#FFF000>{xpProgress.completeLevel}</color> to play!"
		//Or it can also contain more substtings to be parsed, somethig like
		//"Starting at Level - {xpProgress.startingLevel}\nEnding at Level - {xpProgress.completeLevel}"
		private List<string> getExpressions(string source)
		{
			List<string> matches = new List<string>();
			for (int i = 0, j=0; i < source.Length; i++)
			{
				if (source[i] == '{')
				{
					
					j = i + 1;
					while (j < source.Length && source[j] != '}')
					{
						j++;
					}
					
					
					string match = source.Substring(i+1, j - i - 1);
					matches.Add(match);
				}
			}

			return matches;
		}

		private object getReplacementObject(string sourceKey, string sourceString, List<string> matches)
		{
			foreach (string match in matches)
			{
				string[] words = match.Split(new[] {'.'});
				if (words.Length <= 0)
				{
					continue;
				}

				if (words[0] == "payload")
				{
					if (payload == null)
					{
						Debug.LogError("Missing payload data");
						continue;
					}

					if (words.Length == 1)
					{
						return payload;
					}
					else if (words.Length == 2)
					{
						payload.TryGetValue(words[1], out object val);
						if (val != null)
						{
							return val;
						}
					}
				}
				else
				{
					//Expect a dataObject
					ProvidableObjectConfig config = featureConfig.getDataObjectConfigForKey(words[0]);
					if (config == null)
					{
						continue;
					}

					BaseDataObject dataObject = featureConfig.getServerDataProvider().provide(featureConfig, config, null) as BaseDataObject;
					if (dataObject == null)
					{
						continue;
					}

					if (words.Length == 1)
					{
						return dataObject;
					}
					else if (words.Length == 2) //Currently supporting accessing 1 property eg. xpProgress.targetValue
					{
						if (dataObject.jsonData == null || dataObject.jsonData.jsonDict == null)
						{
							continue;
						}
						
						if (dataObject.tryReplaceString(words[1], out string result))
						{
							string newString = sourceString.Replace("{" + words[0] + "." + words[1] + "}", result);
							return newString;
						}
					}
				}
			}

			return null;
		}
	}
}
