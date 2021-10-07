using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Metrics.UserAcquisition;
using System.Threading.Tasks;
using Com.HitItRich.IDFA;

public class DevGUIMenuUserAcquisition : DevGUIMenu
{
	private static HIRSKAdConversionAdapter debugAdapter = null;
	private static Task<SKAdConversionValueAdapterBase.ConversionConfiguration> configTask = null;
	private static Task<SKAdConversionValueAdapterBase.ConversionResult> resultTask = null;



	public override void drawGuts()
	{

		GUILayout.BeginVertical();
		if (HIRSKAdConversionAdapter.instance == null && debugAdapter == null)
		{
			if (GUILayout.Button("Instantiate Debug Adapter"))
			{
				debugAdapter = new HIRSKAdConversionAdapter(null);
			}
		}
		else
		{
			HIRSKAdConversionAdapter adapter = HIRSKAdConversionAdapter.instance == null
				? debugAdapter
				: HIRSKAdConversionAdapter.instance;
			
			if (GUILayout.Button("iOS Ad Tracking Request"))
			{
				iOSAppTracking.RequestTracking(IDFASoftPromptManager.SurfacePoint.GameEntry, () => { });
			}
			if (configTask == null)
			{
				if (GUILayout.Button("Get Config"))
				{
					configTask = adapter.GetConfigurationAsync();
				}
			}
			else if (configTask.IsFaulted || configTask.IsCanceled)
			{
				GUILayout.Label("Task failed");
				if (GUILayout.Button("reset config task"))
				{
					configTask = null;
				}
			}
			else if (!configTask.IsCompleted)
			{
				GUILayout.Label("Config task in progress... " + configTask.Status.ToString());
			}
			else
			{
				GUILayout.Label("Reserved Error Value: " + configTask.Result.ReservedErrorValue);
				GUILayout.Label("Use Zynga CV: " + configTask.Result.UseZyngaCv);
				GUILayout.Label("CV Poll Interval: " + configTask.Result.CvPollIntervalSec.ToString());
				GUILayout.Label("Max Update Time: " + configTask.Result.MaxCvUpdateTimeSec.ToString());
				if (GUILayout.Button("reset config task"))
				{
					configTask = null;
				}
			}
			
			if (resultTask == null)
			{
				if (GUILayout.Button("Get Result"))
				{
					resultTask = adapter.GetConversionValueAsync();
				}
			}
			else if (resultTask.IsFaulted || resultTask.IsCanceled)
			{
				GUILayout.Label("Task failed");
				if (GUILayout.Button("reset result task"))
				{
					resultTask = null;
				}
			}
			else if (!resultTask.IsCompleted)
			{
				GUILayout.Label("Result task in progress... " + resultTask.Status.ToString());
			}
			else
			{
				GUILayout.Label("Conversion Value: " + resultTask.Result.ConversionValue);
				if (GUILayout.Button("reset result task"))
				{
					resultTask = null;
				}
			}

			if (debugAdapter != null)
			{
				if (GUILayout.Button("Destroy debug adapter"))
				{
					debugAdapter = null;
				}
			}
		}
		
		GUILayout.EndVertical();
	}
}
