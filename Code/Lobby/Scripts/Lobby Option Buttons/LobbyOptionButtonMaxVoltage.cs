using UnityEngine;
using System.Collections;
using TMPro;

public class LobbyOptionButtonMaxVoltage : LobbyOptionButtonActive
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private Animator animator;
	[SerializeField] private GameObject lockParent;
	[SerializeField] private TextMeshPro lockText;
	
	// =============================
	// CONST
	// =============================
	private const string IDLE_STATE = "LobbyGameCardIdle";
	private const string PRESS_STATE = "LobbyGameCardPressed";

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);

		lockParent.SetActive(SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL);
		lockText.text =  Glb.MAX_VOLTAGE_MIN_LEVEL.ToString();
		playIdle();
	}

	protected override void OnClick()
	{
		//abort if user isn't high enough level
		if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL)
		{
			return;
		}

		Audio.play("MVTransitionIn2Game");
		SlotGameData data = SlotGameData.find(option.game.keyName);
		StatsManager.Instance.LogCount
		(
			"lobby",
			"select_game",
			option.game.keyName,
			data == null ? "" : data.zTrackString,
			"",
			"click",
			1,
			"max_voltage"
		);
		
		animator.StopPlayback();
		base.OnClick();
	}
	
	protected override void OnPress()
	{
		animator.Play(PRESS_STATE);
	}

	public void playIdle()
	{
		animator.Play(IDLE_STATE);
	}
}