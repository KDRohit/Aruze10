using UnityEngine;
using System.Collections;

/**
 * Module to enable & disable a set of objects on round start & end
 */
public class ChallengeGameEnableDisableObjectsModule : ChallengeGameModule
{
	[SerializeField] private GameObject[] enableObjects;
	[SerializeField] private GameObject[] disableObjects;

	[SerializeField] private bool shouldExecuteOnRoundStart = true;
	[SerializeField] private bool shouldExecuteInverseOnRoundEnd = true;



	public override bool needsToExecuteOnRoundStart()
	{
		return shouldExecuteOnRoundStart;
	}

	// on round start, enable or disable the targets
	public override IEnumerator executeOnRoundStart()
	{
		foreach (GameObject go in enableObjects)
		{
			go.SetActive(true);
		}

		foreach (GameObject go in disableObjects)
		{
			go.SetActive(false);
		}

		yield break;
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return shouldExecuteInverseOnRoundEnd;
	}

	// disable everything on round ending
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		foreach (GameObject go in enableObjects)
		{
			go.SetActive(false);
		}

		foreach (GameObject go in disableObjects)
		{
			go.SetActive(false);
		}

		yield break;
	}
}
