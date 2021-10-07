using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class CloseDialogsComponent : BaseComponent
	{
		private string[] dialogsToClose;
		public CloseDialogsComponent(string keyName, JSON json) : base(keyName, json)
		{
			dialogsToClose = json.getStringArray("dialogKeys");
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			List<DialogBase> openDialogs = Dialog.instance.findOpenDialogsOfTypes(new HashSet<string>(dialogsToClose));

			if (openDialogs != null)
			{
				for (int i = 0; i < openDialogs.Count; i++)
				{
					Dialog.close(openDialogs[i]);
				}
			}

			return result;
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new CloseDialogsComponent(keyname, json);
		}
	}
}