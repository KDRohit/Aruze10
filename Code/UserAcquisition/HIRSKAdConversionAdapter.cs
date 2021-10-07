using System;
using System.Threading.Tasks;
using UnityEngine;
using Zynga.Core.Platform;

namespace Zynga.Metrics.UserAcquisition
{
	public delegate void AppTrackingConsentFunc(string attAuthStatus, string attSurface);

	public class HIRSKAdConversionAdapter : SKAdConversionValueAdapterBase, IResetGame
	{

		private TaskCompletionSource<ConversionConfiguration> configTask = null;
		private TaskCompletionSource<ConversionResult> resultTask = null;
		private AppTrackingConsentFunc consentFunc = null;

		public static HIRSKAdConversionAdapter instance { get; private set; }

		private static Version getIOSVersion(string operatingSystem)
		{
			if (operatingSystem == null)
			{
				return null;
			}

			try
			{
				string[] parseResult = operatingSystem.Split(' ');
				if (parseResult[0] == "iOS" && parseResult.Length == 2)
				{
					return Version.Parse(parseResult[1]);
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"SKAdConversionValueAdapter getIOSVersion exception {e.Message}");
			}

			return null;
		}

		public void logIosAppTrackingTransparencyConsent(string attAuthStatus, string attSurface)
		{
			if (consentFunc != null)
			{
				consentFunc(attAuthStatus, attSurface);
			}
		}

		public HIRSKAdConversionAdapter(AppTrackingConsentFunc appConsentFunc)
		{
			if (instance == null)
			{
				instance = this;
				consentFunc = appConsentFunc;
				Server.registerEventDelegate("ios_conversion_registration", onAdTrackingConfiguration, true);
				Server.registerEventDelegate("ios_conversion_value", onConversionValue, true);	
			}
		}

		~HIRSKAdConversionAdapter()
		{
			Server.unregisterEventDelegate("ios_conversion_registration", onAdTrackingConfiguration, true);
			Server.unregisterEventDelegate("ios_conversion_value", onConversionValue, true);
		}

		private void onAdTrackingConfiguration(JSON data)
		{
			if (configTask == null)
			{
				//no request was made
				return;
			}
			
			JSON variantData = data.getJSON("variant");

			if (variantData != null)
			{
				ConversionConfiguration config = new ConversionConfiguration();
				config.UseZyngaCv = variantData.getBool("use_zynga_cv", true);
				config.ReservedErrorValue = variantData.getInt("reserved_error_value", 0);
				int maxUpdateInterval = variantData.getInt("max_cv_update_time_sec", 86400);
				config.MaxCvUpdateTimeSec = new TimeSpan(0,0,maxUpdateInterval);
				int updateInterval = variantData.getInt("cv_poll_interval_sec", 300);
				config.CvPollIntervalSec = new TimeSpan(0, 0, updateInterval);
				
				configTask.SetResult(config);
			}
			else
			{
				configTask.SetResult(null);
			}
			
			
			//set reference to null so we can accept a new request
			configTask = null;
		}

		private void onConversionValue(JSON data)
		{
			if (resultTask == null)
			{
				//no request was made
				return;
			}
			
			JSON variantData = data.getJSON("variant");
			if (variantData != null)
			{
				ConversionResult result = new ConversionResult();
				result.ConversionValue = variantData.getInt("conversion_value", 0);
				
				//set result for any task that is waiting for it
				resultTask.SetResult(result);
				
			}
			else
			{
				resultTask.SetResult(null);
			}
			
			//set reference to null so we can accept a new request
			resultTask = null;
		}
		
		/// <inheritdoc/>
		public override Task<ConversionConfiguration> GetConfigurationAsync()
		{
			if (configTask == null)
			{
				configTask = new TaskCompletionSource<ConversionConfiguration>();
				Version iosVersion = getIOSVersion(DeviceInfo.OperatingSystem);
				IosAdAction.getConfig(iosVersion);
			}

			return configTask.Task;
		}

		/// <inheritdoc/>
		public override Task<ConversionResult> GetConversionValueAsync()
		{
			if (resultTask == null)
			{
				resultTask = new TaskCompletionSource<ConversionResult>();
				Version iosVersion = getIOSVersion(DeviceInfo.OperatingSystem);
				IosAdAction.getConversionValue(iosVersion);
			}

			return resultTask.Task;
		}
		
		public static void resetStaticClassData()
		{
			instance = null;
		}
	}
}