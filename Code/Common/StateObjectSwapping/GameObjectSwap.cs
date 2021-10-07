using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// GameObjectSwap takes a list of GameObjectSwapperState instances. When swap() is called, all
/// other game objects for other states are disabled, and the list of game objects for the current state
/// is enabled.
/// </summary>
[System.Obsolete("Animation list should be used instead of object swapper")]
public class GameObjectSwap : ObjectSwap
{
	[SerializeField] protected List<GameObjectSwapperState> swapperStates;

	/// <summary>
	/// Disables all other game objects in each state, and enables all game objects for current GameObjectSwapperState
	/// </summary>
	/// <param name="state"></param>
	public override void swap(string state)
	{
		GameObjectSwapperState target = findSwapperState(state);

		if (target != null && swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i] != null)
				{
					swapperStates[i].disableAll();
				}
			}

			target.enableAll();
		}
	}

	/// <summary>
	/// Find the particular GameObjectSwapperState for the given state
	/// </summary>
	/// <param name="state"></param>
	/// <returns></returns>
	private GameObjectSwapperState findSwapperState(string state)
	{
		if (swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i] != null && swapperStates[i].state == state)
				{
					return swapperStates[i];
				}
			}
		}

		return null;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		if (swapperStates != null)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				sb.Append(swapperStates[i].state);
				if (i < swapperStates.Count - 1)
				{
					sb.Append(",");
				}
			}

			return sb.ToString();
		}

		return "";
	}
}