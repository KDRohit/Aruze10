
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// The Spin it Rich base class. A pretty simple game, but takes care of adding a card / card with seam,
/// behind every symbol.

public class Hi01 : SlotBaseGame
{
	public Texture2D cardWithSeam;					// The texture that should go on the background of the symbol.

	private PlayingAudio backgroundHum = null;
	private bool playedWDAudio = false;

	// Sounds Names
	private const string SPIN_START_SOUND = "mechreel03start";
	private const string SPINNING_SOUND = "mechreel03loop";
	private const string W2_ANIMATING_SOUND = "SymbolW2HiLimit7";

	protected override void SymbolAnimatingCallback(SymbolAnimator animator)
	{
		// Check and see if the symbol is a W2.
		if (animator != null)
		{
			if (!playedWDAudio)
			{
				playedWDAudio = true;
				if (animator.symbolInfoName == "W2" || animator.symbolInfoName == "WD") // Check for WD because they're the same symbol and backend switches them sometimes.
				{
					Audio.play(W2_ANIMATING_SOUND);
				}
			}
		}
	}

	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());

		playedWDAudio = false;
		Audio.play(SPIN_START_SOUND);
		backgroundHum = Audio.play(SPINNING_SOUND, 1, 0, 0, float.PositiveInfinity);
	}

	protected override void reelsStoppedCallback()
	{
		if (backgroundHum != null)
		{
			backgroundHum.stop(0);
		}
		base.reelsStoppedCallback();
	}
}
