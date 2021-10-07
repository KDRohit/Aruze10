using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonLearnMore3D : LobbyOptionButtonActive
{
	public float flipDur = 4.0f;
	public TextMeshPro messageTMPro;
	
	private Texture learnMoreTexture;
	private GameTimer cycleTimer;
	private bool isShowingMainTexture = false;

	public const string MESSAGE_FORMAT = "{0}_game_unlock_{1}_message_{2}";
	public const string FILENAME_FORMAT = "{0}_game_unlock_{1}_{2}_1X2.jpg";
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		
		messageTMPro.text = Localize.textUpper(
			string.Format(MESSAGE_FORMAT, option.game.xp.xpromoTarget, option.game.keyName, 1));
		
		string learnMoreImageFilename =
			SlotResourceMap.getLobbyImagePath(
				option.game.groupInfo.keyName, option.game.keyName, "1X2");

		learnMoreImageFilename = learnMoreImageFilename.Replace(
				option.game.keyName + "_1X2.jpg",
				string.Format(FILENAME_FORMAT, option.game.xp.xpromoTarget, option.game.keyName, 2));
		
		RoutineRunner.instance.StartCoroutine(
			DisplayAsset.loadTextureFromBundle(learnMoreImageFilename, imageTextureLoaded, null, skipBundleMapping:true, pathExtension:".png"));
		
		cycleTimer = new GameTimer(flipDur);
	#if RWR
		createRWR();
	#endif
	}
	
	protected override void Update()
	{
		base.Update();
		
		if (screenTexture != null &&
			learnMoreTexture != null &&
			cycleTimer != null &&
			cycleTimer.isExpired
			)
		{
			isShowingMainTexture = !isShowingMainTexture;
			
			screenMaterial.mainTexture = (isShowingMainTexture ? screenTexture : learnMoreTexture);

			messageTMPro.text = Localize.textUpper(
				string.Format(MESSAGE_FORMAT, option.game.xp.xpromoTarget, option.game.keyName, isShowingMainTexture ? 1 : 2));
			
			cycleTimer.startTimer(flipDur);
		}
		
	}
	
	private void imageTextureLoaded(Texture2D tex, Dict data)
	{
		if (tex != null)
		{
			learnMoreTexture = tex;
		}
	}
}
