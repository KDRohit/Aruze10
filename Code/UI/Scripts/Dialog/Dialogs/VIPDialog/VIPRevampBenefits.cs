using UnityEngine;
using System.Collections;

public class VIPRevampBenefits : MonoBehaviour
{
	public Animator animator;
	public MeshRenderer profileImage;
	public VIPNewIconRevamp vipIcon;
	public UISprite levelHighlight;
	public SwipeArea optionsSwipeArea;
	public SwipeArea benefitsSwipeArea;
	public UIScrollBar scrollBar;

	public string state { get; private set; }

	// =============================
	// CONST
	// =============================
	protected const string OVERVIEW_INTRO = "Overview Status Intro";
	protected const string OVERVIEW_OUTRO = "Overview Status Outro";
	protected const string OVERVIEW_OFF = "Overview Status Off";
	protected const string SWITCH_TO_STATUS = "Switch To Status Screen";
	protected const string SWITCH_TO_OVERVIEW = "Switch To Overview Screen";
	protected const int STARTING_HIGHLIGHT_X = -955;
	protected const int SPACING_X = 260;

	void Awake()
	{		
		refresh();
	}
	
    public void showBenefits()
	{
		if (optionsSwipeArea != null)
		{
			optionsSwipeArea.enabled = false;
		}
		benefitsSwipeArea.enabled = true;
		refresh();
		playAnimation(OVERVIEW_INTRO);
		StatsManager.Instance.LogCount("dialog", "vip_benefit", "overview", "", "", "view");

		disableScrollbar();
	}

	public void refresh()
	{
		SocialMember player = SlotsPlayer.instance.socialMember;
		DisplayAsset.loadTextureToRenderer(profileImage, player.getImageURL, "", true);
		int level = VIPLevel.getEventAdjustedLevel();
		vipIcon.setLevel(level);
		state = OVERVIEW_INTRO;

		float width = levelHighlight.transform.localScale.x;
		Vector3 vec = levelHighlight.transform.localPosition;
		vec.x = STARTING_HIGHLIGHT_X + SPACING_X * level;
		levelHighlight.transform.localPosition = vec;
	}

	public void onOverview()
	{
		benefitsSwipeArea.gameObject.SetActive(false);
		if (state == OVERVIEW_OFF || state == SWITCH_TO_STATUS)
		{
			playAnimation(SWITCH_TO_OVERVIEW);
			Audio.play("VIPScrollRight");
			state = SWITCH_TO_OVERVIEW;
			StatsManager.Instance.LogCount("dialog", "vip_benefit", "overview", "", "vip_status", "click");

			disableScrollbar();
		}
	}

	public void onStatus()
	{
		if (state == OVERVIEW_INTRO || state == SWITCH_TO_OVERVIEW)
		{
			if (VIPBenefitsSlider.instance != null)
			{
				VIPBenefitsSlider.instance.resetPosition();
			}

			enableScrollbar();

			playAnimation(SWITCH_TO_STATUS);
			Audio.play("VIPScrollLeft");
			state = SWITCH_TO_STATUS;
			StatsManager.Instance.LogCount("dialog", "vip_benefit", "status", "", "overview", "click");
		}
	}

	public void disableScrollbar()
	{
		if (scrollBar != null)
		{
			scrollBar.gameObject.SetActive(false);
		}
	}

	public void enableScrollbar()
	{
		if (scrollBar != null)
		{
			scrollBar.gameObject.SetActive(true);
		}
	}

	public void close()
	{
		Audio.play("minimenuclose0");

		if (optionsSwipeArea != null)
		{
			optionsSwipeArea.enabled = true;
		}

		benefitsSwipeArea.enabled = false;

		if (state == SWITCH_TO_STATUS)
		{
			StatsManager.Instance.LogCount("dialog", "vip_benefit", "status", "", "", "close");
		}
		else
		{
			StatsManager.Instance.LogCount("dialog", "vip_benefit", "overview", "", "", "close");
		}
		state = SWITCH_TO_STATUS;
		playAnimation(OVERVIEW_OUTRO);
	}

	private void playAnimation(string animName)
	{
		if (animator != null)
		{
			animator.Play(animName);
		}
	}
}
