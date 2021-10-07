using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace QuestForTheChest
{
	public class QFCToaster : Toaster
	{
		// =============================
		// PUBLIC
		// =============================
		public enum TOASTER_SUB_TYPE
		{
			CONTEST_ENDED,  
			KEYS_AWARDED,  
			TEAM_LEAD,   
			ROUND_COMPLETE,  
			CONTEST_ENDING,  
			KEYS_TO_WIN 
		}

		// =============================
		// PRIVATE
		// =============================
		private const string QFC_TOASTER_TEAM_TAKEN_LEAD_SOUND = "QfcToasterTeamTakenLead";
		private const string QFC_TOASTER_TEAMMATE_NEED_SOUND = "QfcToasterTeammateNeed";
		private const string QFC_TOASTER_TEAMMATE_FOUND_KEY_SOUND = "QfcToasterTeammateFoundKeys";
		private const string QFC_TOASTER_TEAMMATE_END_SOUND = "QfcToasterTeammateEnd";
		private const string QFC_TOASTER_OPPONENT_TAKEN_LEAD_SOUND = "QfcToasterOpponentTakenLead";
		private const string QFC_TOASTER_EVENT_ENDING_SOUND = "QfcToasterEventEnding";
		private const string QFC_TOASTER_EVENT_OVER_SOUND = "QfcToasterEventOver";

		[SerializeField] private TextMeshPro buttonLabel;
		[SerializeField] private TextMeshPro keyNumLabel;

		[SerializeField] private ClickHandler barClickHandler;
		[SerializeField] private UISprite backgroundSprite;
		[SerializeField] private UISprite logoSprite;
		[SerializeField] private GameObject stopwatch;
		[SerializeField] private FacebookFriendInfo profileInfo;
		[SerializeField] private GameObject playerPic;
		[SerializeField] private GameObject key;
		[SerializeField] private GameObject chest;

		private UIAtlas themedAtlas;
		private SocialMember member {get; set;}
		private string facebookName = "";
		private string message = "";
		private TOASTER_SUB_TYPE subType = 0;
		private string zid = "";
		private int keysWon = 0;
		private int keysNeed = 0;
		private QFCTeams team;
		private bool lead;
		private string profileURL = "";
		private int timeLeft = 0; //in seconds

		// =============================
		// CONST
		// =============================
		private const string PANEL_ORANGE = "Toaster Panel Orange 00 Stretchy";
		private const string PANEL_RED = "Toaster Panel Red 00 Stretchy";
		private const string PANEL_BLUE = "Toaster Panel Blue 00 Stretchy";

		public override void init(ProtoToaster proto)
		{
			if (null != proto.args)
			{
				subType = (TOASTER_SUB_TYPE)proto.args.getWithDefault(D.OPTION, TOASTER_SUB_TYPE.CONTEST_ENDED);

				zid = (string) proto.args.getWithDefault(D.PLAYER, "-1");
				member = SocialMember.findByZId(zid);
				if(member != null)
				{
					profileInfo.member = member;
				}
				setQFCPLayerByZid(zid);
				
				keysWon = (int)proto.args.getWithDefault(D.VALUE, 0);
				lead = (bool) proto.args.getWithDefault(D.KEY, false);
				keysNeed = (int)proto.args.getWithDefault(D.KEYS_NEED, 0);
				timeLeft = (int)proto.args.getWithDefault(D.TIME_LEFT, 0);
				
			}

			setMessageText(zid, keysWon, 0, subType, timeLeft);  
			setBackgroungSprite(subType);
			setIcons(subType);
			playSound(subType);
			AssetBundleManager.load(this, string.Format(QuestForTheChestFeature.THEMED_ATLAS_PATH, ExperimentWrapper.QuestForTheChest.theme), assetLoadSuccess, assetLoadFailed, isSkippingMapping:true, fileExtension:".prefab");

			if (barClickHandler != null)
			{
				barClickHandler.registerEventDelegate(viewMapClicked);	
			}

			base.init(proto);
		}

		private void setQFCPLayerByZid(string zid)
		{
			if (string.IsNullOrEmpty(zid) || zid == "-1")
			{
				facebookName = "";
				return;
			}
			
			team = QuestForTheChestFeature.instance.getTeamForPlayer(zid);
			Dictionary<string, QFCPlayer> teamDict = QuestForTheChestFeature.instance.getTeamMembersAsPlayerDict(team);
			if (teamDict.ContainsKey(zid))
			{	
				QFCPlayer player = teamDict[zid];

				if (member == null)
				{
					//Just use player name from the server if the SocialMember is null
					facebookName = player.name;
				}
				else
				{
					//Using the same name being used in the Profiles dialog and defaulting to the server given name if those aren't available
					if (member.networkProfile != null && !string.IsNullOrEmpty(member.networkProfile.name))
					{
						facebookName = player.member.networkProfile.name;
					}
					else if (!string.IsNullOrEmpty(member.fullName))
					{
						facebookName = player.member.firstNameLastInitial;
					}
					else
					{
						facebookName = player.name;
					}
				}
			}
		}

		private void setIcons(TOASTER_SUB_TYPE subType)
		{
			switch (subType)
			{
				case TOASTER_SUB_TYPE.CONTEST_ENDED:
					stopwatch.SetActive(true);
					playerPic.SetActive(false);
					key.SetActive(false);
					chest.SetActive(false);
					break;
				case TOASTER_SUB_TYPE.CONTEST_ENDING:
					stopwatch.SetActive(true);
					playerPic.SetActive(false);
					key.SetActive(false);
					chest.SetActive(false);
					break;
				case TOASTER_SUB_TYPE.KEYS_AWARDED:
					stopwatch.SetActive(false);
					playerPic.SetActive(true);    
					key.SetActive(false);
					chest.SetActive(false);
					break;
				case TOASTER_SUB_TYPE.TEAM_LEAD:
					stopwatch.SetActive(false);
					playerPic.SetActive(false);
					key.SetActive(false);
					chest.SetActive(true);
					break;
				case TOASTER_SUB_TYPE.ROUND_COMPLETE:
					stopwatch.SetActive(false);
					playerPic.SetActive(true);  
					key.SetActive(false);
					chest.SetActive(false);
					break;
				case TOASTER_SUB_TYPE.KEYS_TO_WIN:
					stopwatch.SetActive(false);
					playerPic.SetActive(false);
					key.SetActive(true);
					chest.SetActive(false);
					break;
				default:
					break;
			}
		}

		private void setBackgroungSprite(TOASTER_SUB_TYPE subType)
		{
			backgroundSprite.spriteName = PANEL_BLUE;
			switch (subType)
			{
				case TOASTER_SUB_TYPE.CONTEST_ENDED:
				case TOASTER_SUB_TYPE.CONTEST_ENDING:
					backgroundSprite.spriteName = PANEL_ORANGE; 
					break;
				case TOASTER_SUB_TYPE.TEAM_LEAD:
					if(!lead)  
					{
						backgroundSprite.spriteName = PANEL_RED;
					}
					break;
				default:
					break;
			}
		}

		private void playSound(TOASTER_SUB_TYPE subType)
		{
			string suffix = ExperimentWrapper.QuestForTheChest.theme;
			switch (subType)
			{
				case TOASTER_SUB_TYPE.CONTEST_ENDED:
					Audio.play(QFC_TOASTER_EVENT_OVER_SOUND + suffix);
					break;
				case TOASTER_SUB_TYPE.CONTEST_ENDING:
					Audio.play(QFC_TOASTER_EVENT_ENDING_SOUND + suffix);
					break;
				case TOASTER_SUB_TYPE.KEYS_AWARDED:
					Audio.play(QFC_TOASTER_TEAMMATE_FOUND_KEY_SOUND + suffix);
					break;
				case TOASTER_SUB_TYPE.KEYS_TO_WIN:
					Audio.play(QFC_TOASTER_TEAMMATE_NEED_SOUND + suffix);
					break;
				case TOASTER_SUB_TYPE.TEAM_LEAD:
					if (lead)
					{
						Audio.play(QFC_TOASTER_TEAM_TAKEN_LEAD_SOUND + suffix);
					}
					else
					{
						Audio.play(QFC_TOASTER_OPPONENT_TAKEN_LEAD_SOUND + suffix);
					}
					break;
				case TOASTER_SUB_TYPE.ROUND_COMPLETE:
					Audio.play(QFC_TOASTER_TEAMMATE_END_SOUND + suffix);
					break;
				default:
					break;
			}
		}

		private void setMessageText(string zid, int keys, int position, TOASTER_SUB_TYPE subType, int timeLeft)
		{
			switch (subType)
			{
				case TOASTER_SUB_TYPE.CONTEST_ENDED:
					buttonLabel.text = Localize.text("event_has_ended");
					break;
				case TOASTER_SUB_TYPE.CONTEST_ENDING: 
					System.TimeSpan t = System.TimeSpan.FromSeconds(timeLeft);
					StringBuilder timeLeftString = new StringBuilder();
					if(t.Days != 0)
					{
						timeLeftString.AppendFormat("{0} {1}", t.Days, t.Days == 1 ? "day" : "days");
					}
					if(t.Hours != 0)
					{
						timeLeftString.AppendFormat("{0} {1}", t.Hours, t.Hours == 1 ? "hour" : "hours");
					}
					if(t.Minutes != 0)
					{
						timeLeftString.AppendFormat("{0} {1}", t.Minutes, t.Minutes == 1 ? "minute" : "minutes");
					}

					buttonLabel.text = Localize.text("event_ends_in", timeLeftString.ToString());       
					break;
				case TOASTER_SUB_TYPE.KEYS_AWARDED:
					if (keys == 1)
					{
						buttonLabel.text = Localize.text("teammate_found_key", facebookName, keys);  
					}else
					{
						buttonLabel.text = Localize.text("teammate_found_keys", facebookName, keys);  
					}
					break;
				case TOASTER_SUB_TYPE.TEAM_LEAD:
					if(lead == true)  
					{
						buttonLabel.text = Localize.text("home_team_taken_lead");
					}else
					{
						buttonLabel.text = Localize.text("away_team_taken_lead");
					}
					break;
				case TOASTER_SUB_TYPE.ROUND_COMPLETE:
					buttonLabel.text = Localize.text("teammate_reach_end_bonus", facebookName);  
					break;
				case TOASTER_SUB_TYPE.KEYS_TO_WIN:
					buttonLabel.text = Localize.text("team_keys_to_win", keysNeed); 
					keyNumLabel.text = Localize.text(keysNeed.ToString());  
					break;
				default:
					break;
			}
		}

		public override void close()
		{
			barClickHandler.clearAllDelegates();
			base.close();
			QuestForTheChestFeature.instance.onToasterClose();
		}

		//Click handler that opens the map dialog
		private void viewMapClicked(Dict args = null)
		{
			QFCMapDialog.showDialog();
			close();
		}

		private void assetLoadSuccess(string assetPath, Object obj, Dict data = null)
		{
			themedAtlas = ((GameObject)obj).GetComponent<UIAtlas>();
			logoSprite.atlas = themedAtlas;
			logoSprite.spriteName = "Logo Toaster";
		}

		private void assetLoadFailed(string assetPath, Dict data = null)
		{
			Debug.Log("QFC Themed Asset failed to load: " + assetPath);
		}
	}
}

