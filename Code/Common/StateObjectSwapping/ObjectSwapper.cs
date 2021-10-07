using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;

/// <summary>
/// Primary class involved with managing all ObjectSwap instances. Attach this script to a particular prefab,
/// and link all ObjectSwap scripts into the objectSwaps list. This can be executed in editor, viewing and testing states
/// through the use of the "run" boolean. currentStates list all of the states that have been assigned to any ObjectSwap
/// instances throughout your prefab, as well as listing how many occurrences there are of those states
/// </summary>
[ExecuteInEditMode]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class ObjectSwapper : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	private int lastCount;
	[SerializeField, ObjectSwapperAttribute] private string currentState;
	[SerializeField] private bool run;
	[ReadOnly] public List<string> currentStates;

	// =============================
	// PUBLIC
	// =============================
	public List<ObjectSwap> objectSwaps = new List<ObjectSwap>();

	/// <summary>
	/// Goes through the list of objects with states to swap to, and calls swap()
	/// </summary>
	/// <param name="state"></param>
	public void setState(string state)
	{
		currentState = state;

		for (int i = 0; i < objectSwaps.Count; ++i)
		{
			if (objectSwaps[i] == null)
			{
				continue;
			}
			objectSwaps[i].swap(state);
		}
	}

	public string getCurrentState()
	{
		return currentState;
	}

#if UNITY_EDITOR
	void Update()
	{
		if (Application.isPlaying)
		{
			return;
		}

		if (lastCount != objectSwaps.Count || currentStates == null || currentStates.Count != objectSwaps.Count)
		{
			lastCount = objectSwaps.Count;
			getAllStates();
		}

		if (run)
		{
			run = false;
			setState(currentState);
		}
	}
	
	void Reset()
	{
		DestroyImmediate(this);
	}

	private void getAllStates()
	{
		Dictionary<string, int> stateCount = new Dictionary<string, int>();
		for (int i = 0; i < objectSwaps.Count; ++i)
		{
			if (objectSwaps[i] != null)
			{
				string statesString = objectSwaps[i].ToString();
				string[] states = statesString.Split(',');
				for (int k = 0; k < states.Length; k++)
				{
					if (!stateCount.ContainsKey(states[k]))
					{
						stateCount.Add(states[k], 1);
					}
					else
					{
						stateCount[states[k]]++;
					}
				}
			}
		}

		currentStates = new List<string>();
		foreach (KeyValuePair<string, int> entry in stateCount)
		{
			currentStates.Add(string.Format("{0} ({1} occurrences)", entry.Key, entry.Value));
		}
	}

#endif
}