using UnityEngine;
using System.Text;
using QuestForTheChest;
using System.Collections.Generic;
using Zynga.Core.Util;
using Random = UnityEngine.Random;

class DevGUIMenuQuestForTheChest : DevGUIMenu
{
	private StringBuilder homeTeamMembers;
	private StringBuilder awayTeamMembers;
	private StringBuilder boardInfo;
	
	private bool fakeOn = false;
	private Dictionary<string, QFCPlayer> homeTeam;
	private Dictionary<string, QFCPlayer> awayTeam;
	private List<QFCBoardNode> boardNodes;
	private string zidToIncrement = "";
	private int tokensToAward = 1;
	private int nodesToAdvance = 0;

	private HashSet<string> usedNames;
	private HashSet<string> usedZids;

	public const int maximumLogCount = 50;
	public static RollingList<JSON> chestRewardEvents;
	public static RollingList<JSON> tokenRewardEvents;
	public static RollingList<JSON> nodeRewardEvents;
	public static RollingList<JSON> playerProgressEvents;
	public static RollingList<JSON> raceCompleteEvents;
	private static int raceHistoryIndex = 0;
	private static int fakeRaceIndex = 0;
	
	public static bool isDirty = false;

	private void drawRollingList(RollingList<JSON> logItems)
	{
		if (logItems != null && logItems.Count > 0)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Node Reward Events: ");
			GUILayout.EndHorizontal();

			for (int i = logItems.Count - 1; i >= 0; --i)
			{
				GUILayout.BeginHorizontal();
				GUILayout.TextArea(logItems[i].ToString());
				GUILayout.EndHorizontal();
			}
		}
	}
	
	public override void drawGuts()
	{
		PreferencesBase prefs = SlotsPlayer.getPreferences();
		GUILayout.BeginHorizontal();

		string fakeOnOffButtonText = "ON/OFF";
		if (QuestForTheChestFeature.instance.isEnabled && !fakeOn)
		{
			fakeOnOffButtonText = "Use fake events";
		}
		else if (QuestForTheChestFeature.instance.isEnabled)
		{
			fakeOnOffButtonText = "Use server events";
		}
		if (GUILayout.Button(fakeOnOffButtonText))
		{
			if (QuestForTheChestFeature.instance.isEnabled && fakeOn)
			{
				//TODO: call qfcaction to get current race info
			}

			fakeOn = !fakeOn;
			if (fakeOn)
			{
				//generate a new event
				generateFakeRaceInfoEvent();
				isDirty = true;
			}
		}
		
		GUILayout.EndHorizontal();
		
		if (QuestForTheChestFeature.instance.isEnabled || fakeOn)
		{
			if (QuestForTheChestFeature.instance.isRaceComplete)
			{
				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Start Next Race"))
				{
					if (fakeOn)
					{
						generateFakeRaceInfoEvent();
						isDirty = true;
					}
					else
					{
						QuestForTheChestFeature.instance.consumeRaceComplete("");
					}
				}

				GUILayout.EndHorizontal();
			}
			else
			{
				if (GUILayout.Button("Show Map Dialog"))
				{
					QFCMapDialog.showDialog();
					DevGUI.isActive = false;
				}
				
				if (GUILayout.Button("Fake Bonus Game"))
				{
					fakeBonusGame();
					DevGUI.isActive = false;
				}
				
				if (GUILayout.Button("Get Full Updated Board Info"))
				{
					QFCAction.getCurrentRaceInformation(QuestForTheChestFeature.instance.competitionId, QuestForTheChestFeature.instance.raceIndex);
				}
				GUILayout.BeginHorizontal();
				GUILayout.Label("Zid: ");
				zidToIncrement = GUILayout.TextField(zidToIncrement);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Number of Tokens: ");
				string tokenString = tokensToAward.ToString();
				tokenString = GUILayout.TextField(tokenString);
				tokensToAward = System.Convert.ToInt32(tokenString);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Nodes to Advance: ");
				string nodeString = nodesToAdvance.ToString();
				nodeString = GUILayout.TextField(nodeString);
				nodesToAdvance = System.Convert.ToInt32(nodeString);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Update Player Progress"))
				{
					try
					{
						if (!string.IsNullOrEmpty(zidToIncrement))
						{
							generateFakePlayerProgressEvent(zidToIncrement, tokensToAward, nodesToAdvance);
							isDirty = true;
						}
					}
					catch(System.Exception e)
					{
						Debug.LogError("Can't use zid: " + zidToIncrement + ", " + e.ToString());
					}
				}
				if (GUILayout.Button("Complete Round"))
				{
					try
					{
						if (!string.IsNullOrEmpty(zidToIncrement))
						{
							generateFakeCompleteRoundEvent(zidToIncrement);
							isDirty = true;
						}
					}
					catch(System.Exception e)
					{
						Debug.LogError("Can't use zid: " + zidToIncrement + ", " + e.ToString());
					}
				}

				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Current User Events: ");
				GUILayout.EndHorizontal();


				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Race Complete Event (win)"))
				{
					generateFakeRaceCompleteEvent(true);
					isDirty = true;
				}

				if (GUILayout.Button("Race Complete Event (lose)"))
				{
					generateFakeRaceCompleteEvent(false);
					isDirty = true;
				}

				if (GUILayout.Button("Chest Reward Event"))
				{
					generateFakeChestRewardEvent();
					isDirty = true;
				}

				if (GUILayout.Button("Token Reward Event"))
				{
					generateFakeTokenRewardEvent();
					isDirty = true;
				}

				if (GUILayout.Button("Node Reward Event"))
				{
					generateFakeNodeRewardEvent();
					isDirty = true;
				}

				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Toaster Specific");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Contest Complete"))
				{
					generateFakeContestCompleteEvent();
					isDirty = true;
				}

				if (GUILayout.Button("Multiple Contest Ends in X"))
				{
					Debug.Log("parse button is pressed");
					generateFakeContestEndingEvent();
					isDirty = true;
				}

				if (GUILayout.Button("Multiple X Keys needed to win"))
				{
					generateFakeKeysNeededEvent();
					isDirty = true;
				}

				GUILayout.EndHorizontal();
			}

			drawRollingList(chestRewardEvents);
			drawRollingList(tokenRewardEvents);
			drawRollingList(nodeRewardEvents);
			drawRollingList(playerProgressEvents);
			drawRollingList(raceCompleteEvents);

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			if (homeTeam == null || isDirty)
			{
				homeTeamMembers = new StringBuilder("Home Team\n");
				homeTeam = QuestForTheChestFeature.instance.getTeamMembersAsPlayerDict(QFCTeams.HOME);
					
				//TEMP: remove this once actual data is hooked up
				if (homeTeam.Count == 0)
				{
					Debug.LogError("Invalid home team");
				}

				foreach (KeyValuePair<string, QFCPlayer> kvp in homeTeam)
				{
					homeTeamMembers.AppendLine("ZID: " + kvp.Key);
					homeTeamMembers.AppendLine("Name: " + kvp.Value.name);
					homeTeamMembers.AppendLine("Tokens: " + kvp.Value.keys);
					homeTeamMembers.AppendLine("Round: " + kvp.Value.round);
					homeTeamMembers.AppendLine("Position: " + kvp.Value.position + "\n");
				}
			}

			GUILayout.TextArea(homeTeamMembers.ToString());

			GUILayout.EndVertical();
				
			GUILayout.BeginVertical();

			if (awayTeam == null || isDirty)
			{
				awayTeamMembers = new StringBuilder("Away Team\n");
				awayTeam = QuestForTheChestFeature.instance.getTeamMembersAsPlayerDict(QFCTeams.AWAY);
					
				//TEMP: remove this once actual data is hooked up
				if (awayTeam.Count == 0)
				{
					Debug.LogError("invalid away team");
				}

				foreach (KeyValuePair<string, QFCPlayer> kvp in awayTeam)
				{
					awayTeamMembers.AppendLine("ZID: " + kvp.Key);
					awayTeamMembers.AppendLine("Name: " + kvp.Value.name);
					awayTeamMembers.AppendLine("Tokens: " + kvp.Value.keys);
					awayTeamMembers.AppendLine("Round: " + kvp.Value.round);
					awayTeamMembers.AppendLine("Position: " + kvp.Value.position + "\n");
				}
			}

			GUILayout.TextArea(awayTeamMembers.ToString());
			GUILayout.EndVertical();
				
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			if (boardNodes == null || isDirty)
			{
				boardInfo = new StringBuilder();
				boardNodes = QuestForTheChestFeature.instance.nodeData;
				if (boardNodes != null)
				{
					for (int i = 0; i < boardNodes.Count; i++)
					{
						QFCBoardNode currentNode = boardNodes[i];
						boardInfo.AppendLine("Node Index: " + i);

						boardInfo.AppendLine("Node Transform: " + currentNode.position);
						if (currentNode.rewardMultiplier > 0)
						{
							boardInfo.AppendLine("Reward Multiplier: " + currentNode.rewardMultiplier);
						}

						if (!currentNode.storyLocalizationBody.IsNullOrWhiteSpace())
						{
							boardInfo.AppendLine("Story Localization: " + Localize.text(currentNode.storyLocalizationBody));
						}

						if (currentNode.occupiedBy != null)
						{
							for (int playerIndex = 0; playerIndex < currentNode.occupiedBy.Count; playerIndex++)
							{
								QFCPlayer occupiedPlayer;
								if (homeTeam.TryGetValue(currentNode.occupiedBy[playerIndex], out occupiedPlayer))
								{
									boardInfo.AppendLine("Player: " + occupiedPlayer.name + " / " + currentNode.occupiedBy[playerIndex]);
								}
								else if (awayTeam.TryGetValue(currentNode.occupiedBy[playerIndex], out occupiedPlayer))
								{
									boardInfo.AppendLine("Player: " + occupiedPlayer.name + " / " + currentNode.occupiedBy[playerIndex]);
								}
							}
						}
						boardInfo.AppendLine();
					}
				}
			}
			
			GUILayout.TextArea(boardInfo.ToString());
			GUILayout.EndHorizontal();

			if (isDirty)
			{
				isDirty = false;
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Prev Race"))
			{
				--raceHistoryIndex;
				if (raceHistoryIndex < 0)
				{
					raceHistoryIndex = 0;
				}
			}

			GUILayout.TextArea(getRaceHistory(raceHistoryIndex),
				new GUILayoutOption[]
				{
					GUILayout.Height(250),
					GUILayout.Width(400)
				});

			if (GUILayout.Button("Next Race"))
			{
				++raceHistoryIndex;
				if (raceHistoryIndex >= QuestForTheChestFeature.instance.numSavedRaces )
				{
					if (QuestForTheChestFeature.instance.numSavedRaces > 0)
					{
						raceHistoryIndex = QuestForTheChestFeature.instance.numSavedRaces - 1;
					}
					else
					{
						raceHistoryIndex = 0;
					}

				}

			}
			GUILayout.EndHorizontal();
		}
		else
		{
			GUILayout.BeginHorizontal();
			//Add a final check here to see if we aren't currently on a team
			GUILayout.TextArea("Feature isn't active because: " + System.Environment.NewLine + QuestForTheChestFeature.instance.getInactiveReason());

			GUILayout.EndHorizontal();
		}

	}

	private static string getRaceHistory(int index)
	{
		QFCRaceData data = QuestForTheChestFeature.instance.getOldRaceData(index);
		if (data == null)
		{
			//Debug.LogError("Invalid data for index: " + index);
			return "";
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Race Index: " + data.index);
		sb.AppendLine("Home Team Key Total: " + data.homeKeyTotal);
		sb.AppendLine("Away Team Key Total: " + data.awayKeyTotal);

		//TODO: put in player positions
		return sb.ToString();
	}

	private void generateFakeRaceInfoEvent()
	{
		//reset used zids
		clearRandomZids();

		//reset events
		clearEvents();

		//reset display text;
		homeTeam = null;
		awayTeam = null;

		int totalNodes = 13;

		StringBuilder json = new StringBuilder();
		json.AppendLine("{");
		json.AppendLine("\t\"type\" : \":qfc_race_info\",");
		json.AppendLine("\t\"start_time\" : " + System.DateTime.UtcNow.Ticks + ",");
		json.AppendLine("\t\"end_time\" :  " + (System.DateTime.UtcNow.AddSeconds(60 * 60)).Ticks + ",");
		json.AppendLine("\t\"tokens_required\" : " + Random.Range(20, 100) + ",");
		json.AppendLine("\t\"total_nodes\" : " + totalNodes + ",");
		json.AppendLine("\t\"race_index\" : " + (fakeRaceIndex++) + ",");
		json.AppendLine("\t\"home_team\" : {");

		//make current user player 0
		json.AppendLine("\t\t\"" + SlotsPlayer.instance.socialMember.zId + "\" : { ");
		json.AppendLine("\t\t\t\"name\" : \"" + SlotsPlayer.instance.socialMember.fullName + "\",");
		json.AppendLine("\t\t\t\"photo\" : \"" + SlotsPlayer.instance.socialMember.getImageURL + "\",");
		json.AppendLine("\t\t\t\"current_node\" : " + Random.Range(0, totalNodes) + ",");
		json.AppendLine("\t\t\t\"token_count\" : " + Random.Range(0, 100));
		json.AppendLine("\t\t}");

		for (int i = 1; i < 4; ++i)
		{
			json.AppendLine(",");
			json.AppendLine("\t\t\"" + getRandomUniqueZid() + "\" : {");
			json.AppendLine("\t\t\t\"name\" : \"" + getRandomUniqueName() + "\",");
			json.AppendLine("\t\t\t\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("\t\t\t\"current_node\" : " + Random.Range(0, totalNodes) + ",");
			json.AppendLine("\t\t\t\"token_count\" : " + Random.Range(0, 100));
			json.AppendLine("\t\t}");
		}

		json.AppendLine("\t},");
		json.AppendLine("\t\"away_team\" : {");

		for (int i = 0; i < 4; ++i)
		{
			if (i != 0)
			{
				json.AppendLine(",");
			}
			json.AppendLine("\t\t\"" + getRandomUniqueZid() + "\" : {");
			json.AppendLine("\t\t\t\"name\" : \"" + getRandomUniqueName() + "\",");
			json.AppendLine("\t\t\t\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("\t\t\t\"current_node\" : " + Random.Range(0, totalNodes) + ",");
			json.AppendLine("\t\t\t\"token_count\" : " + Random.Range(0, 100));
			json.AppendLine("\t\t}");
		}

		json.AppendLine("\t},");
		json.AppendLine("\t\"node_rewards\" :{");
		for (int i = 0; i < totalNodes; i++)
		{
			if (i != 0)
			{
				json.AppendLine(",");
			}

			int rewardMultiplier = i % 4 == 0 ? 2 : 0;
			json.AppendLine("\t\"" + i + "\" : " + rewardMultiplier);
		}
		json.AppendLine("\t}");
		
		json.AppendLine("}");

		QuestForTheChestFeature.instance.onNewRace(new JSON(json.ToString()));
	}

	private void generateFakeProgressEvent()
	{
		StringBuilder json = new StringBuilder();
		int tokensWon = Random.Range(1, 6);
		int newNode = 0;
		if (homeTeam.ContainsKey(SlotsPlayer.instance.socialMember.zId))
		{
			newNode = homeTeam[SlotsPlayer.instance.socialMember.zId].position + 1;
		}
		else
		{
			Debug.LogError("Current player doesn't exist in home team");
			return;
		}
		
		json.AppendLine("{");
		json.AppendLine("	\"type\" : \":qfc_player_progress\",");
		json.AppendLine("	\"tokens_won\" :" + tokensWon + ",");
		json.AppendLine("	\"new_node\" :" + newNode + ",");
		json.AppendLine("	\"creation_time\" : " + GameTimer.currentTime);

		json.AppendLine("}");

		QuestForTheChestFeature.instance.onPlayerProgress(new JSON(json.ToString()));

	}
	
	private static void generateFakeChestRewardEvent()
	{
		StringBuilder json = new StringBuilder();
		
		json.AppendLine("{");
		json.AppendLine("	\"type\" : \":qfc_race_chest\",");
		json.AppendLine("	\"team_coin_reward\" :" + 500 + ",");
		json.AppendLine("	\"rewards\" : [");
		json.AppendLine("		{");
		json.AppendLine("			\"type\" : \":coin\",");
		json.AppendLine("			\"value\" :" + 100 );
		json.AppendLine("		}");
		json.AppendLine("	]");
		json.AppendLine("}");

		QuestForTheChestFeature.instance.onChestAwarded(new JSON(json.ToString()));
	}
	
	private void generateFakeRaceCompleteEvent(bool homeTeamWon)
	{
		StringBuilder json = new StringBuilder();
		string winningZid = "";
		if (homeTeamWon)
		{
			List<SocialMember> homeTeam = QuestForTheChestFeature.instance.getTeamMembersAsSocialMembers(QFCTeams.HOME);
			winningZid = homeTeam[Random.Range(0, homeTeam.Count-1)].zId;
		}
		else
		{
			List<SocialMember> awayTeam = QuestForTheChestFeature.instance.getTeamMembersAsSocialMembers(QFCTeams.AWAY);
			winningZid = awayTeam[Random.Range(0, homeTeam.Count-1)].zId;
		}
		json.AppendLine("{");
		json.AppendLine("	\"type\" : \":qfc_race_complete\",");
		json.AppendLine("	\"has_won\" :" + homeTeamWon.ToString().ToLower() + ",");
		json.AppendLine("	\"race_index\" :" + QuestForTheChestFeature.instance.raceIndex + ",");
		json.AppendLine("	\"winner_zid\" :\"" + winningZid + "\"");
		json.AppendLine("}");

		QuestForTheChestFeature.instance.onRaceComplete(new JSON(json.ToString()));
	}
	
	private void generateFakeContestCompleteEvent()
	{
		StringBuilder json = new StringBuilder();
		
		json.AppendLine("{");
		json.AppendLine("	\"type\" : \":qfc_contest_complete\",");
		json.AppendLine("}");

		QuestForTheChestFeature.instance.onContestComplete(new JSON(json.ToString()));
	}

	private void generateFakeContestEndingEvent()
	{
		string toasterTimeTriggerData = ExperimentWrapper.QuestForTheChest.toasterTimeTrigger; 

		if (string.IsNullOrEmpty(toasterTimeTriggerData))
		{
			Debug.LogWarning("No q4c toaster time trigger data");
		}
		else
		{
			string[] toasterTimeTriggeStrings = toasterTimeTriggerData.Split(',');
			//triggers toaster contest ending in X events
			for (int i = 0; i < toasterTimeTriggeStrings.Length; i++)
			{
				float timerTrigger = 0.0f;
				//use this button to test all time trigger cases on EOS
				if(float.TryParse(toasterTimeTriggeStrings[i], out timerTrigger))  
				{
					int timerTriggerSeconds = (int)(timerTrigger * 60 * 60);
					Dict args = Dict.create(D.OPTION, QFCToaster.TOASTER_SUB_TYPE.CONTEST_ENDING, D.TIME_LEFT, timerTriggerSeconds);  
					QuestForTheChestFeature.instance.onContestEndingSoon(args); 
				} 
			}
		}
	}

	private void generateFakeKeysNeededEvent()
	{
		string toasterKeyTriggerData = ExperimentWrapper.QuestForTheChest.toasterKeyTrigger; 
		if (string.IsNullOrEmpty(toasterKeyTriggerData))
		{
			Debug.LogWarning("No q4c toaster key trigger data");
		}
		else
		{
			string[] toasterKeyTriggeStrings = toasterKeyTriggerData.Split(',');
			for (int i = 0; i < toasterKeyTriggeStrings.Length; i++)
			{
				int keyNumTrigger = 0;
				//use this button to test all key trigger cases on EOS
				if(int.TryParse(toasterKeyTriggeStrings[i], out keyNumTrigger))
				{
					QuestForTheChestFeature.instance.showKeysToWinToaster(keyNumTrigger);
				}
			}
		}	
	}

	private static void generateFakeTokenRewardEvent()
	{
		StringBuilder json = new StringBuilder();
		
		json.AppendLine("{");
		json.AppendLine("	\"type\" : \":qfc_token_reward\",");
		json.AppendLine("	\"rewards\" : [");
		json.AppendLine("		{");
		json.AppendLine("			\"type\" : \":coin\",");
		json.AppendLine("			\"value\" :" + 2000 );
		json.AppendLine("		}");
		json.AppendLine("	]");
		json.AppendLine("}");
		QuestForTheChestFeature.instance.onKeyReward(new JSON(json.ToString()));
	}
	
	private static void generateFakeNodeRewardEvent()
	{
		StringBuilder json = new StringBuilder();
		
		json.AppendLine("{");
		json.AppendLine("	\"type\" : \":qfc_node_reward\",");
		json.AppendLine("	\"rewards\" : [");
		json.AppendLine("		{");
		json.AppendLine("			\"type\" : \":coin\",");
		json.AppendLine("			\"value\" :" + 100 );
		json.AppendLine("		}");
		json.AppendLine("	]");
		json.AppendLine("}");
		QuestForTheChestFeature.instance.onNodeReward(new JSON(json.ToString()));
	}

	private static void generateFakePlayerProgressEvent(string zid, int tokens, int numNodes)
	{
		//update position
		int newPosition = QuestForTheChestFeature.instance.getPositionForPlayer(zid);
		if (numNodes > 0)
		{
			newPosition += numNodes;
			if (newPosition >= QuestForTheChestFeature.instance.maxNode)
			{
				newPosition = 0;
			}
		}

		//check if lead changed
		bool? newLead = null;
		if (tokens > 0)
		{
			int awayScore = QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY);
			int homeScore = QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.HOME);

			QFCTeams team = QuestForTheChestFeature.instance.getTeamForPlayer(zid);
			if (team == QFCTeams.AWAY && (homeScore - awayScore) > 0 && (homeScore - awayScore) < tokens)
			{
				newLead = false;
			}
			else if (team == QFCTeams.HOME && (awayScore - homeScore) > 0 && (awayScore - homeScore < tokens))
			{
				newLead = true;
			}
		}

		StringBuilder json = new StringBuilder();
		json.AppendLine("{");
		json.AppendLine("\t\"type\" : \"qfc_player_progress\",");
		json.AppendLine("\t\"zid\" : \"" + zid + "\",");
		json.AppendLine("\t\"tokens_won\" : " + tokens + ",");
		json.AppendLine("\t\"new_node\": " + newPosition);
		if (newLead.HasValue)
		{
			json.AppendLine("\t\"lead\" : " + newLead.Value);
		}
		json.AppendLine("}");

		QuestForTheChestFeature.instance.onPlayerProgress(new JSON(json.ToString()));
	}

	private static void generateFakeCompleteRoundEvent(string zid)
	{
		StringBuilder json = new StringBuilder();

		json.AppendLine("{");
		json.AppendLine("\t\"type\" : \":qfc_toaster_round_complete\",");
		json.AppendLine("\t\"zid\" : \"" + zid + "\"");
		json.AppendLine("}");

		QuestForTheChestFeature.instance.onRoundComplete(new JSON(json.ToString()));
	}

	private string getRandomUniqueZid()
	{
		string zid = "-1";
		bool found = false;
		while (!found)
		{
			string result = "";
			for (int i = 0; i < 11; i++)
			{
				result += Random.Range(0, 10).ToString();
			}

			if (!usedZids.Contains(result))
			{
				usedZids.Add(result);
				zid = result;
				found = true;
			}
		}

		return zid;
	}

	private string getRandomUniqueName()
	{
		string name = "";
		bool found = false;
		while (!found && usedNames.Count < FakeNameGenerator.firstNames.Count)
		{
			int index = Random.Range(0, FakeNameGenerator.firstNames.Count);
			if (!usedNames.Contains(FakeNameGenerator.firstNames[index]))
			{
				usedNames.Add(FakeNameGenerator.firstNames[index]);
				name = FakeNameGenerator.firstNames[index];
				found = true;
			}
		}

		return name;
	}

	private void clearEvents()
	{
		//clear everything but race complete events

		if (chestRewardEvents != null)
		{
			chestRewardEvents.Clear();
		}

		if (nodeRewardEvents != null)
		{
			nodeRewardEvents.Clear();
		}

		if (tokenRewardEvents != null)
		{
			tokenRewardEvents.Clear();
		}

		if (playerProgressEvents != null)
		{
			playerProgressEvents.Clear();
		}

	}

	private void clearRandomZids()
	{
		if (usedNames == null)
		{
			usedNames = new HashSet<string>();
		}
		else
		{
			usedNames.Clear();
		}

		if (usedZids == null)
		{
			usedZids = new HashSet<string>();
		}
		else
		{
			usedZids.Clear();
		}
	}

	private void fakeBonusGame()
	{
		string bonusGameJson = "{\"type\": \"qfc_mini_game_outcome\",\"event\": \"EKQRs3lpIpHvQwKDmUxMWkEvzjQBsn6Wo5rKupwNPzdCF\",\"creation_time\": \"1566256253\",\"test\": \"1\",\"outcomes\": [{\"outcome_type\": \"bonus_game\",\"outcomes\": [{\"outcome_type\": \"wheel\",\"win_id\": \"9821\"}], \"bonus_game\": \"qfc_mini_game\",\"bonus_game_pay_table\": \"qfc_mini_wheel_game_v1\",\"round_1_stop_id\": \"9821\"}], \"absolute_coin_rewards\": {\"9816\": \"130900\",\"9817\": \"0\",\"9818\": \"65450\",\"9819\": \"0\",\"9820\": \"130900\",\"9821\": \"0\",\"9822\": \"261800\",\"9823\": \"0\"}, \"tokens\": \"12\"}";
		QuestForTheChestFeature.instance.onMiniGameAwarded(new JSON(bonusGameJson));
	}


	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}
