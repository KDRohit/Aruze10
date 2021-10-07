using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementsShelf : MonoBehaviour
{
	[SerializeField] private GameObject trophyPrefab;
	[SerializeField] private Transform[] anchorPoints;
	[SerializeField] private Animator animator;
	[SerializeField] private UISprite[] coloredSprites;

	[SerializeField] private Color wozColor;
	[SerializeField] private Color wonkaColor;
	[SerializeField] private Color hirColor;
	[SerializeField] private Color networkColor;
	
	private List<AchievementsShelfTrophy> panels;

	private Color getSpotlightColor(NetworkAchievements.Sku sku)
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
	
	public void init(List<Achievement> achievementList, int startIndex, int shelfSize, ProfileAchievementsTab tab, SlideController slideController)
	{
		if (achievementList == null || achievementList.Count == 0)
		{
			Debug.LogErrorFormat("AchievementsShelf.cs -- init -- list was null or empty");
			return;
		}

		// These should all be the same SKU so just grab the color from the first one.
		Color glowColor = getSpotlightColor(achievementList[0].sku);
		for (int i = 0; i < coloredSprites.Length; i++)
		{
			coloredSprites[i].color = glowColor;
		}
		
		panels = new List<AchievementsShelfTrophy>();
		for (int i = 0; i < shelfSize; i++)
		{
			int index = startIndex + i;
		    if (index > achievementList.Count -1)
			{
				break;
			}

			GameObject trophy = CommonGameObject.instantiate(trophyPrefab, anchorPoints[i]) as GameObject;
			if (trophy != null)
			{
				trophy.name = "Trophy";
			    AchievementsShelfTrophy panel = trophy.GetComponent<AchievementsShelfTrophy>();
				if (panel == null)
				{
					Debug.LogErrorFormat("AchievementsShelf.cs -- init -- could not get a NetworkAchievementsPanel script off of the created object.");
				}
				else
				{
					// Otherwise initialize it with the achievement.
					panel.init(achievementList[index], index, tab, slideController);
					panels.Add(panel);
				}
			}
		}
		animator.Play("intro");
	}

	public void refreshTrophies()
	{
		if (panels != null)
		{
			for (int i = 0; i < panels.Count; i++)
			{
				panels[i].refreshIcons();
			}
		}
	}

	public TextMeshPro[] getAllTMPros()
	{
		List<TextMeshPro> tmPros = new List<TextMeshPro>();
		for (int i = 0; i < panels.Count; i++)
		{
			tmPros.AddRange(panels[i].getAllTMPros());
		}
		return tmPros.ToArray();
	}
}
