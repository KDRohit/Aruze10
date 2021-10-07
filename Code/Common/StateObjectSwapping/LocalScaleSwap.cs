using System.Collections.Generic;
using UnityEngine;
using System.Text;

[System.Obsolete("Animation list should be used instead of object swapper")]
public class LocalScaleSwap : ObjectSwap
{
	[SerializeField] protected Transform objectToScale;
	[SerializeField] protected List<LocalScaleState> swapperStates;

	/// <summary>
	/// Repositions the localPosition of the target transform based on the state
	/// </summary>
	public override void swap(string state)
	{
		if (objectToScale == null)
		{
			objectToScale = transform;
		}

		if (objectToScale != null && swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i].state == state)
				{
					objectToScale.localScale = swapperStates[i].localScale;
				}
			}
		}
	}

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