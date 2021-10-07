using System.Collections;

/*
 * Triggers token position movement on the pick item click 
 */
public class BoardGameTokenUpdateOnPickModule : BoardGameModule
{
	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	// What happens when roll is clicked.
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(boardGameVariantParent.updatePlayerTokenPosition( pickingVariantParent.getCurrentPickOutcome().meterValue));
	}
}