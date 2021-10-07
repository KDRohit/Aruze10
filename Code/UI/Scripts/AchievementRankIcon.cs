using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementRankIcon : MonoBehaviour
{
    [SerializeField] private UISprite rankIcon;
	[SerializeField] private UISprite rankNumberSprite;
	
    [SerializeField] private TextMeshPro rankNameLabel;
    [SerializeField] private TextMeshPro pointsLabel;

	public void addTextToMasker(TextMeshProMasker masker)
	{
		if (masker != null)
		{
			masker.addObjectToList(rankNameLabel);
			masker.addObjectToList(pointsLabel);
		}
	}
	public void setRank(int rank, long points = 0L)
	{
		setRank(AchievementLevel.getLevel(rank), points);
	}

	public void setRank(AchievementLevel rank, long points = 0L)
	{
		if (rank == null)
		{
			// Don't do anything if this isn't a valid rank.
			return;
		}
		
		if (rankNumberSprite != null && rankIcon != null)
		{
			// If we have both linked, then we are using the larger images.
			setRankSprites(rankIcon, rankNumberSprite, rank);
		}
		else
		{
			// If we only have the rank icon sprite linked, then use the small badge.
			rankIcon.spriteName = rank.spriteName;
		}

	    SafeSet.labelText(rankNameLabel, rank.name);
		SafeSet.labelText(pointsLabel, CommonText.formatNumber(points));
	}

    public void setRank(SocialMember member)
	{
		setRank(member.achievementRank, member.achievementScore);
	}

	// In order to save atlas space we separated out the large rank icon sprites into parts to be reused, so we need to put them together here.
	private void setRankSprites(UISprite background, UISprite number, AchievementLevel level)
	{
		string backgroundName = "";
	    if (level.rank == 0)
		{
			backgroundName = "Big Badge Rank RisingStar";
		}
		else if (level.rank <= 3)
		{
			// Bronze
			backgroundName = "Big Badge Rank Masters";
		}
		else if (level.rank <= 6)
		{
			// Silver
			backgroundName = "Big Badge Rank Challenger";
		}
		else
		{
			//Gold.
			backgroundName = "Big Badge Rank Superstar";
		}

		string numberSpriteName = "";
		int num = level.rank % 3;
		if (level.rank == 0) 
		{
			// If we are the bottom rank then there isnt a number.
			number.gameObject.SetActive(false);
		}
		else if (num == 0)
		{
			numberSpriteName = string.Format("Rank Icon Number {0}", 3);			
		}
		else
		{
			numberSpriteName = string.Format("Rank Icon Number {0}", num);
		}
		
		background.spriteName = backgroundName;
		number.spriteName = numberSpriteName;
	}
	
	
	public static string STANDARD_RANK_ICON_PREFAB_PATH = "Features/Achievements Lobby/Prefabs/Rank Icon";
	private static GameObject rankIconPrefab = null;
	private static List<KeyValuePair<GameObject, SocialMember>> iconsToCreate;
	private static bool isDownloadingRankIcon = false;
	
	public static void loadRankIconToAnchor(GameObject parent, SocialMember member)
	{
		if (rankIconPrefab == null)
		{
			if (iconsToCreate == null)
			{
				iconsToCreate = new List<KeyValuePair<GameObject, SocialMember>>();
			}
			iconsToCreate.Add(new KeyValuePair<GameObject, SocialMember>(parent, member));
			if (!isDownloadingRankIcon)
			{
				isDownloadingRankIcon = true;
				// If we don't already have it cached, then download it now.
				AssetBundleManager.load(STANDARD_RANK_ICON_PREFAB_PATH, rankIconSuccess, rankIconFailure);
			}
		}
		else
		{
			applyRankIcon(parent, member);
		}
	}

	private static void applyRankIcon(GameObject parent, SocialMember member)
	{
		if (rankIconPrefab == null)
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- applyRankIcon -- the prefab was null but we somehow are calling this function, something has gone wrong.");
			return;
		}
		if (parent == null || member == null)
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- applyRankIcon -- no parent or socialMember, aborting.");
			return;
		}
	    GameObject rankIconObject = CommonGameObject.instantiate(rankIconPrefab, parent.transform) as GameObject;
		AchievementRankIcon rankIcon = rankIconObject.GetComponent<AchievementRankIcon>();
		if (rankIcon != null)
		{
		    rankIcon.setRank(member);
		}
		else
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- applyRankIcon -- instantiated the object but couldn't get the script off of it.");
		}
	}
	
	private static void rankIconSuccess(string assetPath, Object obj, Dict data = null)
	{
		isDownloadingRankIcon = false;
	    if (obj != null)
		{
			rankIconPrefab = obj as GameObject;

			if (rankIconPrefab == null)
			{
				Debug.LogErrorFormat("NetworkAchievements.cs -- rankIconSuccess -- loaded an object but failed to convert it to a GameObject: {0}", assetPath);
				return;
			}
			if (iconsToCreate != null)
			{
				for (int i = 0; i< iconsToCreate.Count; i++)
				{
					GameObject parent = iconsToCreate[i].Key;
					SocialMember member = iconsToCreate[i].Value;
					applyRankIcon(parent, member);
				}
			}
		}
		else
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- rankIconSuccess -- obj was null from path: {0}", assetPath);
		}
	}

	private static void rankIconFailure(string assetPath, Dict data = null)
	{
		isDownloadingRankIcon = false;		
		Debug.LogErrorFormat("NetworkAchievements.cs -- rankIconFailure -- failed to download the object from bundle!: {0}", assetPath);
	}	
	
}