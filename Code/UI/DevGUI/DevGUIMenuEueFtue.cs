using Com.HitItRich.EUE;
using Com.Scheduler;
using UnityEngine;
using Zynga.Core.Util;

public class DevGUIMenuEueFtue : DevGUIMenu
{
    public override void drawGuts()
    {
        GUILayout.BeginVertical();
        if (GUILayout.Button("Show first login"))
        {
            EueFtueRichDialog.skipFTUE = false;
            EUEManager.showFirstLoadOverlay();
        }

        if (GUILayout.Button("Show DB Force"))
        {
            EueFtueRichDialog.skipFTUE = false;
            EUEManager.showBonusCollect();
        }
        
        if (GUILayout.Button("Show Game Intro"))
        {
            EueFtueRichDialog.skipFTUE = false;
            EUEManager.showGameIntro();
        }

        if (GUILayout.Button("Show Challenge Intro"))
        {
            EueFtueRichDialog.skipFTUE = false;
            EUEManager.showChallengeIntro();
        }

        if (GUILayout.Button("Show Challenge Complete"))
        {
            EueFtueRichDialog.skipFTUE = false;
            EUEManager.showChallengeComplete();
        }

        if (GUILayout.Button("Show Level up w/ Rich"))
        {
            int newLevel = 2;
            string levelData = "{\"level\":\"" + newLevel + "\",\"required_xp\":\"25000\",\"bonus_amount\":\"1000\",\"bonus_vip_points\":\"1\",\"max_bet\":\"500\"}";
            JSON levelJSON = new JSON(levelData);
            JSON[] levelArray = new JSON[] { levelJSON };
            ExperienceLevelData.populateAll(levelArray);
            Overlay.instance.topHIR.showLevelUpAnimation(100, 100, 2);
            Overlay.instance.topHIR.playLevelUpSequence(false, newLevel);
        }
        
        if (GUILayout.Button("Show End Dialog - Normal"))
        {
            Scheduler.addDialog("eue_end_generic");
        }
        
        if (GUILayout.Button("Show End Dialog - Slotventure"))
        {
            Scheduler.addDialog("eue_end_slotventure");
        }
        
#if !ZYNGA_PRODUCTION
        if (GUILayout.Button("Fake challenge complete"))
        {
            if (CampaignDirector.eue != null)
            {
                ((EueCampaign)CampaignDirector.eue).showCurrentMissionComplete();
            }
        }
#endif

        if (GUILayout.Button("Clear FTUE seen flags (causes reset)"))
        {
            PreferencesBase prefs = SlotsPlayer.getPreferences();
            prefs.SetBool(Prefs.FTUE_GAME_INTRO, false);
            prefs.SetBool(Prefs.FTUE_FIRST_LOGIN, false);
            prefs.SetBool(Prefs.FTUE_CHALLENGE_INTRO, false);
            prefs.SetBool(Prefs.FTUE_CHALLENGE_COMPLETE, false);
            prefs.SetBool(Prefs.FTUE_ABORT, false);
            prefs.SetString(Prefs.FIRST_APP_START_TIME, "");
            prefs.Save();
            CustomPlayerData.setValue(CustomPlayerData.DAILY_BONUS_COLLECTED, false);
            Glb.resetGame("Dev Gui");
        }
        
        GUILayout.EndVertical();
    }
}
