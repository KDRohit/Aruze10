using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ShowDialogComponent : BaseComponent
	{
		protected Dict args = null;
		protected string dialogKey;
		protected SchedulerPriority.PriorityType priorityType;
		private float delay = 0f;
		private string featureName = "";
		protected List<string> nonBundledTextureUrls = new List<string>();
		protected List<string> bundledTextureUrls = new List<string>();
		protected AssetFailDelegate onDownloadFailed = null;
		private bool abortOnFail = false;

		protected const string DIALOG_KEY = "dialogKey";
		protected const string PRIORITY = "priority";
		protected const string DELAY = "delay";
		protected const string TITLE = "title";
		protected const string DESCRIPTION_TEXT = "descriptionText";
		
		public ShowDialogComponent(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);

			if (this.payload == null)
			{
				this.payload = new Dictionary<string, object>();
			}

			this.payload["gameKey"] = GameState.game != null ? GameState.game.keyName : "";
			setupDialogArgs();
			
			if (delay > 0)
			{
				RoutineRunner.instance.StartCoroutine(showDialogDelayed(args, delay));
			}
			else
			{
				showDialog(dialogKey, args, priorityType);	
			}

			return result;
		}

		private IEnumerator showDialogDelayed(Dict args = null, float delay = 0f)
		{
			yield return new WaitForSeconds(delay);
			showDialog(dialogKey, args, priorityType);
		}

		protected virtual void showDialog(string dialogKey, Dict args, SchedulerPriority.PriorityType priorityType)
		{
			if (!Scheduler.hasTaskWith(dialogKey))
			{
				if (nonBundledTextureUrls.Count == 0 && bundledTextureUrls.Count == 0)
				{
					Scheduler.addDialog(dialogKey, args, priorityType);
				}
				else
				{
					Dialog.instance.showDialogAfterDownloadingTextures(dialogKey, nonBundledTextureUrls.ToArray(), args, abortOnFail, priorityType, true, onDownloadFailed:onDownloadFailed, nonMappedBundledTextures:bundledTextureUrls.ToArray());
				}
			}
		}

		protected virtual void setupDialogArgs()
		{
			dialogKey = jsonData.getString( DIALOG_KEY, "");
			priorityType = (SchedulerPriority.PriorityType) jsonData.getInt(PRIORITY, 0);
			delay = jsonData.getFloat(DELAY, 0f);
			string title = jsonData.getString(TITLE, "");
			string description = jsonData.getString(DESCRIPTION_TEXT, "");
			bool useShroud = jsonData.getBool("useShroud", true);
			args = Dict.create(D.TITLE, title, D.MESSAGE, description, D.DATA, this, D.PAYLOAD, payload, D.SHROUD, useShroud);
			abortOnFail = jsonData.getBool("abortOnFail", false);
			if (jsonData.jsonDict.TryGetValue("downloadTextures", out object texturesArrayObj))
			{
				object[] downloadTextureUrls = (object[]) texturesArrayObj;
				for (int i = 0; i < downloadTextureUrls.Length; i++)
				{
					if (downloadTextureUrls[i] is string url)
					{
						bool isBundled = !string.IsNullOrEmpty(AssetBundleManager.getBundleNameForResource(url));
						if (isBundled)
						{
							bundledTextureUrls.Add(url);
						}
						else
						{
							nonBundledTextureUrls.Add(url);
						}
					}
				}
			}
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new ShowDialogComponent(keyname, json);
		}
	}
}