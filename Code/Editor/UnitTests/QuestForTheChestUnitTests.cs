using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuestForTheChest
{

	[TestFixture]
	public class QuestForTheChestUnitTests : IPrebuildSetup
	{
		private const int totalNodesForTest = 13;
		private string getNodeRewards()
		{
			return "{\"1\":\"0\",\"2\":\"0\",\"3\":\"0.1600000000000000033306690738754696212708950042724609375\",\"4\":\"0\",\"5\":\"0.299999999999999988897769753748434595763683319091796875\",\"6\":\"0\",\"7\":\"0\",\"8\":\"0.54000000000000003552713678800500929355621337890625\",\"9\":\"0\",\"10\":\"0.770000000000000017763568394002504646778106689453125\",\"11\":\"0\",\"12\":\"0\",\"13\":\"0\"}";
		}

		private QFCThemedStaticData getThemeData()
		{
			GameObject obj = new GameObject();
			QFCThemedStaticData themeData = obj.AddComponent<QFCThemedStaticData>();
			themeData.nodeLocations = new Transform[totalNodesForTest];
			for (int i = 0; i < totalNodesForTest; ++i)
			{
				GameObject tempObj = new GameObject();
				tempObj.transform.localPosition =  new Vector3(Random.Range(-500f, 500f), Random.Range(-500f, 500f), 0);
				themeData.nodeLocations[i] = tempObj.transform;
			}
			themeData.rewardShroudColor = new Color32(88,22,171,255);
			return themeData;
		}

		public void Setup()
		{
			StringBuilder json = new StringBuilder();
			json.AppendLine("{");
			json.AppendLine("	\"type\" : \":qfc_race_info\",");
			json.AppendLine("	\"start_time\" : " + System.DateTime.UtcNow.Ticks + ",");
			json.AppendLine("	\"end_time\" :  " + (System.DateTime.UtcNow.AddSeconds(60 * 60)).Ticks + ",");
			json.AppendLine("	\"home_team\" : {");
			json.AppendLine("		\"70000000001\" : {");
			json.AppendLine("			\"name\" : \"Ankush T.\",");
			json.AppendLine("			\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("			\"current_node\" : 5,");
			json.AppendLine("			\"tokens_earned\" : 12");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000002\" : {");
			json.AppendLine("			\"name\" : \"Terrence p.\",");
			json.AppendLine("			\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("			\"current_node\" : 3,");
			json.AppendLine("			\"tokens_earned\" : 6");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000003\" : {");
			json.AppendLine("			\"name\" : \"David z.\",");
			json.AppendLine("			\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("			\"current_node\" : 2,");
			json.AppendLine("			\"tokens_earned\" : 3");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000004\" : {");
			json.AppendLine("			\"name\" : \"Juang l.\",");
			json.AppendLine("			\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("			\"current_node\" : 0,");    // This player is at the start node
			json.AppendLine("			\"tokens_earned\" : 0");
			json.AppendLine("		},");
			json.AppendLine("		\"z70000000005\" : {");
			json.AppendLine("			\"name\" : \"Sameer a.\",");
			json.AppendLine("			\"photo\" : \"https://cat-bounce.com/catbounce.png\",");
			json.AppendLine("			\"current_node\" : 0,");
			json.AppendLine("			\"tokens_earned\" : 0");
			json.AppendLine("		}");
			json.AppendLine("	},");
			json.AppendLine("	\"away_team\" : {");
			json.AppendLine("		\"70000000006\" : {");
			json.AppendLine("			\"name\" : \"Mike M.\",");
			json.AppendLine("			\"photo\" : \"https://www.petinsurance.com/images/VSSimages/consumer/v5/hero-cat.png\",");
			json.AppendLine("			\"current_node\" : 7,");
			json.AppendLine("			\"tokens_earned\" : 22");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000007\" : {");
			json.AppendLine("			\"name\" : \"Stephen A.\",");
			json.AppendLine("			\"photo\" : \"https://www.petinsurance.com/images/VSSimages/consumer/v5/hero-cat.png\",");
			json.AppendLine("			\"current_node\" : 4,");
			json.AppendLine("			\"tokens_earned\" : 9");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000008\" : {");
			json.AppendLine("			\"name\" : \"QD.\",");
			json.AppendLine("			\"photo\" : \"https://www.petinsurance.com/images/VSSimages/consumer/v5/hero-cat.png\",");
			json.AppendLine("			\"current_node\" : 1,");
			json.AppendLine("			\"tokens_earned\" : 2");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000009\" : {");
			json.AppendLine("			\"name\" : \"Victor C.\",");
			json.AppendLine("			\"photo\" : \"https://www.petinsurance.com/images/VSSimages/consumer/v5/hero-cat.png\",");
			json.AppendLine("			\"current_node\" : 5,");    // This player is at the start node
			json.AppendLine("			\"tokens_earned\" : 5");
			json.AppendLine("		},");
			json.AppendLine("		\"70000000010\" : {");
			json.AppendLine("			\"name\" : \"Nick S.\",");
			json.AppendLine("			\"photo\" : \"https://www.petinsurance.com/images/VSSimages/consumer/v5/hero-cat.png\",");
			json.AppendLine("			\"current_node\" : 0,");
			json.AppendLine("			\"tokens_earned\" : 0");
			json.AppendLine("		}");
			json.AppendLine("	},");
			json.AppendLine("   \"node_rewards\": " + getNodeRewards() + ",");
			json.AppendLine("	\"tokens_required\" : 50,");    // tokens needed to win the race
			json.AppendLine("	\"total_nodes\" : " + totalNodesForTest);         // total nodes in the board
			json.AppendLine("}");



			QuestForTheChestFeature.instance.onNewRace(new JSON(json.ToString()), getThemeData(), false);
		}



		/******************
		Valid Data Tests
		******************/

		[Test]
		[PrebuildSetup(typeof(QuestForTheChestUnitTests))]
		public static void QuestForTheChest_AdvancePlayer_Move()
		{
			Assert.AreEqual(4, QuestForTheChestFeature.instance.advancePlayer("70000000002"));
		}

		[Test]
		[PrebuildSetup(typeof(QuestForTheChestUnitTests))]
		public static void QuestForTheChest_AdvancePlayer_MultiMove()
		{
			int moveAmount = UnityEngine.Random.Range(1, 5);
			Assert.AreEqual(7 + moveAmount, QuestForTheChestFeature.instance.advancePlayer("70000000006", moveAmount));
		}

		[Test]
		[PrebuildSetup(typeof(QuestForTheChestUnitTests))]
		public static void QuestForTheChest_AdvancePlayer_MovePastFinish()
		{
			Assert.AreEqual(0,QuestForTheChestFeature.instance.advancePlayer("70000000001", 1000));
			Assert.AreEqual(1, QuestForTheChestFeature.instance.getRoundForPlayer("70000000001"));
		}

		[Test]
		[PrebuildSetup(typeof(QuestForTheChestUnitTests))]
		public static void QuestForTheChest_GameBoard_BoardSetup()
		{
			//test that all nodes are created
			QFCBoardNode currentNode = QuestForTheChestFeature.instance.getFirstBoardNode();
			Assert.NotNull(currentNode);
			for (int i = 0; i < QuestForTheChestFeature.instance.totalNodes-1; ++i)
			{
				currentNode = currentNode.nextNode;
				Assert.NotNull(currentNode);
			}

			//test that there are no extra nodes
			Assert.IsNull(currentNode.nextNode);
		}

		[Test]
		[PrebuildSetup(typeof(QuestForTheChestUnitTests))]
		public static void QuestForTheChest_Logic_TeamTokenTotal()
		{
			Assert.AreEqual(38, QuestForTheChestFeature.instance.getTeamKeyTotal(QFCTeams.AWAY));
		}

		[Test]
		[PrebuildSetup(typeof(QuestForTheChestUnitTests))]
		public static void QuestForTheChest_Logic_AwardToken()
		{
			Assert.AreEqual(13, QuestForTheChestFeature.instance.awardKeys("70000000001"));
		}

	}
}
