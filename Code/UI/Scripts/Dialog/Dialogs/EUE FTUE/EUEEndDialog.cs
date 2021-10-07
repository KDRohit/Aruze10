using System.Collections;
using System.Collections.Generic;
using TMPro;

using UnityEngine;

namespace Com.HitItRich.EUE
{
	public class EUEEndDialog : DialogBase
	{
		[SerializeField] private ButtonHandler acceptButton;
		[SerializeField] private MultiLabelWrapperComponent textLabel;
		[SerializeField] private EUECharacterItem character;

		private bool slotventuresActive = false;

		public override void init()
		{
			acceptButton.registerEventDelegate(onClick);
			
			SlotventuresChallengeCampaign campaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;
			slotventuresActive = campaign != null && ExperimentWrapper.Slotventures.isEUE && !campaign.isComplete;

			if (character != null)
			{
				if (slotventuresActive)
				{
					character.setText("ftue_end_slotventure");
				}
				else
				{
					character.setText("ftue_end_normal");
				}	
			}
			
			StatsManager.Instance.LogCount("game_actions", "machine_dialog", "", "", slotventuresActive ? "eue_sv" : "generic", "view");
		}

		public override void close()
		{
		}

		private void onClick(Dict args)
		{
			Dialog.close();
			
			if (slotventuresActive)
			{
				//if we need to load slotventures
				if (SlotventuresLobby.isBeingLazilyLoaded)
				{
					Glb.resetGameAndLoadBundles(string.Format("Load {0} now", "slotventures_lobby"),  new List<string>() {SlotventuresLobby.COMMON_BUNDLE_NAME , SlotventuresLobby.THEMED_BUNDLE_NAME, SlotventuresLobby.COMMON_BUNDLE_NAME_SOUNDS },  SlotventuresLobby.onReload);
				}
				else
				{
					//reset the game so we dont' have to do a double transition
					LobbyLoader.lobbyLoadEvent += SlotventuresLobby.onReload;
					Glb.resetGame("slotventures_lobby");
				}
			}
			StatsManager.Instance.LogMileStone("ftue", "exit_machine_ftue");
			StatsManager.Instance.LogCount("game_actions", "machine_dialog", "", "", slotventuresActive ? "eue_sv" : "generic", "click");
		}
	}	
}

