using UnityEngine;
using FeatureOrchestrator;
using System.Collections.Generic;

public class DevGUIMenuCasinoEmpire : DevGUIMenu
{
    private string pickIndex = "0";
    public override void drawGuts()
    {
		
        GUILayout.BeginVertical();
        FeatureConfig featureConfig = null;
        if (Orchestrator.instance.allFeatureConfigs.TryGetValue("hir_boardgame", out featureConfig))
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go To Last Roll"))
            {
                Orchestrator.instance.performStep(featureConfig, null, "moveToEndOfCurrentBoard", true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go To First Roll"))
            {
                Orchestrator.instance.performStep(featureConfig, null, "moveToBeginningOfCurrentBoard", true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go To Selected Roll"))
            {
                Dictionary<string, object> payload = new Dictionary<string, object>();
                payload.Add("newPickIndex", pickIndex);
                Orchestrator.instance.performStep(featureConfig, payload, "moveToSelectedPick", true);
            }
            GUILayout.BeginVertical();
            pickIndex = GUILayout.TextField(pickIndex);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Make All Mystery Cards Purchase Offers"))
            {
                Orchestrator.instance.performStep(featureConfig, null, "switchToPurchaseOfferRewardPaytable", true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Make All Mystery Cards Land or Unland Random Space"))
            {
                Orchestrator.instance.performStep(featureConfig, null, "switchToLitUnlitActionsPaytable", true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Make All Mystery Cards & Mini Slots Card Pack Rewards"))
            {
                Orchestrator.instance.performStep(featureConfig, null, "switchToCollectiblePackRewardPaytable", true);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset FTUE"))
            {
                CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SEEN_VERSION, 0);
                CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, -1);
                CustomPlayerData.setValue(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_FTUE_SEEN, false);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Feature isn't active");
        }
        GUILayout.EndVertical();
    }
}