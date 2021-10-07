using UnityEngine;
using System.Collections;

/**
Original Author: Scott Lepthien
Class used for animating anticipations which aren't sent by the server and which have a left and right variant based on which reel the symbol is on
*/
public class PlaySymbolAnticipationOnReelStopModuleLeftAndRight : PlaySymbolAnticipatonOnReelStopModule 
{
	[Tooltip("if not set will default to symbolLandSoundKey")]
	[SerializeField] private string symbolLandSoundKeyLeft;
	[Tooltip("if not set will default to symbolLandSoundKey")]
	[SerializeField] private string symbolLandSoundKeyRight;
	[Tooltip("Adds text to symbolToAnimate so can mutate to that symbol, might not be needed if includeSymbolNameContains is set true")]
	[SerializeField] private string leftSymbolPostfix = "_Left";
	[Tooltip("Adds text to symbolToAnimate so can mutate to that symbol, might not be needed if includeSymbolNameContains is set true")]
	[SerializeField] private string rightSymbolPostfix = "_Right";

	private bool leftLanded = false;
	private int centerId = 0;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		centerId = (int)(Mathf.Ceil(reelGame.engine.getReelArray().Length / 2.0f)) + 1;

		yield break;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		leftLanded = false;

		yield break;
	}

	protected override void shouldPlaySymbolAnticipations(SlotReel reel)
	{
		if (isReelOnLeft(reel) || leftLanded)
		{
			handleSymbolAnticipations(reel);
		}
	}

	// Allows derived modules to contorl what symbol is mutated to before animating, for instance if the symbol has different animations base on reel location
	protected override string getSymbolMutateName(SlotReel stoppedReel, string originalName)
	{
		if (isReelOnLeft(stoppedReel))
		{
			leftLanded = true;
			return originalName + leftSymbolPostfix;
		}

		return originalName + rightSymbolPostfix;
	}

	protected override void onAnticipationAnimationDone(SlotSymbol sender)
	{
		// restore the symbol to the original version so it will animate correctly if it is part of outcomes
		string originalName = sender.serverName;

		if (!string.IsNullOrEmpty(leftSymbolPostfix))
		{
			originalName = originalName.Replace(leftSymbolPostfix, "");
		}

		if (!string.IsNullOrEmpty(rightSymbolPostfix))
		{
			originalName = originalName.Replace(rightSymbolPostfix, "");
		}

		sender.mutateTo(originalName);

		base.onAnticipationAnimationDone(sender);
	}

	override protected void playSymbolLandSound(SlotSymbol symbol)
	{
		string soundKey;
		if (isReelOnLeft(symbol.reel))
		{
			soundKey = symbolLandSoundKeyLeft;
		}
		else
		{
			soundKey = symbolLandSoundKeyRight;
		}

		if (string.IsNullOrEmpty(soundKey))
		{
			soundKey = symbolLandSoundKey;
		}

		if (!string.IsNullOrEmpty(soundKey))
		{
			Audio.playSoundMapOrSoundKey(soundKey);
		}
	}
		
	private bool isReelOnLeft(SlotReel reel)
	{
		if (reel != null)
		{
			return reel.reelID < centerId;
		}
		
		return true;
	}
}
