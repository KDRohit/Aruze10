using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ShowVideoComponent : BaseComponent
	{
		private string videoUrl = "";
		private string action = "";
		private string actionButtonLabel = "";
		private string summaryImagePath = "";
		private bool autoPopped = false;
		private string statName = "";
		private string closeAction = "";
		public ShowVideoComponent(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			videoUrl = jsonData.getString("videoUrl", "");
			action = jsonData.getString("action", "");
			actionButtonLabel = jsonData.getString("actionButtonLabel", "");
			summaryImagePath = jsonData.getString("summaryImagePath", "");
			autoPopped = jsonData.getBool("autoPopped", false);
			statName = jsonData.getString("statName", "");
			closeAction = jsonData.getString("closeAction", "");

			VideoDialog.showDialog(
				videoUrl,
				action,
				actionButtonLabel, 
				summaryScreenImage: summaryImagePath, 
				autoPopped: autoPopped,
				statName:statName,
				closeAction:closeAction
			);
			return result;
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new ShowVideoComponent(keyname, json);
		}
	}
}