using UnityEngine;
using System.Collections;
using TMPro;

public class NetworkProfileOverlayButton : MonoBehaviour
{
	[SerializeField] private MeshRenderer profileRenderer;
	[SerializeField] private FacebookFriendInfo fbInfo;
	[SerializeField] private ButtonHandler buttonHandler;
    public GameObject gemCycleAnchor;

	// Achievements
	private AchievementRankIcon rankIcon;
	[SerializeField] private GameObject rankIconAnchor;
	[HideInInspector] public bool isSetup = false;

	private bool _isEnabled = false;

	private const string OPEN_PROFILE_AUDIO = "EnterProfileAreaNetworkAchievements";
	public bool isEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			_isEnabled = value;
			if (profileRenderer.material.HasProperty("_Monochrome"))
			{
				profileRenderer.material.SetFloat("_Monochrome", _isEnabled ? 0.0f : 1.0f);
			}
			else
			{
				Debug.LogWarningFormat("NetworkProfileOverlayButton.cs -- isEnabled -- material does not have monochrome property");
			}
			buttonHandler.isEnabled = _isEnabled;
		}
	}

	void OnDestroy()
	{
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			SlotsPlayer.instance.socialMember.onMemberUpdated -= refreshMember;
		}
	}

	void Awake()
	{
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			SlotsPlayer.instance.socialMember.onMemberUpdated -= refreshMember;
			SlotsPlayer.instance.socialMember.onMemberUpdated += refreshMember;
		}
	}
	
	public void init()
	{
		if (!isSetup)
		{
			StatsManager.Instance.LogCount("main_lobby", "ll_profile", "lobby_hud", "view");
		}

		if (SlotsPlayer.instance.socialMember == null)
		{
			Debug.LogErrorFormat("NetworkProfileOverlayButton.cs -- init() -- member was null!");
		}
		fbInfo.member = SlotsPlayer.instance.socialMember;

		if (buttonHandler != null)
		{
			buttonHandler.registerEventDelegate(onButtonClick);
		}
		else
		{
			Debug.LogWarningFormat("NetworkProfileOverlayButton.cs -- init -- buttonHandler is null, this should not be null");
		}

		if (rankIconAnchor != null)
		{
			//AchievementRankIcon.loadRankIconToAnchor(rankIconAnchor, SlotsPlayer.instance.socialMember);
		}
		
		isSetup = true;
	}
	

	public void refreshMember(SocialMember member)
	{
		if (rankIconAnchor != null && rankIcon == null)
		{
			rankIcon = rankIconAnchor.GetComponentInChildren<AchievementRankIcon>();
		}
		
		if (rankIcon != null)
		{
			rankIcon.setRank(member);
		}
	}

	private void onButtonClick(Dict args = null)
	{
		// Open the player's profile.
		Audio.play(OPEN_PROFILE_AUDIO);
		NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember);
		StatsManager.Instance.LogCount("main_lobby", "ll_profile", "lobby_hud", "click");
	}
}
