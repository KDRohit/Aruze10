using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevGUIMenuCollections : DevGUIMenu
{

    // Use this for initialization
    public override void drawGuts()
    {
        GUILayout.BeginVertical();

        if (Collectables.isActive())
        {
	        if (GUILayout.Button("End in 5 seconds"))
	        {
		        Collectables.endTimer.updateEndTime(5);
	        }    
        }
        
        if (GUILayout.Button("Show first pack drop"))
        {

            Collectables.cachedPackJSON = new JSON(@"{
	""type"": ""collectible_pack_dropped"",
	""creation_time"": ""1526916048"",
	""album_id"": ""1"",
	""pack_id"": ""1"",
	""card_ids"": [""1"", ""3"", ""4"", ""5""]
	}");

            CollectablesMOTD.showDialog("debug_first");
        }

        if (GUILayout.Button("Show pack drop"))
        {
            JSON data = new JSON(@"{
	""type"": ""collectible_pack_dropped"",
			""creation_time"": 1570463743,
			""album"": ""world_traveler"",
			""pack"": ""level_pack_s4_t7_bluediamond"",
			""cards"": [
			""005_Paris_WorldTraveler"",
			""003_Paris_WorldTraveler"",
			""014_NewYork_WorldTraveler"",
			""029_Rome_WorldTraveler""
				],
			""source"": ""spin""
		}");

            JSON starPackData = data.getJSON("star_pack");
            string albumName = data.getString("album", "");
            string eventId = data.getString("event", "");
            string packId = data.getString("pack", "");
            JSON rewardsJson = data.getJSON("rewards");
            string source = data.getString("source", "");
            string[] droppedCardsNames = data.getStringArray("cards");

            List<CollectableCardData> collectedCards = new List<CollectableCardData>();
            for (int i = 0; i < droppedCardsNames.Length; i++)
            {
                string cardKeyName = droppedCardsNames[i];
                CollectableCardData collectedCard = Collectables.Instance.findCard(cardKeyName);
                if (collectedCard != null)
                {
                    collectedCards.Add(collectedCard);
                }
            }

            PackDroppedDialog.showDialog(collectedCards, albumName, eventId, packId, source, starPackData, rewardsJson);

        }

        GUILayout.EndVertical();
    }

}