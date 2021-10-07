using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementsDisplayPanel : MonoBehaviour
{
    [HideInInspector] public Achievement achievement;

	[SerializeField] private GameObject trophyUnlockedHeader;
	[SerializeField] private MeshRenderer trophyRenderer;
	[SerializeField] private MeshRenderer trophyProgressRenderer;

	[SerializeField] private GameObject trophyDefault;
	[SerializeField] private GameObject trophyProgressDefault;
	
	[SerializeField] private TextMeshPro percentageLabel;
	[SerializeField] private TextMeshPro progressLabel;
	[SerializeField] private TextMeshPro dateCompletedLabel;
	[SerializeField] private UISprite progressSprite;
	[SerializeField] private UISprite progressBackgroundSprite;
	[SerializeField] private TextMeshPro trophyName;
	[SerializeField] private TextMeshPro trophyDescription;
	[SerializeField] private TextMeshPro pointsValue;
	[SerializeField] private MeshRenderer skuIconRenderer;
	[SerializeField] private Animator trophyAnimator;

	[SerializeField] private Animator infoBarAnimator;
	[SerializeField] private ImageButtonHandler rewardButton;
	[SerializeField] private TextMeshPro rewardButtonLabel;
	[SerializeField] private TextMeshPro rewardLabel;

	// Rarity
	[SerializeField] private List<AchievementsDisplayBanner> banners;

	// Netowrk Level Trophies
	[SerializeField] private UICenteredGrid networkSkuIconsGrid;
	[SerializeField] private GameObject networkSkuIconPrefab;
	[SerializeField] private AchievementsSkuInfoTooltip networkSkuTooltip;
	
	// Materials for the skuIcon.
	[SerializeField] private Material hirMaterial;
	[SerializeField] private Material wonkaMaterial;
	[SerializeField] private Material wozMaterial;
	[SerializeField] private Material blackDiamondMaterial;
	[SerializeField] private Material networkMaterial;

	[SerializeField] private UISprite[] glowSprites;
	[SerializeField] private Color hirGlowColor;
	[SerializeField] private Color wozGlowColor;
	[SerializeField] private Color wonkaGlowColor;
	[SerializeField] private Color networkGlowColor;

	[SerializeField] private Color hirTrophyShroudColor;
	[SerializeField] private Color wozTrophyShroudColor;
	[SerializeField] private Color wonkaTrophyShroudColor;
	[SerializeField] private Color networkTrophyShroudColor;

	[SerializeField] private Transform coinStartPos;
	[SerializeField] private Transform coinEndPos;
	[SerializeField] private UISprite backgroundSprite;
	[SerializeField] private string hirBackgroundSpriteName;
	[SerializeField] private string wozBackgroundSpriteName;
	[SerializeField] private string wonkaBackgroundSpriteName;
	[SerializeField] private string llBackgroundSpriteName;

	[SerializeField] private iTween.EaseType easeType;
	[SerializeField] private float gridTweenTime = 1.0f;


	// Trophy Animations
	private const string TROPHY_ANIM_OLD = "Old Trophy";
	private const string TROPHY_ANIM_NEW = "New Trophy";
	private const string TROPHY_ANIM_PROGRESS = "Trophy progress";
	private const string TROPHY_UNLOCKED_AUDIO = "TrophyUnlockedNetworkAchievements";
	private const float NETWORK_SKU_ICON_PAUSE_DURATION = 1.0f;
	private const float TROPHY_ANIMATION_LENGTH = 1.1f;

	private Color trophyEarnedColor = new Color(1f, 1f, 1f);
	private SocialMember member;
	private ClickHandler.onClickDelegate rewardClickCallback;

	private Dictionary<NetworkAchievements.Sku, AchievementsSkuIcon> networkSkuIconMap = new Dictionary<NetworkAchievements.Sku, AchievementsSkuIcon>();

	private void textureLoadedCallback(Texture2D tex, Dict texData)
	{
		if (this == null)
		{
			// Crittercism crash, supsect users exiting dialog and images arriving after garbage collection
			/*
			Crashed Thread

			0	at AchievementsDisplayPanel.textureLoadedCallback(UnityEngine.Texture2D tex, .Dict texData)
			1	DisplayAsset.finishAndCallback()
			2			
			*/
			Bugsnag.LeaveBreadcrumb("AchievementsDisplayPanel : this == null on textureLoadedCallback , exiting...");
			return;
		}

		if (tex != null)
		{
			// If we successfully got a texture, turn off these defaults.
			SafeSet.gameObjectActive(trophyDefault, false);
			SafeSet.gameObjectActive(trophyProgressDefault, false);
			
			// call the DisplayAsset method.
			DisplayAsset.loadTextureToRendererCallback(tex, texData);

			if (achievement != null)
			{
				bool isUnlocked = achievement.isUnlocked(member);
				Color color = isUnlocked ? trophyEarnedColor : getTrophyProgressColor(achievement.sku);
				MeshRenderer selectedRenderer = isUnlocked ? trophyRenderer : trophyProgressRenderer;
				if (selectedRenderer != null && selectedRenderer.gameObject != null && selectedRenderer.material != null)
				{
					selectedRenderer.gameObject.SetActive(true); // Turn the renderer on.
					selectedRenderer.material.color = color;
				}
			}
			else
			{
				Bugsnag.LeaveBreadcrumb("AchievementsDisplayPanel : achievement == null on textureLoadedCallback , exiting...");
			}
		
		}
		else
		{
			Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- textureLoadedCallback -- failed to load a texture!!");
		}
	}
	
	
	private string getTrophyStateAnimationName(Achievement achievement)
	{
		string format = "{0} intro";
		string result;
		if (achievement.isUnlockedNotClicked)
		{
			result =  string.Format(format, TROPHY_ANIM_NEW);
		}
		else if (achievement.isUnlocked(member))
		{
			result =  string.Format(format, TROPHY_ANIM_OLD);
		}
		else
		{
			result =  string.Format(format, TROPHY_ANIM_PROGRESS);
		}
		return result;
	}

	protected IEnumerator coinFly()
	{
		if (this.gameObject == null)
		{
			yield break;
		}

		Transform parent = this.gameObject.transform.parent == null ? this.gameObject.transform : this.gameObject.transform.parent;

		// Create the coin as a child of "parent", at the position of "coinIconSmall",
		// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
		CoinScriptUpdated  coin = CoinScriptUpdated.create(
			parent,
			coinStartPos.position,
			new Vector3(0, 0, -100)
		);

		Audio.play("initialbet0");
		// Calculate the local coordinates of the destination, which is where "coinIconLarge" is positioned relative to "sizer".
		
		Vector2 destination = NGUIExt.localPositionOfPosition(parent, coinEndPos.position);
		yield return StartCoroutine(coin.flyTo(destination));
		yield return new WaitForSeconds(3.0f);
		coin.destroy();

		if (rewardClickCallback != null)
		{
			rewardClickCallback.Invoke(Dict.create());
		}
	}
	
	public void init(Achievement achievement, SocialMember member, bool showUnlocked, ClickHandler.onClickDelegate clickCallback)
	{
		if (achievement == null)
		{
			Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- init() -- achievement was null somewhow, bailing out!");
			return;
		}

		if (member == null)
		{
			Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- init() -- member was null somehow, bailing out before NRE!");
			return;
		}
					   
		this.achievement = achievement;
		this.member = member;
		this.rewardClickCallback = clickCallback;
		
		trophyName.text = achievement.name;
		trophyDescription.text = achievement.description;
		pointsValue.text = achievement.score.ToString();
		
		NetworkAchievements.AchievementRarity rarity = NetworkAchievements.getRarity(achievement.rarityId);

		if (banners != null)
		{
			//map banners to rarities from scat data (in-efficient, but we're dealing with sets of 4, so it shouldn't be noticeable)
			for (int i = 0; i < banners.Count; i++)
			{
				AchievementsDisplayBanner rarityBanner = banners[i];
				if (rarityBanner == null || rarityBanner.gameObject == null)
				{
					Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- init() -- raritybanner was null so bailing out.");
					continue;
				}
				if (!NetworkAchievements.rewardsEnabled || null == rarity)
				{
					rarityBanner.init(achievement.unlockPercentage);
					rarityBanner.gameObject.SetActive("common" == rarityBanner.getRarity().ToLower().Trim());
				}
				else
				{
					bool isEnabled = rarityBanner.getRarity().ToLower().Trim() == rarity.name.ToLower().Trim();
					rarityBanner.init(achievement.unlockPercentage);
					rarityBanner.gameObject.SetActive(isEnabled);
				}
			}
		}
		else
		{
			Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- init() -- the banners have not been attached to the script!");
		}
		
		setSkuIcon(achievement.sku);
		setBackgroundSprite(achievement.sku);
		setTrophyLabelsAndSprite(achievement);
		for (int i = 0; i < glowSprites.Length; i++)
		{
			glowSprites[i].color = getGlowColor(achievement.sku);
		}

		//set unocked header
		bool isUnlocked = achievement.isUnlocked(member);
		bool willShowUnlock = isUnlocked && showUnlocked;
		trophyUnlockedHeader.SetActive(willShowUnlock);
		if (willShowUnlock)
		{
			Audio.play(TROPHY_UNLOCKED_AUDIO);
		}

		//set reward button
		rewardButton.gameObject.SetActive(
			member.isUser &&
			NetworkAchievements.rewardsEnabled &&
			achievement.sku == NetworkAchievements.Sku.HIR &&
			(!isUnlocked || !member.achievementProgress.isRewardCollected(achievement.id)));

		rewardButton.enabled =
			member.isUser &&
			isUnlocked &&
			!member.achievementProgress.isRewardCollected(achievement.id) &&
			achievement.sku == NetworkAchievements.Sku.HIR;

		rewardButtonLabel.text = Localize.text(rewardButton.enabled ? "collect" : "reward");
		rewardButton.registerEventDelegate(collectReward);
		rewardLabel.text = string.Format("{0:n0}", CreditsEconomy.convertCredits(achievement.reward));


		if (member.isUser &&
			achievement.sku == NetworkAchievements.Sku.NETWORK &&
			achievement.linkedAchievements != null &&
			(!achievement.isUnlocked(member) || willShowUnlock))
		{
			// If this is the current user, the achievement is a network-level one,
			// and it is either in progress, or we are showing it being unlocked, then show the network UI.
			setupNetworkAchievementUI(willShowUnlock);
		}
		else
		{
			// Otherwise just turn the whole object off.
			networkSkuIconsGrid.gameObject.SetActive(false);
		}
	}

	private void setupNetworkAchievementUI(bool willShowUnlock)
	{
		// First turn off each icon in case this is being re-used.
		foreach (KeyValuePair<NetworkAchievements.Sku, AchievementsSkuIcon> pair in networkSkuIconMap)
		{
			if (pair.Value != null && pair.Value.gameObject != null)
			{
				pair.Value.gameObject.SetActive(false);
				pair.Value.currentAchievement = null; // Set this to null so we know if it has been setup again later.
			}
		}
			
		for (int i = 0; i < achievement.linkedAchievements.Count; i++)
		{
			if (!networkSkuIconMap.ContainsKey(achievement.linkedAchievements[i].sku))
			{
				// If we havent encountered this SKU on this panel yet, create a new icon.
				GameObject go = GameObject.Instantiate(networkSkuIconPrefab, networkSkuIconsGrid.transform);
				if (go != null)
				{
					AchievementsSkuIcon networkSkuIcon = go.GetComponent<AchievementsSkuIcon>();
					if (networkSkuIcon != null)
					{
						networkSkuIcon.init(achievement.linkedAchievements[i], member, willShowUnlock, this);
						// For each linked achievement, create a SKU icon and initialize it.
						networkSkuIconMap.Add(achievement.linkedAchievements[i].sku, networkSkuIcon);
						go.SetActive(true); // Make sure it is on.
					}
					else
					{
						Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- init() -- failed to find a AchievementsSkuIcon script on the object.");
					}
				}
				else
				{
					Debug.LogErrorFormat("AchievementsDisplayPanel.cs -- init() -- failed to create an game object from the networkSkuIconPrefab");
				}
			}
			else
			{
				if (networkSkuIconMap[achievement.linkedAchievements[i].sku].currentAchievement == null)
				{
					// If we are re-using it and it hasnt been initialized yet, do that here.
					networkSkuIconMap[achievement.linkedAchievements[i].sku].init(
						achievement.linkedAchievements[i],
						member,
						willShowUnlock,
						this);
					// We turned it off earlier in case it wasn't used, so turn it on again once its has been initialized.
					networkSkuIconMap[achievement.linkedAchievements[i].sku].gameObject.SetActive(true);
				}
				else
				{
					// This wasnt designed for multiple trophies from the same sku,
					// but the progress will already work for it, so we might as well make the
					// UI handle it as well. Just check if we need to update the locked status.						
					networkSkuIconMap[achievement.linkedAchievements[i].sku].checkAdditionalAchievement(achievement.linkedAchievements[i], member);
				}
			}
		}
		networkSkuIconsGrid.reposition();
		networkSkuIconsGrid.gameObject.SetActive(true); // Now turn it on.
		networkSkuIconsGrid.gameObject.transform.localScale = Vector3.one * 0.0001f; // Make it tiny since we tween it in.
	}

	private Color getTrophyProgressColor(NetworkAchievements.Sku sku)
	{
		switch(sku)
		{
			case NetworkAchievements.Sku.WOZ:
				return wozTrophyShroudColor;
			case NetworkAchievements.Sku.WONKA:
				return wonkaTrophyShroudColor;
			case NetworkAchievements.Sku.NETWORK:
				return networkTrophyShroudColor;
			case NetworkAchievements.Sku.HIR:
			default:
				return hirTrophyShroudColor;
		}
	}

	private Color getGlowColor(NetworkAchievements.Sku sku)
	{
		switch(sku)
		{
			case NetworkAchievements.Sku.WOZ:
				return wozGlowColor;
			case NetworkAchievements.Sku.WONKA:
				return wonkaGlowColor;
			case NetworkAchievements.Sku.NETWORK:
				return networkGlowColor;
			case NetworkAchievements.Sku.HIR:
			default:
				return hirGlowColor;
		}
	}
	
	private void setTrophyLabelsAndSprite(Achievement currentAchievement)
	{
		// If this is the display achievement for that user, assume it is unlocked.		
		bool isUnlocked = currentAchievement.isUnlocked(member);
		if (isUnlocked)
		{
			// Show the complete trophy;
			if (currentAchievement.getUnlockedTime(member) != System.DateTime.Now)
			{
				dateCompletedLabel.text = Localize.text("unlocked_{0}", currentAchievement.getUnlockedTime(member).ToShortDateString());
				dateCompletedLabel.gameObject.SetActive(true);
			}
			else
			{
				dateCompletedLabel.gameObject.SetActive(false);
			}
			
			trophyDefault.SetActive(true);
			trophyRenderer.gameObject.SetActive(false);

			currentAchievement.loadTextureToRenderer(trophyRenderer, textureLoadedCallback);
			trophyRenderer.material.color = trophyEarnedColor;
			progressLabel.gameObject.SetActive(false);
			percentageLabel.gameObject.SetActive(false);
		}
		else
		{
			// This can be turned on by the animation when it is a "new" trophy so make sure it is off.
			dateCompletedLabel.gameObject.SetActive(false);
			
		    trophyProgressDefault.SetActive(true);
		    trophyProgressRenderer.gameObject.SetActive(false);
			currentAchievement.loadTextureToRenderer(trophyProgressRenderer, textureLoadedCallback);
			
			if (member.isUser && achievement.sku == NetworkAchievements.Sku.HIR)
		    {
				// Show the progress.
				long progress = currentAchievement.getProgress(member);
				long goal = currentAchievement.goal;
				int percent = currentAchievement.getPercentage(member);
				progressLabel.text = Localize.text("{0}_of_{1}", CommonText.formatNumber(progress), CommonText.formatNumber(goal));
				progressLabel.gameObject.SetActive(true);
				percentageLabel.text = Localize.text("{0}_percent", percent);
				progressSprite.fillAmount = percent / 100f;
			}
			else
			{
				progressLabel.gameObject.SetActive(false);
				percentageLabel.gameObject.SetActive(false);
				progressSprite.gameObject.SetActive(false);
				progressBackgroundSprite.gameObject.SetActive(false);
			}	
		}
	}

	private void collectReward(Dict args)
	{
		rewardButton.enabled = false;

		//0 reward will cause an exception
		if (achievement.reward > 0)
		{
			NetworkAchievementAction.collectAchievementReward(achievement);
		}
		else
		{
			Debug.LogWarning("Invalid reward for " + achievement.id);
		}

		//run the coin fly
		StartCoroutine(coinFly());

	}

	// Callback from the AchievementSkuIcon being clicked, launches the tooltip.
	public void onSkuIconClicked(Dict args)
	{
		bool isUnlocked = (bool)args.getWithDefault(D.ACTIVE, false);
		NetworkAchievements.Sku skuKey = (NetworkAchievements.Sku)args.getWithDefault(D.SKU_KEY, NetworkAchievements.Sku.HIR);
		networkSkuTooltip.show(skuKey, isUnlocked, achievement);
	}

	public IEnumerator playSkuIconAnimations()
	{
		AchievementsSkuIcon currentIcon = null;
		NetworkAchievements.Sku sku;
		int iconIndex = 0;
		for (int i = 0; i < NetworkAchievements.skuList.Count; i++)
		{
			sku = NetworkAchievements.skuList[i];
			if (networkSkuIconMap.ContainsKey(sku))
			{
				currentIcon = networkSkuIconMap[sku];
				if (currentIcon != null && currentIcon.currentAchievement != null)
				{
					// If that sku icon exists in the sku map, and it is setup currently, play the intro animation.
					yield return RoutineRunner.instance.StartCoroutine(currentIcon.playIntro(iconIndex));
					iconIndex++;
				}
			}
		}
		
		yield return new WaitForSeconds(NETWORK_SKU_ICON_PAUSE_DURATION);
		
		iconIndex = 0;
		for (int i = 0; i < NetworkAchievements.skuList.Count; i++)
		{
			sku = NetworkAchievements.skuList[i];
			if (networkSkuIconMap.ContainsKey(sku))
			{
				currentIcon = networkSkuIconMap[sku];
				if (currentIcon != null && currentIcon.currentAchievement != null)
				{
					// If that sku icon exists in the sku map, and it is setup currently, play the intro animation.
					yield return RoutineRunner.instance.StartCoroutine(currentIcon.playOutro(iconIndex));
					iconIndex++;
				}
			}
		}
		yield return null;
	}
	
	public void showTrophy()
	{
		trophyAnimator.Play(getTrophyStateAnimationName(achievement));
		infoBarAnimator.Play("dialogueContentIntro");

		if (achievement.sku == NetworkAchievements.Sku.NETWORK && networkSkuIconsGrid.gameObject.activeSelf)
		{
			// If this is a network achievment, and we are showing the grid, we want to tween it in.
			iTween.ScaleTo(networkSkuIconsGrid.gameObject,
				iTween.Hash(
					"isLocal", true,
					"scale", Vector3.one,
					"time", TROPHY_ANIMATION_LENGTH,
					"easeType", iTween.EaseType.easeOutBounce
				));
		}
		
		if (achievement.isNew)
		{
		    NetworkAchievements.markAchievementSeen(achievement); // Mark it seen
		}

		if (achievement.isUnlockedNotSeen)
		{
			// This is now a different list than "new" becuase we want to only decrement it once
			// they view this trophy in the individual view.
			NetworkAchievements.markUnlockedAchievementSeen(achievement);
		}
		
		if (achievement.isUnlockedNotClicked)
		{
			// This is now a different list than "new" becuase we want to only decrement it once
			// they view this trophy in the individual view.
			NetworkAchievements.markUnlockedAchievementClicked(achievement);
		}

		//set reward button
		bool isUnlocked = achievement.isUnlocked(member);
		// If progress is null, assume it has been collected so we dont show the button and potentially throw an error. This should only ever be null on other members trophies anyways.
		bool hasCollected = member.achievementProgress == null || member.achievementProgress.isRewardCollected(achievement.id);

		bool shouldShowRewardButton = member.isUser &&
			achievement.sku == NetworkAchievements.Sku.HIR &&
			NetworkAchievements.rewardsEnabled &&
			(!isUnlocked || !hasCollected);
		rewardButton.gameObject.SetActive(shouldShowRewardButton);
	}

	// Calls showTrophy and then waits for the animations to finish. Used for Network Trophies reward displaying.
	public IEnumerator showTrophyAndWait()
	{
		showTrophy();
		yield return new WaitForSeconds(TROPHY_ANIMATION_LENGTH);
	}

	public void hideTrophy()
	{
		//hide trophy
		trophyAnimator.Play("default");

		//hide the collect button
		rewardButton.gameObject.SetActive(false);
	}
	
	private void setBackgroundSprite(NetworkAchievements.Sku sku)
	{
		switch (sku)
		{
			case NetworkAchievements.Sku.WONKA:
				backgroundSprite.spriteName = wonkaBackgroundSpriteName;
				break;
			case NetworkAchievements.Sku.WOZ:
				backgroundSprite.spriteName = wozBackgroundSpriteName;
				break;
			case NetworkAchievements.Sku.NETWORK:
				backgroundSprite.spriteName = llBackgroundSpriteName;
				break;
			case NetworkAchievements.Sku.HIR:
			default:
				backgroundSprite.spriteName = hirBackgroundSpriteName;
				break;
		}
	}

	private void setSkuIcon(NetworkAchievements.Sku sku)
	{
	    switch (sku)
		{
			case NetworkAchievements.Sku.WONKA:
				skuIconRenderer.sharedMaterial = wonkaMaterial;
				break;
			case NetworkAchievements.Sku.WOZ:
				skuIconRenderer.sharedMaterial = wozMaterial;
				break;
			case NetworkAchievements.Sku.BLACK_DIAMOND:
				skuIconRenderer.sharedMaterial = blackDiamondMaterial;
				break;
			case NetworkAchievements.Sku.NETWORK:
				skuIconRenderer.sharedMaterial = networkMaterial;
				break;
			case NetworkAchievements.Sku.HIR:
			default:
				skuIconRenderer.sharedMaterial = hirMaterial;
				break;
		}
	}
}
