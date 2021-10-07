using UnityEngine;
using System.Collections;

public class PaytableBonusIndex : TICoroutineMonoBehaviour
{
	public PaytableBonus[] bonuses;

	// Use this for initialization
	public void init(int bonusWalk)
	{
		if (PaytablesDialog.instance == null)
		{
			Debug.LogError("Can't find parent PaytableDialog in PaytableBonusIndex.");
			return;
		}

		// Look up the game that we're going to display information about:
		string gameIdentifier = PaytablesDialog.instance.gameKey;
		SlotGameData game = SlotGameData.find(gameIdentifier);
		if (game == null)
		{
			Debug.LogError("Can't find game: " + gameIdentifier + " in PaytableGlyphIndex.");
			return;
		}

		// Look through the list of bonus games - and populate the dialog:
		int slotWalk = 0;
		while((slotWalk < this.bonuses.Length) && (bonusWalk < game.bonusGames.Length))
		{
			this.bonuses[slotWalk].init(gameIdentifier, game.bonusGames[bonusWalk]);
			slotWalk++;
			bonusWalk++;
		}

		while(slotWalk < this.bonuses.Length)
		{
			this.bonuses[slotWalk].hide();
			slotWalk++;
		}
	}
}
