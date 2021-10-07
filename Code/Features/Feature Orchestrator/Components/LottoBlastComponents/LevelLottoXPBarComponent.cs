using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class LevelLottoXPBarComponent : ShowUIPrefab
	{
		public LevelLottoXPBarComponent(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			if (result == null)
			{
				result = new Dictionary<string, object>();
			}

			string fxPrefabPath = jsonData.getString("fxPrefab", "");
			if (!string.IsNullOrEmpty(fxPrefabPath))
			{
				AssetBundleManager.load(fxPrefabPath, onLoadSuccess, onLoadFail, args, isSkippingMapping: true, fileExtension:".prefab");
			}
			
			result.Add("out", null);
			return result;
		}

		protected override void setupDialogArgs()
		{
			base.setupDialogArgs();
			
			Timer timer = jsonData.jsonDict["durationKeyname"] as Timer;
			if (timer != null)
			{
				timer.durationTimer.registerFunction(onTimerExpired);
				args.merge(D.TIME, timer.durationTimer);
			}
			XPProgressCounter progress = jsonData.jsonDict["xpProgressData"] as XPProgressCounter;
			string progressTextSingle = jsonData.getString("progressTextSingle", "");
			args.merge(D.OPTION, progressTextSingle, D.OPTION1, progress);
		}

		private void onTimerExpired(Dict args = null, GameTimerRange sender = null)
		{
			Dictionary<string, object> payloadData = new Dictionary<string, object>();
			payloadData.Add("timerComplete", payload);
			FeatureConfig config = Orchestrator.instance.allFeatureConfigs[featureName];
			Orchestrator.instance.completePerform(config, payloadData, this, shouldLog);
		}

		private void onLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
			if (overlay != null)
			{
				GameObject fxObject = obj as GameObject;
				overlay.addLevelLottoFXObject(fxObject);
			}
		}
		
		private void onLoadFail(string assetPath, Dict data = null)
		{
			Debug.LogError("Failed to load " + assetPath);
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new LevelLottoXPBarComponent(keyname, json);
		}
	}
}
