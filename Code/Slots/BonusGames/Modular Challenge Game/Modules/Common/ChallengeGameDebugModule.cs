using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Debugging class to OnGUI out the outcomes
 */
public class ChallengeGameDebugModule : ChallengeGameModule
{
	private bool canShowOutcomes = false;
	public bool showOutcomes = false;
	// toggle with inspector
	private ModularChallengeGameOutcome parentOutcome;
	private List<ModularChallengeGameOutcomeEntry> picks;
	private List<ModularChallengeGameOutcomeEntry> leftovers;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant roundParent)
	{
		parentOutcome = roundParent.outcome;
		leftovers = roundParent.outcome.getRound(roundParent.roundIndex).reveals;
		picks = roundParent.outcome.getRound(roundParent.roundIndex).entries;

		base.executeOnRoundInit(roundParent);
	}

	public override bool needsToExecuteOnRoundStart()
	{		
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		canShowOutcomes = true;
		yield return null;
	}


	void OnGUI()
	{
		if (canShowOutcomes && showOutcomes)
		{
			switch (parentOutcome.outcomeType)
			{
				case SlotOutcome.OUTCOME_TYPE_PICKEM:
					GUILayout.Label("---- Picks ----");
					foreach (ModularChallengeGameOutcomeEntry item in picks)
					{
						GUILayout.Label("Reveal: " + item.credits);
						if (item.pickemPick != null && item.pickemPick.jackpotAwarded > 0)
						{
							GUILayout.Label("^^ Jackpot!!");
						}
					}

					GUILayout.Label("---- Leftovers ----");
					foreach (ModularChallengeGameOutcomeEntry item in leftovers)
					{
						GUILayout.Label("Reveal: " + item.credits);
						if (item.pickemPick != null && item.pickemPick.jackpotAwarded > 0)
						{
							GUILayout.Label("^^ Jackpot!!");
						}
					}
					break;
					
				case SlotOutcome.OUTCOME_TYPE_WHEEL:
					GUILayout.Label("------- Winning Wheel Pick --------");
					foreach (ModularChallengeGameOutcomeEntry item in picks)
					{
						GUILayout.Label("Pick index: " + item.wheelPick.winIndex + " wins: " + item.credits);
						if (item.bonusGame != null)
						{
							GUILayout.Label("^^ Transition to bonus game: " + item.wheelPick.bonusGame + " with data: " + item.wheelPick.extraData);
						}
					}

					GUILayout.Label("------- Wheel Leftovers --------");
					foreach (ModularChallengeGameOutcomeEntry item in leftovers)
					{
						GUILayout.Label("Pick index: " + item.wheelPick.winIndex + " wins: " + item.credits);
						if (item.bonusGame != null)
						{
							GUILayout.Label("^^ Transition to bonus game: " + item.wheelPick.bonusGame + " with data: " + item.wheelPick.extraData);
						}
					}
					break;
			}
		}
	}


	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{		
		return true;
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		canShowOutcomes = false;
		picks = null;
		leftovers = null;
		return null;
	}

}
