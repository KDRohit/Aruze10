using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonLearnMore : LobbyOptionButtonActive, IResetGame
{
	public float flipDur = 4.0f;
	public TextMeshPro messageTMPro;
	public GameObject learnMoreImage;
	
	public const string MESSAGE_FORMAT = "{0}_game_unlock_{1}_message_{2}";
	public const string FILENAME_FORMAT = "{0}_game_unlock_{1}_{2}_1X2.jpg";

	private static bool hasClickedXPromo = false;
	private static bool hasViewedXPromo = false;
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);

		if (!hasViewedXPromo && MOTDDialog.getSkuGameUnlockPhylum() == "woz_game_unlock")
		{
			hasViewedXPromo = true;
			StatsManager.Instance.LogCount("lobby", "xpromo", MOTDDialog.getSkuGameUnlockPhylum(), "", "", "view");
		}
		
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
	#if RWR
		createRWR();
	#endif
	}
	
	private void imageTextureLoaded(Texture2D tex, Dict data)
	{
		if (tex != null && learnMoreImage != null)
		{
			Renderer rend = learnMoreImage.GetComponent<Renderer>();
			Material mat = new Material(getOptionShader());
			mat.mainTexture = tex;
			rend.material = mat;
					
			StartCoroutine(animateFlipbook());
		}
	}
	
	private IEnumerator animateFlipbook()
	{
		while (true)
		{
			messageTMPro.text = Localize.textUpper(
				string.Format(MESSAGE_FORMAT, option.game.xp.xpromoTarget, option.game.keyName, 1));
			
			image.SetActive(true);
			learnMoreImage.SetActive(false);
			
			yield return new WaitForSeconds(flipDur);
			
			messageTMPro.text = Localize.textUpper(
				string.Format(MESSAGE_FORMAT, option.game.xp.xpromoTarget, option.game.keyName, 2));
			
			image.SetActive(false);
			learnMoreImage.SetActive(true);
			
			yield return new WaitForSeconds(flipDur);
		}
	}

	protected override void OnClick()
	{
		base.OnClick();

		if (!hasClickedXPromo && MOTDDialog.getSkuGameUnlockPhylum() == "woz_game_unlock")
		{
			hasClickedXPromo = true;
			StatsManager.Instance.LogCount("lobby", "xpromo", MOTDDialog.getSkuGameUnlockPhylum(), "", "", "click");
		}
	}

	public static void resetStaticClassData()
	{
		hasClickedXPromo = false;
		hasViewedXPromo = false;
	}
}
