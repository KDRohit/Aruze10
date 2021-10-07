using UnityEngine;
using System.Collections;
using TMPro;

/*
	Dialog for showing the Double Free Spins MOTD
*/
public class DoubleFreeSpinsMOTD : DialogBase
{
	private const string BACKGROUND_PATH_HIR = "double_free_spin/MOTD_doublefreespins_{0}.png";
	protected const string DIALOG_KEY = "doublespinvip";
	public ButtonHandler closeButton;
	public ButtonHandler playButton;

	protected string gameKey = "";
	
	void Update()
	{
		AndroidUtil.checkBackButton(checkBackButton);
	}

	public override void init()
	{
		playButton.registerEventDelegate(visitVIP);
		closeButton.registerEventDelegate(closeClicked);
		
		gameKey = dialogArgs.getWithDefault(D.GAME_KEY, "") as string;
		
		// Make sure the same game isn't shown on this dialog twice in a row.
		CustomPlayerData.setValue(CustomPlayerData.DOUBLE_FREE_SPINS_MOTD_LAST_SEEN, gameKey.GetHashCode());
	}

	public virtual void closeClicked(Dict args = null) 
	{
		Dialog.close();
	}

	public virtual void visitVIP(Dict args = null)
	{
		DoSomething.now("vip_room");
		Dialog.close();
	}

	public override void close()
	{
		// Do special cleanup.
	}

	public static bool showDialog(string gameKey, string motdKey = "")
	{
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey,
			D.GAME_KEY, gameKey
		);
		
		string backgroundImagePath = string.Format(BACKGROUND_PATH_HIR, gameKey);
		Dialog.instance.showDialogAfterDownloadingTextures(DIALOG_KEY, backgroundImagePath, args);
		return true;
	}

	// required for Unity/Mono compiler
	private void checkBackButton()
	{
		closeClicked();
	}

}
