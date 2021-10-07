using UnityEngine;
using System.Collections;

/*
Class Name: AchievementsSkuIcon.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: Container for setting up the skuIcon used in Network (cross SKU) achievements.
Feature-flow: N/A
*/
public class AchievementsSkuIcon : MonoBehaviour
{
	[SerializeField] private Animator animator;
	[SerializeField] private GameObject lockedIcon;
	[SerializeField] private GameObject unlockedIcon;
	[SerializeField] private GameObject downloadIcon;
	[SerializeField] private MeshRenderer skuRenderer;
	[SerializeField] private ButtonHandler iconHandler;

	[SerializeField] private Material hirMaterial;
	[SerializeField] private Material wozMaterial;
	[SerializeField] private Material wonkaMaterial;

	[SerializeField] private Color notCompleteColor;
	[SerializeField] private Color completeColor;

	public Achievement currentAchievement;

	// Animation consts
	private const float ANIMATION_DELAY = 0.2f;
	private const string INTRO_ANIMATION_NAME = "checkIntro";
	private const string OUTRO_ANIMATION_NAME = "iconOutro";
	private const string DARK_INTRO_ANIMATION_NAME = "preAnimDark";

	public void init(Achievement achievement, SocialMember member, bool willShowUnlock, AchievementsDisplayPanel achievementPanel)
	{
		gameObject.name = string.Format("App Icon {0}", (int)achievement.sku);
		currentAchievement = achievement;
		string appId = "";
		switch (achievement.sku)
		{
			case NetworkAchievements.Sku.HIR:
				appId = AppsManager.HIR_SLOTS_ID;
				skuRenderer.material = new Material(hirMaterial);
				break;
			case NetworkAchievements.Sku.WONKA:
				appId = AppsManager.WONKA_SLOTS_ID;
				skuRenderer.material = new Material(wonkaMaterial);
				break;
			case NetworkAchievements.Sku.WOZ:
				appId = AppsManager.WOZ_SLOTS_ID;
				skuRenderer.material = new Material(wozMaterial);
				break;
			case NetworkAchievements.Sku.BLACK_DIAMOND:
				appId = AppsManager.BDC_SLOTS_ID;
				skuRenderer.material = new Material (wozMaterial);
				break;
			default:
				appId = AppsManager.HIR_SLOTS_ID;
				skuRenderer.material = new Material(hirMaterial);
				break;
		}

		bool isUnlocked = achievement.isUnlocked(member) || willShowUnlock;
		skuRenderer.sharedMaterial.color = isUnlocked ? completeColor : notCompleteColor;

		if (isUnlocked)
		{
			// Achievement is unlocked, we dont care if you have the app installed.
			unlockedIcon.SetActive(true);
			lockedIcon.SetActive(false);
			downloadIcon.SetActive(false);
		}
		else
		{
			// In WebGL there is no downloading of an app, so this should always be the fallback case.
#if !UNITY_WEBGL
			if (!AppsManager.isBundleIdInstalled(appId))
			{
				// If the app is not installed, AND the achievement is locked, then show the download icon.
				unlockedIcon.SetActive(false);
				lockedIcon.SetActive(false);
				downloadIcon.SetActive(true);

			}
			else
#endif
			{
				// If you already have the app installed, then show the question mark.
				unlockedIcon.SetActive(false);
				lockedIcon.SetActive(true);
				downloadIcon.SetActive(false);
			}
		}

		Dict args = Dict.create(D.SKU_KEY, achievement.sku, D.ACTIVE, isUnlocked);
		iconHandler.clearAllDelegates(); // In case this is being re-used, clear delegates.
		iconHandler.registerEventDelegate(achievementPanel.onSkuIconClicked, args);

		if (willShowUnlock)
		{
			animator.Play(DARK_INTRO_ANIMATION_NAME);
		}
	}

	public void checkAdditionalAchievement(Achievement achievement, SocialMember member)
	{
		// If checking a second achievement, we only want to update the locked status.
		if (unlockedIcon.activeSelf && !achievement.isUnlocked(member))
		{
			// We only need to update it if we think it is unlocked already.
			// If it is currently locked, then we wont change that if this second one is unlocked.
			lockedIcon.SetActive(true);
			unlockedIcon.SetActive(false);
		}
	}
	
	public IEnumerator playIntro(int index)
	{
		// 0.2s delay between animations from left to right, so use use the index.
		if (index > 0)
		{
			yield return new WaitForSeconds(index * ANIMATION_DELAY);
		}
		animator.Play(INTRO_ANIMATION_NAME);
	}

	public IEnumerator playOutro(int index)
	{
		// 0.2s delay between animations from left to right, so use use the index.
		if (index > 0)
		{
			yield return new WaitForSeconds(index * ANIMATION_DELAY);
		}
		animator.Play(OUTRO_ANIMATION_NAME);
	}
}
