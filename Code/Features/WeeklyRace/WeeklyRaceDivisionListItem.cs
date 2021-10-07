using UnityEngine;
using System.Collections;

using TMPro;

public class WeeklyRaceDivisionListItem : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private UISprite background;
	[SerializeField] private GameObject dailyBonusWheel;
	[SerializeField] private UITexture profileImage;
	[SerializeField] private Animator profileAnimator;
	[SerializeField] private Animator backgroundAnimator;
	public GameObject profileObject;
	
	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro divisionText;
	public TextMeshPro dailyBonusText;
	public TextMeshPro freeBonusText;
	public UISprite divisionBadge;
	public UISprite divisionLabel;

	// =============================
	// CONST
	// =============================
	private const string PLAYER_BG = "Player Panel Bar Blue Stretchy";
	private const string STANDARD_BG = "Player Panel Bar Purple Stretchy";
	private const string PROMOTION_ANIM = "Promotion Landing";
	private const string DEMOTION_ANIM = "Demotion Landing";
	private const string PROFILE_ANIM = "Profile Picture Lift Land";

	public void setup(int division, int divisionBonus, bool isPlayersDivision = false)
	{
		divisionBadge.spriteName = WeeklyRace.getBadgeSprite(division);
		divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(division);
		divisionText.text = WeeklyRace.getFullDivisionName(division);

		dailyBonusText.text = string.Format("+{0}%", divisionBonus.ToString());
		divisionLabel.gameObject.SetActive(division > 0);

		if (isPlayersDivision)
		{
			profileObject.SetActive(true);
			SlotsPlayer.instance.socialMember.loadProfileImageToUITexture(profileImage);
			background.spriteName = PLAYER_BG;
			dailyBonusWheel.SetActive(true);
		}
		else
		{
			background.spriteName = STANDARD_BG;
			profileObject.SetActive(false);
			dailyBonusWheel.SetActive(false);
		}
	}

	public void showPlayerBg()
	{
		background.spriteName = PLAYER_BG;		
		dailyBonusWheel.SetActive(true);
	}

	public void showStandardBg()
	{
		background.spriteName = STANDARD_BG;
		dailyBonusWheel.SetActive(false);
	}

	public void playProfileAnimation()
	{
		profileAnimator.Play(PROFILE_ANIM);
	}

	public void playPromotionAnimation()
	{
		backgroundAnimator.Play(PROMOTION_ANIM);
	}

	public void playDemotionAnimation()
	{
		backgroundAnimator.Play(DEMOTION_ANIM);		
	}
}
