using System.Collections;


/// <summary>
/// Animate wilds as bonus symbols normally do. This is used in ainsworth16 where were need to animate the outcome
/// on the Wild symbol that is traditionally associated with Bonus symbols. This happens when the bonus game is triggered, 
/// but we use the WD symbol instead of a BN symbol. This will ikely not work in other games - unless they function similarly 
/// to ainsworth16
/// </summary>
public class AnimateWildsAsBonusModule : SlotModule
{
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return reelGame.getCurrentOutcome().isBonus;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(playOutcomeForWildBonus());
	}

	public virtual IEnumerator playOutcomeForWildBonus()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIdx].visibleSymbols;

			for (int i = 0; i < visibleSymbols.Length; i++)
			{
				SlotSymbol symbol = visibleSymbols[i];

				// this being an anticipation reel implies that it contains a bonus symbol (WD) which triggered the bonus
				if (symbol.isWildSymbol && !symbol.isAnimatorDoingSomething)
				{
					symbol.animateOutcome();
				}
			}
		}
		yield break;
	}
}
