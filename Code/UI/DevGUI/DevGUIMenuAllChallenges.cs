using UnityEngine;

public class DevGUIMenuAllChallenges : DevGUIMenu
{
    public override void drawGuts()
    {
        GUILayout.BeginHorizontal();

        GUILayout.EndHorizontal();
    }

    // Implements IResetGame
    new public static void resetStaticClassData()
    {
    }
}
