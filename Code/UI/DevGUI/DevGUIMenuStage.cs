using UnityEngine;
using System;
using Zynga.Zdk.Services.Track;
using System.Threading.Tasks;
using System.Collections.Generic;


class DevGUIMenuStage : DevGUIMenu
{
	private static string stageName = "";
	private static string skuKey = "";
	private static string stage = "";
	private	Task<AnalyticsFlowResult> result = null;
	BatchedTrackServiceBase batcher = null;
	private string[] configFiles;
	
	public override void drawGuts()
	{	
		GUILayout.Label("Current appID : " + ZdkManager.Instance.AppId);		
		GUILayout.Label("Current fbAppID : " + ZdkManager.Instance.FbAppId);		

		if (batcher == null && Packages.Track != null)
		{
			batcher = Packages.Track.BatchedService;
		}
		
		if (GUILayout.Button("Do GetSandboxAndAnalyticsFlow request"))
		{
			if (batcher != null)
			{
				result = null;
				result = batcher.GetSandboxAndAnalyticsFlow(ZdkManager.Instance.AppId);
			}		
		}

		if (result != null)
		{
			GUILayout.Label("GetSandboxAndAnalyticsFlow isSandbox : " + result.Result.IsSandbox);		
			GUILayout.Label("GetSandboxAndAnalyticsFlow anaylyticsFlow value : " + result.Result.AnalyticsFlow);		
		}
		else
		{
			GUILayout.Label("No result from GetSandboxAndAnalyticsFlow request, push button to get one");
		}		
			
		GUILayout.Label("Current cofig file data at path : " + Data.getConfigFilePath());
		if (Data.configJSON == null)
		{
			GUILayout.Label("Data.configJSON is NULL. Not a good place to be.");
		}
		else
		{
			GUILayout.TextArea(Data.configJSON.ToString());
		}

#if !ZYNGA_PRODUCTION
		if (String.IsNullOrEmpty(SharedConfig.currentConfigName))
		{
			GUILayout.Label("Shared config JSON is not active");
		}
		else
		{
			GUILayout.Label("Current shared config JSON for : " + SharedConfig.currentConfigName);
			if (SharedConfig.configJSON == null)
			{
				GUILayout.Label("SharedConfig.currentConfig JSON data is NULL");
			
			}
			else
			{
				GUILayout.TextArea(SharedConfig.configJSON.ToString());
			}		
		}
		
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Stage Switcher");
		GUILayout.EndHorizontal();

		if (configFiles == null)
		{
			configFiles = getConfigFiles();
		}

		int buttonsPerRow = 3;
		int buttonWidth = (int)(DevGUI.windowRect.width - 60) / buttonsPerRow;

		GUILayout.BeginHorizontal();
		for (int i = 0; i < configFiles.Length; i++)
		{
			string configName = configFiles[i].Split('.')[0];

			if (i % buttonsPerRow == 0 &&
			    i > 0 &&
			    i < configFiles.Length-1)
			{
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}
			
			if (GUILayout.Button(configName, GUILayout.Width(buttonWidth)))
			{
				SharedConfig.create(configName);
			}

		}
		GUILayout.EndHorizontal();
#endif
	}

	private string[] getConfigFiles()
	{
		TextAsset[] configFiles = Resources.LoadAll<TextAsset>("Config");
		List<string> configNames = new List<string>();
		if (configFiles != null)
		{
			for (int i = 0; i < configFiles.Length; i++)
			{
				configNames.Add(configFiles[i].name);
			}
		}
		return configNames.ToArray();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}