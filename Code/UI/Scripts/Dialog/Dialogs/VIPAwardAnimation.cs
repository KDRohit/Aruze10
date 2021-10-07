using UnityEngine;
using System.Collections;
using TMPro;
/*
Controls display and functionality of VIP introduction animations.
*/

public class VIPAwardAnimation : TICoroutineMonoBehaviour
{
	public Animator animator;
	public TextMeshPro buttonLabel;
	public GameObject cardParent;
	public GameObject gameBoxesParent;
	public GameObject earlyAccessParent;
	public GameObject newGameParent;
	public VIPNewIcon vipIcon;
	public VIPNewIcon playerVIPIcon;
	public FacebookFriendInfo facebookInfo;
	public TextMeshPro welcomeLabel;
	public TextMeshPro transferLabel;
	public TextMeshPro awardedLabel;
	public GameObject transferManualActive;			// Allows deactivating this item regardless of animation active state.
	public GameObject awardedManualActive;			// Allows deactivating this item regardless of animation active state.
	public GameObject benefitsManualActive;			// Allows deactivating this item regardless of animation active state.
	public GameObject visitVIPButtonManualActive;	// Allows deactivating this item regardless of animation active state.
	public Renderer earlyAccessRenderer;
	public Renderer newGameRenderer;
	public GameObject benefitsBox;
	public TextMeshPro extraPurchasesLabel;
	public TextMeshPro extraDailyBonusLabel;
	public TextMeshPro extraFromGiftsLabel;
	public TextMeshPro extraSentLabel;
	public GameObject vipStatusBoostAnchor;

	private int gameIconCount = 0;
		
	private GenericDelegate clickFunction = null;
	
	// Initialization
	public IEnumerator init(VIPLevel level, bool isFromIntro, bool isFirstTime, GenericDelegate clickFunction)
	{
		// Gimme da assets!
		if (VIPStatusBoostEvent.isEnabled())
		{
			VIPStatusBoostEvent.loadVIPLevelUpAssets(vipStatusBoostAnchor);
		}

		this.clickFunction = clickFunction;
		
		facebookInfo.member = SlotsPlayer.instance.socialMember;
		
		vipIcon.setLevel(level);
		playerVIPIcon.setLevel(level);
		
		bool doShowVIPButton = true;

		string firstName =  SlotsPlayer.instance.socialMember.firstName;
		if (isFirstTime)
		{
			if (!SlotsPlayer.isAnonymous ||
				!string.IsNullOrEmpty(firstName) && firstName != SocialMember.BLANK_USER_NAME)
			{
				welcomeLabel.text = Localize.textUpper("vip_name_header_{0}", SlotsPlayer.instance.socialMember.firstName);
			}
			else
			{
				welcomeLabel.text = Localize.textUpper("welcome");
			}
		}
		else
		{
			if (!SlotsPlayer.isAnonymous ||
				!string.IsNullOrEmpty(firstName) && firstName != SocialMember.BLANK_USER_NAME)
			{
				welcomeLabel.text = Localize.textUpper("congratulations_{0}", SlotsPlayer.instance.socialMember.firstName);
			}
			else
			{
				welcomeLabel.text = Localize.textUpper("congratulations_ex");
			}
		}
		
		if (isFromIntro)
		{
			// Show some slightly different wording on the intro version.
			buttonLabel.text = Localize.textUpper("continue");
			transferLabel.text = Localize.textUpper("vip_welcome_description_{0}", level.name);
			awardedManualActive.gameObject.SetActive(false);
			benefitsManualActive.SetActive(false);
			visitVIPButtonManualActive.SetActive(false);
		}
		else
		{
			buttonLabel.text = Localize.textUpper("ok");
			awardedLabel.text = Localize.textUpper("vip_awarded_description_{0}", level.name);
			transferManualActive.gameObject.SetActive(false);
			
			extraPurchasesLabel.text = Localize.text("plus_{0}_percent", CommonText.formatNumber(level.purchaseBonusPct));
			extraDailyBonusLabel.text = Localize.text("plus_{0}_percent", CommonText.formatNumber(level.dailyBonusPct));
			extraFromGiftsLabel.text = Localize.text("plus_{0}_percent", CommonText.formatNumber(level.receiveGiftBonusPct));
			extraSentLabel.text = Localize.text("plus_{0}_percent", CommonText.formatNumber(level.sendGiftBonusPct));

			if (level.levelNumber == 0)
			{
				// There's no benefits to show at level 0, so just hide the box.
				benefitsManualActive.SetActive(false);
			}

			doShowVIPButton = (VIPLobby.instance == null);
		}
		
		LobbyGame newGame = (level.games.Count > 0 ? level.games[0] : null);
		LobbyGame eaGame = null;
				
		if (LobbyGame.vipEarlyAccessGame != null && level.levelNumber == LobbyGame.vipEarlyAccessGame.vipLevel.levelNumber)
		{
			eaGame = LobbyGame.vipEarlyAccessGame;
		}
		
		loadGameIcon(newGame, newGameRenderer);
		loadGameIcon(eaGame, earlyAccessRenderer);
		
		if (newGame == null && eaGame == null)
		{
			// Hide the game boxes and center the card.
			gameBoxesParent.SetActive(false);
			CommonTransform.setX(cardParent.transform, 0.0f);
			doShowVIPButton = false;
		}
		else if (newGame == null && eaGame != null)
		{
			// Only the early access game exists, so center it and hide the new game box.
			newGameParent.SetActive(false);
			CommonTransform.setX(earlyAccessParent.transform, 0.0f);
		}
		else if (newGame != null && eaGame == null)
		{
			// Only the new game exists, so center it and hide the early access game box.
			earlyAccessParent.SetActive(false);
			CommonTransform.setX(newGameParent.transform, 0.0f);
		}
		// else if both games exist, don't change anything.

		if (!doShowVIPButton)
		{
			// Already in the high limit room, so hide the button that goes there.
			visitVIPButtonManualActive.SetActive(false);
		}
		
		while (gameIconCount < 2)
		{
			yield return null;
		}
		
		animator.Play("VIP Award");
	}
	
	private void loadGameIcon(LobbyGame game, Renderer rend)
	{
		if (game == null)
		{
			gameIconCount++;
			return;
		}
		
		rend.material = new Material(LobbyOptionButtonActive.getOptionShader());
		rend.material.color = Color.white;
	
		string filename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, "");
		
		StartCoroutine(DisplayAsset.loadTextureFromBundle(filename, optionTextureLoaded, Dict.create(D.ICON, rend), skipBundleMapping:true, pathExtension:".png"));
	}

	// Callback for loading a texture.
	private void optionTextureLoaded(Texture2D tex, Dict data)
	{
		gameIconCount++;
		
		Renderer rend = data.getWithDefault(D.ICON, null) as Renderer;
		
		if (tex != null)
		{
			rend.material.mainTexture = tex;
		}
	}
			
	public void continueClicked()
	{
		if (clickFunction != null)
		{
			clickFunction();
		}
	}

	public void visitVIPClicked()
	{
		Dialog.close();
		LobbyLoader.returnToNewLobbyFromDialog(true, LobbyInfo.Type.VIP);
	}
}
