using UnityEngine;

public class DevGUIMenuSlotSymbolCache : DevGUIMenu
{
	private Vector2 scrollPosition;

	public override void drawGuts()
	{
		if (GameState.hasGameStack)
		{
			if (ReelGame.activeGame != null)
			{
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				ReelGame.activeGame.drawOnGuiSlotSymbolCacheInfo();
				GUILayout.EndScrollView();
			}
			else
			{
				GUILayout.Label("Game not loaded yet.");
			}
		}
		else
		{
			GUILayout.Label("In lobby");
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
