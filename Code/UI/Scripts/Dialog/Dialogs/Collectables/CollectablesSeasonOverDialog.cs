using System.Collections;
using UnityEngine;

public class CollectablesSeasonOverDialog : DialogBase, IResetGame
{
	[SerializeField] private ButtonHandler closeHandler;
	[SerializeField] private ButtonHandler okayHandler;
	[SerializeField] private LabelWrapperComponent headerLabel;
	[SerializeField] private LabelWrapperComponent subHeaderLabel;
	[SerializeField] private Renderer logo;

	public static bool needsToShowDialog { get; private set; }
	private static string[] endedAlbums = null;

	public override void init()
	{
		downloadedTextureToRenderer(logo, 0);
		closeHandler.registerEventDelegate(closeClicked);
		okayHandler.registerEventDelegate(closeClicked);

		//This comes as an array but we only run one album at a time
		if (endedAlbums != null && endedAlbums.Length > 0)
		{
			headerLabel.text = Localize.text(endedAlbums[0] + "_ended_title");
			subHeaderLabel.text = Localize.text(endedAlbums[0] + "_ended_sub_header");
		}
		CollectablesAction.seasonEndSeen();
		MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, "collectables_end"));
	}

	public void closeClicked(Dict args = null)
	{
		Dialog.close();
	}

	public override void close()
	{

	}

	public static void setEndedAlbums(string[] albums)
	{
		needsToShowDialog = true;
		endedAlbums = albums;
	}

	// If we ever had multiple albums active at once, knowing what album we were going into would be nice. 
	public static bool showDialog()
	{
		if (!needsToShowDialog)
		{
			Debug.LogError("Can't show the Collectables over dialog because we haven't received a season end event");
			return false;
		}
		else if (endedAlbums != null && endedAlbums.Length > 0)
		{
			string imagePath = string.Format("Features/Collections/Albums/{0}/Collection Textures/Season Ending Logo", endedAlbums[0]);
			Dialog.instance.showDialogAfterDownloadingTextures("collectables_season_over", imagePath, isExplicitPath:true);
			return true;
		}
		else
		{
			Debug.LogError("Can't show the Collectables over dialog because we're missing which Album ended.");
			return false;
		}
	}

	public static void resetStaticClassData()
	{
		endedAlbums = null;
		needsToShowDialog = false;
	}
}
