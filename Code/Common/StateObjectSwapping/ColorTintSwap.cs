using System.Collections.Generic;
using UnityEngine;
using System.Text;

[System.Obsolete("Animation list should be used instead of object swapper")]
public class ColorTintSwap : ObjectSwap
{
	[SerializeField] protected UISprite sprite;
	[SerializeField] protected List<ColorTintState> swapperStates;

	public override void swap(string state)
	{
		if (sprite == null)
		{
			return;
		}

		if (sprite != null && swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i].state == state)
				{
					sprite.color = swapperStates[i].color;
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
