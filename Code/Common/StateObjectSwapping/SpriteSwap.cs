using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;

/// <summary>
/// Swaps the target sprite by sprite name assigned per SpriteState instance
/// </summary>
[System.Obsolete("Animation list should be used instead of object swapper")]
public class SpriteSwap : ObjectSwap
{
	// =============================
	// PROTECTED
	// =============================
	[SerializeField] protected UISprite target;
	[SerializeField] protected List<SpriteState> swapperStates;

	/// <summary>
	/// Swaps the target sprite with the spriteName attached to the SpriteState instance
	/// </summary>
	/// <param name="state"></param>
	public override void swap(string state)
	{
		if (swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i] != null && swapperStates[i].state == state)
				{
					if (target != null)
					{
						target.spriteName = swapperStates[i].spriteNameToUse;
					}
					return;
				}
			}
		}
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		if (swapperStates != null)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i] == null)
				{
					continue;
				}
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