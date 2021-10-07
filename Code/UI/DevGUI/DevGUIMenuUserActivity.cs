using UnityEngine;

public class DevGUIMenuUserActivity : DevGUIMenu, IResetGame
{
    private static bool idleOverride = false;
    public override void drawGuts()
    {
        if (UserActivityManager.instance == null)
        {
            return;
        }
        GUILayout.BeginVertical();
        drawTimestamp("Login (utc)", UserActivityManager.instance.loginTime);
        drawTimestamp("Click (utc)", UserActivityManager.instance.lastInputTime);
        drawTimestamp("Spin (utc)", UserActivityManager.instance.lastSpinTime);
        drawTimestamp("Saved time (utc)", UserActivityManager.instance.savedTime);
        GUILayout.Label("Is Idle: " + UserActivityManager.instance.isIdle);
        GUILayout.Label("Was Idle Before Login: " + UserActivityManager.instance.wasIdleBeforeLogin);
        GUILayout.Label("Time until idle: " + UserActivityManager.instance.timeUntilIdle);
        GUILayout.Label("Time until next save to disk: " + UserActivityManager.instance.timeUntilDiskWrite);

#if !ZYNGA_PRODUCTION
        if (idleOverride && GUILayout.Button("Turn off Force Idle Before Login"))
        {
            idleOverride = false;
            UserActivityManager.instance.debugForceIdleBeforeLogin(false);
        }
        else if (!idleOverride && GUILayout.Button("Force Idle Before login"))
        {
            idleOverride = true;
            UserActivityManager.instance.debugForceIdleBeforeLogin(true);
        }
        
        if (GUILayout.Button("Force idle now"))
        {
            UserActivityManager.instance.debugForceIdleNow();
        }

        if (GUILayout.Button("Force write to disk"))
        {
            UserActivityManager.instance.forceWriteToDisk();
        }

        if (GUILayout.Button("Decrement saved time by 1 hour"))
        {
            UserActivityManager.instance.debugDecrementSavedTime(Common.SECONDS_PER_HOUR);
        }
#endif
        GUILayout.EndVertical();
    }


    private void drawTimestamp(string title, int time)
    {
        System.DateTime date = Common.convertFromUnixTimestampSeconds(time);
	    GUILayout.Label(title + ": " +  CommonText.formatDateTime(date));
    }
    
    public static void resetStaticClassData()
    {
        idleOverride = false;
    }
}
