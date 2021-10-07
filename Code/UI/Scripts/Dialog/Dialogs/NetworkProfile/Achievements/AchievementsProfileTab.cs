using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementsProfileTab : NetworkProfileTab
{
	[SerializeField] private MeshRenderer trophyImage;
	[SerializeField] private GameObject defaultTrophy;
    [SerializeField] private TextMeshPro trophyName;
	[SerializeField] private AchievementRankIcon rankIcon;
	[SerializeField] private GameObject trophyTooltip;
	[SerializeField] private GameObject rankTooltipPrefab;
	[SerializeField] private GameObject rankTooltipAnchor;	
	[SerializeField] private TextMeshPro trophyTooltipLabel;

	[SerializeField] private ClickHandler trophyClickHandler;
	[SerializeField] private ClickHandler trophyShroudHandler;
	[SerializeField] private ClickHandler rankClickHandler;

	private NetworkProfileRankTooltip rankTooltip;
	
	public override void init(SocialMember member, NetworkProfileDialog dialog)
	{
		base.init(member, dialog);

		if (rankIcon != null)
		{
			// Need to nullcheck while we support the old dialog.
			rankIcon.setRank(member);
		}

		if (trophyClickHandler != null)
		{
			// Need to nullcheck while we support the old dialog.
			trophyClickHandler.registerEventDelegate(trophyClicked);
		}

		if (trophyShroudHandler != null)
		{
			trophyShroudHandler.registerEventDelegate(trophyClicked);
		}
		
		if (rankClickHandler != null)
		{
			// Need to nullcheck while we support the old dialog.
			rankClickHandler.registerEventDelegate(rankClicked);
		}
		
		if (member.networkProfile != null && member.networkProfile.displayAchievement != null)
		{
			setFavoriteTrophy(member.networkProfile.displayAchievement);
		}
		else
		{
			// Use the default settings for this.
			if (trophyImage != null)
			{
				SafeSet.gameObjectActive(trophyImage.gameObject, false);
			}
			string tooltipKey = member.isUser ? "choose_completed_trophy" : "no_favorite_trophy_selected_yet";
			SafeSet.labelText(trophyTooltipLabel, Localize.text(tooltipKey, ""));
			SafeSet.gameObjectActive(defaultTrophy, true);
			SafeSet.labelText(trophyName, Localize.text("no_selected_trophy", ""));
		}
		
		if (trophyTooltip != null)
		{
			trophyTooltip.SetActive(false); // Default this to off.
		}

		if (rankTooltipAnchor != null && rankTooltipPrefab != null)
		{
			GameObject rankTooltipObject = GameObject.Instantiate(rankTooltipPrefab, rankTooltipAnchor.transform);
			if (rankTooltipObject == null)
			{
			    Debug.LogErrorFormat("NetworkProfileDisplay.cs -- setupProfile -- could not create the rank tooltip object...");
			}
			else
			{
				rankTooltipObject.SetActive(false);
				rankTooltip = rankTooltipObject.GetComponent<NetworkProfileRankTooltip>();
			}
		}		
	}

	public override void setFavoriteTrophy(Achievement achievement)
	{
		SafeSet.gameObjectActive(trophyImage.gameObject, true);
		SafeSet.gameObjectActive(defaultTrophy, false);
		if (trophyImage != null)
		{
			member.networkProfile.displayAchievement.loadTextureToRenderer(trophyImage);
		}
		SafeSet.labelText(trophyName, member.networkProfile.displayAchievement.name);
	}

	private void trophyClicked(Dict args = null)
	{
		if (member.networkProfile.displayAchievement != null)
		{
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "ll_profile",
				phylum: "favorite_trophy",
				klass: "click",
				family: member.networkProfile.displayAchievement.id,
				genus: member.networkID);
			dialog.showSpecificTrophy(Dict.create(D.ACHIEVEMENT, member.networkProfile.displayAchievement));
		}
		else
		{
			trophyTooltip.SetActive(!trophyTooltip.activeSelf);
		}
	}

	public override void rankClicked(Dict args = null)
	{
		if (rankTooltip != null)
		{
			rankTooltip.show(member);
		}
	}

	public override void hideRankTooltip() 
	{
		if (rankTooltip != null)
		{
			rankTooltip.hide();
		}
	}	
}
