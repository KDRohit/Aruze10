using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class HideLevelLottoUIPrefab : BaseComponent
	{
		public HideLevelLottoUIPrefab(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			
			OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
			if (overlay != null)
			{
				bool playAnim = false;
				XPProgressCounter progress = jsonData.jsonDict["xpProgressData"] as XPProgressCounter;
				if (progress != null)
				{ 
					playAnim = progress.currentValue >= progress.completeValue;
				}
				overlay.removeGenericXPProgressBar(playAnim);
			}

			if (result == null)
			{
				result = new Dictionary<string, object>();	
			}
			
			result.Add("out", null);
			return result;
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new HideLevelLottoUIPrefab(keyname, json);
		}
	}
}
