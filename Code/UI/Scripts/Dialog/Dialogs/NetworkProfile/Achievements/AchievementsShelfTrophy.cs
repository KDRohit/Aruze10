using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Class Name: AchievementsShelfTrophy.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: Display control class for the achievements in their "shelf" view.
Feature-flow: open the network profile and click on the trophies tab.
*/

public class AchievementsShelfTrophy : MonoBehaviour
{
	[SerializeField] private GameObject defaultTrophySprite;
    [SerializeField] private TextMeshPro nameLabel;
	[SerializeField] private TextMeshPro percentageLabel;
	[SerializeField] private GameObject newLabelObject;
	[SerializeField] private GameObject completedParent;
	[SerializeField] private GameObject inProgressParent;
	[SerializeField] private ClickHandler clickHandler;
	[SerializeField] private TextMeshPro newLabel;
	[SerializeField] private UITexture trophyImageTexture;
	[SerializeField] private MeshRenderer trophyRenderer; 
	[SerializeField] private GameObject favoriteCheckmark;
	[SerializeField] private UISprite progressSprite;
	[SerializeField] private GameObject particles;
	[SerializeField] private UISprite glowSprite;
	[SerializeField] private SpriteMask particleMask;

	[SerializeField] private GameObject checkmarkGlowOne;
	[SerializeField] private GameObject checkmarkGlowTwo;

	[SerializeField] private Color wozColor;
	[SerializeField] private Color wonkaColor;
	[SerializeField] private Color hirColor;
	[SerializeField] private Color networkColor;

	[SerializeField] private Color wozProgressColor;
	[SerializeField] private Color wonkaProgressColor;
	[SerializeField] private Color hirProgressColor;
	[SerializeField] private Color networkProgressColor;

	private ProfileAchievementsTab tab;
	private SocialMember member;
	private Achievement achievement;
	private SlideController slideController; // Need to store this to de-register.
	private List<Material> clonedMaterials;

	private Color trophyEarnedColor = new Color(1f, 1f, 1f);

	private void uiTextureLoadedCallback(Texture2D tex, Dict texData)
	{
		if (this == null)
		{
			// The dialog can be closed quickly before a download has finished, so if that has happened lets not
			// throw a missing reference exception and just bail here.
			return;
		}
		
		if (tex != null)
		{
			
			if (achievement == null || member == null)
			{
				Debug.LogErrorFormat("AchievementsShelfTrophy.cs -- textureLoadedCallback -- achievement or member was null...this is weird. and should not happen.");
				return;
			}

			if (trophyImageTexture != null && trophyImageTexture.gameObject != null)
			{
				// If the texture wasn't null, turn off our defaults and load it in.
				defaultTrophySprite.SetActive(false);
				Color color = achievement.isUnlocked(member) ? trophyEarnedColor : getTrophyProgressColor(achievement.sku);
				trophyImageTexture.gameObject.SetActive(true);
				DisplayAsset.loadTextureToUITextureCallback(tex, texData);
				trophyImageTexture.color = color;
			}
		}
		else
		{
			Debug.LogErrorFormat("AchievementsShelfTrophy.cs -- loadTextureToUITextureCallback -- texture was null, leaving defaults up.");
		}
	}

	private void renderTextureLoadedCallback(Texture2D tex, Dict texData)
	{
		if (this == null)
		{
			// The dialog can be closed quickly before a download has finished, so if that has happened lets not
			// throw a missing reference exception and just bail here.
			return;
		}
		
		if (tex != null)
		{

			if (achievement == null || member == null)
			{
				Debug.LogErrorFormat("AchievementsShelfTrophy.cs -- rendertextureLoadedCallback -- achievement or member was null...this is weird. and should not happen.");
				return;
			}

			if (trophyRenderer != null && trophyRenderer.gameObject != null && trophyRenderer.material != null)
			{
				// If the texture wasn't null, turn off our defaults and load it in.
				defaultTrophySprite.SetActive(false);
				Color color = achievement.isUnlocked(member) ? trophyEarnedColor : getTrophyProgressColor(achievement.sku);
				trophyRenderer.gameObject.SetActive(true);
				DisplayAsset.loadTextureToRendererCallback(tex, texData);
				trophyRenderer.material.color = color;
				//changing the colour of the material duplicates it, adn unity does not garbage collect this so we have to track it
				if (clonedMaterials == null)
				{
					clonedMaterials = new List<Material>();
				}
				//if we're setting the colour to the same value as in the init call, this may not actually create a new texture.  
				if (!clonedMaterials.Contains(trophyRenderer.material))
				{
					clonedMaterials.Add(trophyRenderer.material);
				}
				
			}
		}
		else
		{
			Debug.LogErrorFormat("AchievementsShelfTrophy.cs -- rendertextureLoadedCallback -- texture was null, leaving defaults up.");
		}
	}

	private Color getGlowColor(NetworkAchievements.Sku sku)
	{
		switch(sku)
		{
			case NetworkAchievements.Sku.WOZ:
				return wozColor;
			case NetworkAchievements.Sku.WONKA:
				return wonkaColor;
			case NetworkAchievements.Sku.NETWORK:
				return networkColor;
			case NetworkAchievements.Sku.HIR:
			default:
				return hirColor;
		}
	}	
	private Color getTrophyProgressColor(NetworkAchievements.Sku sku)
	{
		switch(sku)
		{
			case NetworkAchievements.Sku.WOZ:
				return wozProgressColor;
			case NetworkAchievements.Sku.WONKA:
				return wonkaProgressColor;
			case NetworkAchievements.Sku.NETWORK:
				return networkProgressColor;
			case NetworkAchievements.Sku.HIR:
			default:
				return hirProgressColor;
		}
	}	

	private void init(Achievement achievement, int index, SocialMember memberProfile)
	{
		member = memberProfile;
		this.achievement = achievement;
		nameLabel.text = achievement.name;
		Color glowColor = getGlowColor(achievement.sku);
		glowSprite.color = glowColor;
		defaultTrophySprite.SetActive(true);

		
		
		bool isUnlocked = achievement.isUnlocked(member);

		if (!isUnlocked)
		{
			Color colour = null == achievement ? hirProgressColor : getTrophyProgressColor(achievement.sku);
			if (trophyImageTexture != null)
			{
				trophyImageTexture.color = colour;
			}

			if (trophyRenderer != null && trophyRenderer.gameObject != null && trophyRenderer.material != null)
			{
				trophyRenderer.material.color = colour;
				if (clonedMaterials == null)
				{
					clonedMaterials = new List<Material>();
				}
				clonedMaterials.Add(trophyRenderer.material);
			}
			
			if (member.isUser && achievement.sku == NetworkAchievements.Sku.HIR)
			{
				// We only have this information for our own user, and the achievement is an hir achievment, and these calculations aren't trivial for every achievement
				// so let's only calculate them if we are actually going to show it.
				percentageLabel.text = Localize.textUpper("{0}_percent", achievement.getPercentage());
				progressSprite.fillAmount = (achievement.getPercentage() / 100f);
			}
		}
		else if (trophyImageTexture != null)
		{
			trophyImageTexture.color = trophyEarnedColor;
		}
		else if (trophyRenderer != null && trophyRenderer.gameObject != null && trophyRenderer.material != null)
		{
			trophyRenderer.material.color = trophyEarnedColor;
			if (clonedMaterials == null)
			{
				clonedMaterials = new List<Material>();
			}
			clonedMaterials.Add(trophyRenderer.material);
		}

		if (trophyImageTexture != null)
		{
			trophyImageTexture.gameObject.SetActive(false);
			achievement.loadTextureToUITexture(trophyImageTexture, uiTextureLoadedCallback);
		}
		if (trophyRenderer != null)
		{
			trophyRenderer.gameObject.SetActive(false);
			achievement.loadTextureToRenderer(trophyRenderer, renderTextureLoadedCallback);
		}

		bool isFavorite = (member != null &&
			member.isUser &&
			member.networkProfile != null &&
			member.networkProfile.displayAchievement != null &&
			(member.networkProfile.displayAchievement.id == achievement.id));


		glowSprite.gameObject.SetActive(isUnlocked);
		inProgressParent.SetActive(!isUnlocked && member.isUser && achievement.sku == NetworkAchievements.Sku.HIR);
		
		if (favoriteCheckmark != null)
		{
			favoriteCheckmark.SetActive(isFavorite);
		}
		
		if (checkmarkGlowOne != null)
		{
			checkmarkGlowOne.SetActive(isFavorite);
		}
		
		if (checkmarkGlowTwo != null)
		{
			checkmarkGlowTwo.SetActive(isFavorite);
		}

		if (completedParent != null)
		{
			completedParent.SetActive(isUnlocked);	
		}
		
		if (newLabelObject != null)
		{
			newLabelObject.SetActive(achievement.isNew);
		}
		
		if (null != particles)
		{
			particles.SetActive(achievement.isUnlockedNotClicked);
			if (achievement.isUnlockedNotClicked && particleMask != null)
			{
				particleMask.addMaskedObject(particles.transform);
			}
		}

		if (index >= 0)
		{
			clickHandler.registerEventDelegate(trophyClicked, Dict.create(D.ACHIEVEMENT, achievement, D.INDEX, index));
		}
	}

	public void init(Achievement achievement, SocialMember member)
	{
		init(achievement, -1, member);
	}
	
	public void init(Achievement achievement, int index, ProfileAchievementsTab tab, SlideController slideController)
	{
		this.slideController = slideController;
		this.tab = tab;
		particleMask = tab.particleMask;
		
		init(achievement, index, tab.member);

		
		if (achievement.isNew || achievement.isUnlockedNotSeen)
		{
			slideController.onContentMoved += checkForSeen;
			slideController.addMomentum(0.001f);
		}
		this.tab = tab;
	}

	public void refreshIcons()
	{
		bool isFavorite = (member != null &&
			member.isUser &&
			member.networkProfile != null &&
			member.networkProfile.displayAchievement != null &&
			(member.networkProfile.displayAchievement.id == achievement.id));

		favoriteCheckmark.SetActive(isFavorite);
		checkmarkGlowOne.SetActive(isFavorite);
		checkmarkGlowTwo.SetActive(isFavorite);

		particles.SetActive(achievement.isUnlockedNotClicked);
		newLabelObject.SetActive(achievement.isNew);
	}

	public TextMeshPro[] getAllTMPros()
	{
		return new TextMeshPro[] {
			nameLabel,
			percentageLabel,
			newLabel
		};
	}

	void OnDestroy()
	{
		if (achievement != null && achievement.isUnlockedNotClicked)
		{
			particleMask.removeMaskedObject(particles.transform);
		}

		if (slideController != null)
		{
			slideController.onContentMoved -= checkForSeen;
		}
		
		if (clonedMaterials != null)
		{
			for(int i=0; i<clonedMaterials.Count; ++i)
			{
				Destroy(clonedMaterials[i]);
			}

			clonedMaterials.Clear();
		}
	}

	private void trophyClicked(Dict args)
	{
		Achievement clickedAchievement = args.getWithDefault(D.ACHIEVEMENT, null) as Achievement;
		string achievementKey = (clickedAchievement == null) ? "null" : clickedAchievement.id;
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_room",
			klass: "click",
			family: achievementKey,
			genus: member.networkID);
	
		if (tab != null)
		{
			tab.trophyClickedCallback(args);
		}
		
	}
	
	private void checkForSeen(Transform contentTransform, Vector2 delta)
	{
		if (gameObject != null && tab.newMarkerTransform != null)
		{
			if (transform.position.y > tab.newMarkerTransform.position.y)
			{
				// If this is above the view marker, then mark it as seen and unregister this function from the event.
				NetworkAchievements.markAchievementSeen(achievement);
				NetworkAchievements.markUnlockedAchievementSeen(achievement);
				//achievement.isNew = false;
				slideController.onContentMoved -= checkForSeen;
			}			
		}
	}
}
