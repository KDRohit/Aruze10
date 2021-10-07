using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zap.Automation
{
	/**
	 * Class made to handle default file handling across ZAP, to ensure that default file paths are always set so
	 * that ZAP will not just fail to work due to trying to use bogus file paths that are in the root of the machine.
	 *
	 * Creation Date: 11/26/2019
	 * Author: Scott Lepthien
	 */
	public class ZAPFileHandler
	{
		// Default values for some of the Prefs
		public const string DEFAULT_ZAP_SAVE_LOCATION = "../tools/ZAP/TestPlans/"; // default for ZAP_SAVE_LOCATION
		public const string DEFAULT_ZAP_RESULTS_LOCATION = "../tools/ZAP/Results/"; // default for ZAP_RESULTS_LOCATION

		public static string getZapSaveFileLocation()
		{
			string currentSaveLocation = SlotsPlayer.getPreferences().GetString(ZAPPrefs.ZAP_SAVE_LOCATION, "");

			// If they aren't set from reading the prefs, then we'll fill them in with defaults
			if (string.IsNullOrEmpty(currentSaveLocation))
			{
				currentSaveLocation = DEFAULT_ZAP_SAVE_LOCATION;
				CommonFileSystem.createDirectoryIfNotExisting(currentSaveLocation);
				SlotsPlayer.getPreferences().SetString(ZAPPrefs.ZAP_SAVE_LOCATION, currentSaveLocation);
			}
			
			return currentSaveLocation;
		}

		public static string getZapResultsFileLocation()
		{
			string currentResultsLocation = SlotsPlayer.getPreferences().GetString(ZAPPrefs.ZAP_RESULTS_LOCATION, "");
				
			// If they aren't set from reading the prefs, then we'll fill them in with defaults
			if (string.IsNullOrEmpty(currentResultsLocation))
			{
				currentResultsLocation = DEFAULT_ZAP_RESULTS_LOCATION;
				CommonFileSystem.createDirectoryIfNotExisting(currentResultsLocation);
				SlotsPlayer.getPreferences().SetString(ZAPPrefs.ZAP_RESULTS_LOCATION, currentResultsLocation);
			}
				
			return currentResultsLocation;
		}
	}
}
