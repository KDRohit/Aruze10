using Com.Scheduler;
using UnityEngine;

namespace FeatureOrchestrator
{
	public class ShowBoardGameDialogComponent : ShowDialogComponent
	{
		private const string DEFAULT_THEME = "casino";
		private const string THEMED_BACKGROUND_PATH = "Features/Board Game/Themes/{0}/boardgame_dialog_background";

		private string activeTheme = "";
		public ShowBoardGameDialogComponent(string keyName, JSON json) : base(keyName, json)
		{
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new ShowBoardGameDialogComponent(keyname, json);
		}

		protected override void showDialog(string dialogKey, Dict args, SchedulerPriority.PriorityType priorityType)
		{
			onDownloadFailed = loadWithDefaultBg;
			base.showDialog(dialogKey, args, priorityType);
		}

		private void loadWithDefaultBg(string path, Dict failedArgs)
		{
			if (!path.Contains(DEFAULT_THEME))
			{
				bundledTextureUrls.Clear();
				bundledTextureUrls.Add(string.Format(THEMED_BACKGROUND_PATH, DEFAULT_THEME));
				onDownloadFailed = null;
				args[D.THEME] = activeTheme;
				base.showDialog(dialogKey, args, priorityType);
			}
		}

		protected override void setupDialogArgs()
		{
			base.setupDialogArgs();
			activeTheme = jsonData.getString("theme", "");
			if (string.IsNullOrEmpty(activeTheme))
			{
				activeTheme = DEFAULT_THEME;
			}
			
			bundledTextureUrls.Add(string.Format(THEMED_BACKGROUND_PATH, activeTheme));
			args[D.THEME] = activeTheme;
		}
	}
}