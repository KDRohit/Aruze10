using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using Zynga.Core.Util;

namespace FeatureOrchestrator
{
	
	public class FeatureConfig
	{
		public Dictionary<string, ProviderConfig> providerConfigs;
		public Dictionary<string, DataObjectConfig> dataObjectConfigs;
		public Dictionary<string, ComponentConfig> componentConfigs;
		
		private SessionCacheProvider sessionCacheProvider;
		private ServerDataProvider serverDataProvider;

		public FeatureConfig(List<ComponentConfig> componentConfigsList, List<DataObjectConfig> dataObjectConfigsList, List<ProviderConfig> providerConfigsList)
		{
			componentConfigs = new Dictionary<string, ComponentConfig>();
			dataObjectConfigs = new Dictionary<string, DataObjectConfig>();
			providerConfigs = new Dictionary<string, ProviderConfig>();

			populateConfigDict (providerConfigsList, providerConfigs);
			populateConfigDict (dataObjectConfigsList, dataObjectConfigs);
			populateConfigDict (componentConfigsList, componentConfigs);
		}

		private void populateConfigDict<T> (List<T> sourceList, Dictionary<string, T> dict) where T : BaseConfig
		{
			foreach (T config in sourceList)
			{
				if (string.IsNullOrEmpty(config.keyName))
				{
					Debug.LogError("Empty keyname found in feature Config");
					continue;
				}
				dict.Add(config.keyName, config);
			}
		}

		public ComponentConfig getComponentConfigForKey(string keyName)
		{
			if (componentConfigs == null)
			{
				return null;
			}

			ComponentConfig config;
			if (componentConfigs.TryGetValue(keyName, out config))
			{
				return config;
			}

			return null;
		}
		
		public DataObjectConfig getDataObjectConfigForKey(string keyName)
		{
			if (dataObjectConfigs == null)
			{
				return null;
			}

			DataObjectConfig config;
			if (dataObjectConfigs.TryGetValue(keyName, out config))
			{
				return config;
			}

			return null;
		}
		
		public ProviderConfig getProviderConfigForKey(string keyName)
		{
			if (providerConfigs == null)
			{
				return null;
			}

			ProviderConfig config;
			if (providerConfigs.TryGetValue(keyName, out config))
			{
				return config;
			}

			return null;
		}

		public string[] getOutputsForComponent(string keyName, string outString)
		{
			ComponentConfig cConfig = getComponentConfigForKey(keyName);
			if (cConfig != null && cConfig.outs != null)
			{
				return cConfig.outs.getStringArray(outString);
			}

			return null;
		}
		
		public IProvider getComponentProvider()
		{
			if (sessionCacheProvider == null)
			{
				sessionCacheProvider = new SessionCacheProvider();
			}

			return sessionCacheProvider;
		}

		public IProvider getServerDataProvider()
		{
			if (serverDataProvider == null)
			{
				serverDataProvider = new ServerDataProvider();
			}

			return serverDataProvider;
		}
		
	}
}
