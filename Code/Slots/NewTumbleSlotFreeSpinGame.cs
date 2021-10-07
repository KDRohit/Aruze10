using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewTumbleSlotFreeSpinGame : FreeSpinGame
{
	protected override void setEngine(string payTableKey)
	{
		engine = new TumbleSlotEngine(this, payTableKey);
	}
}
