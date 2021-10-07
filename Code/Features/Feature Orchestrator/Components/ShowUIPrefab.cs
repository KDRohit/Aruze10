using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ShowUIPrefab : BaseComponent
	{
		protected string location = "";
		protected string prefabPath = "";
		protected Dict args;
		
		protected const string LOCATION = "location";
		protected const string PREFAB = "prefab";
		protected const string PROGRESS_TEXT = "progressText";
		protected const string DURATION_TEXT = "durationText";
		
		public ShowUIPrefab(string keyName, JSON json) : base(keyName, json)
		{
		}

		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			location = jsonData.getString(LOCATION, "");
			prefabPath = jsonData.getString(PREFAB, "");
			setupDialogArgs();
			AssetBundleManager.load(prefabPath, onLoadSuccess, onLoadFail, args);
			
			return result;
		}

		protected virtual void setupDialogArgs()
		{
			string progressText = jsonData.getString(PROGRESS_TEXT, "");
			string durationText = jsonData.getString(DURATION_TEXT, "");

			args = Dict.create(D.MESSAGE, progressText, D.TIME, durationText);
		}

		private void onLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			if (location == "TopOverlay")
			{
				OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
				if (overlay != null)
				{
					GameObject progressObject = obj as GameObject;
					GameObject instance = overlay.addGenericXPProgressBar(progressObject);
					GenericProgressComponentView componentView = instance.GetComponent<GenericProgressComponentView>();
					componentView.setup(this, data);
				}
			}
		}

		private void onLoadFail(string assetPath, Dict data = null)
		{
			Debug.LogError("Failed to load " + assetPath);
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new ShowUIPrefab(keyname, json);
		}
	}
}