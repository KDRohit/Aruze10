using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaytableDialogIndex : TICoroutineMonoBehaviour
{
	public GameObject[] pages;
	public string[] titleKeys;
	public int[] typeOffset;

	public string copyrightTitleLoc = "";
	public string copyrightBodyLoc = "";
	public string copyrightImage = "";

	public void initInfo()
	{
		List<string> gamesWithNoLegalInfo = new List<string>()
		{
			"lbb01"
		};
		string gameName = GameState.game.keyName;
		LobbyGame game = LobbyGame.find(gameName);
		SlotGameData gameData = SlotGameData.find(gameName);
		bool hasLegalInfo = false;
		int numberOfPages = 1; //Starting this at 1 since we're always going to at least have the symbols page
		int currentPageIndex = 0;

		//Grab our legal info
		if (game != null)
		{
			if (game.license != "" || game.groupInfo.license != "")
			{
				string licenseId = (!string.IsNullOrEmpty(game.license)) ? game.license : game.groupInfo.license;
				SlotLicense licenseInfo = SlotLicense.find(licenseId);
				copyrightBodyLoc = (!string.IsNullOrEmpty(game.paytableDescOverride)) ? game.paytableDescOverride : licenseInfo.legal_body;
				copyrightTitleLoc = (!string.IsNullOrEmpty(game.paytableTitleOverride)) ? game.paytableTitleOverride : licenseInfo.legal_title;
				copyrightImage = (!string.IsNullOrEmpty(game.paytableImageOverride)) ? game.paytableImageOverride : licenseInfo.legal_image;
				numberOfPages++;
				hasLegalInfo = true;
			}
			else
			{
				if (!gameName.Contains("gen") || !gamesWithNoLegalInfo.Contains(gameName)) //Not logging a warning for generic games becuase they don't have any legal info, for now. 
				{
					Debug.LogWarning("No license info found for the game or group");
				}
			}
		}

		numberOfPages += gameData.bonusGames.Length;

		//Give our arrays the correct sizes now that we know the number of pages we need
		pages = new GameObject[numberOfPages];
		titleKeys = new string[numberOfPages];
		typeOffset = new int[numberOfPages];
	
		int bonusTypeOffset = 0; //Used if the game has multiple types of bonus games

		for(int i = 0; i < numberOfPages; i++)
		{
			if (i == 0) //Our first page will always be our symbols page
			{
				pages[i] = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Dialogs/Paytables/Game Pages/common/PageSymbols");
				titleKeys[i] = "game_symbols";
				typeOffset[i] = 0;
			}
			else if (hasLegalInfo && i == numberOfPages - 1) //Our last page will always be our legal page
			{
				pages[i] = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Dialogs/Paytables/Game Pages/common/PageCopyright");
				titleKeys[i] = "legal";
				typeOffset[i] = 0;
			}
			else //Filling in the middle with our bonus game pages
			{
				pages[i] = SkuResources.loadSkuSpecificResourcePrefab("Prefabs/Dialogs/Paytables/Game Pages/common/PageBonusSingle");
				titleKeys[i] = "bonus_game";
				typeOffset[i] = bonusTypeOffset;
				bonusTypeOffset++;
			}
		}

	}
}
