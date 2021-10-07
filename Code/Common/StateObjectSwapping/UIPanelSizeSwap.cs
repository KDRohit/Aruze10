using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[System.Obsolete("Animation list should be used instead of object swapper")]
public class UIPanelSizeSwap : ObjectSwap
{
	[SerializeField] protected List<UIPanelSizeState> swapperStates;
	[SerializeField] protected UIPanel panel;

	/// <summary>
	/// Swaps the ui panel center, and size
	/// </summary>
	/// <param name="state"></param>
	public override void swap(string state)
	{
		if (panel != null && swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i].state == state)
				{
					panel.clipRange = swapperStates[i].centerAndSize;
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