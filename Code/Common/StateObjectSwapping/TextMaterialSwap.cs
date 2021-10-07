using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;

[System.Obsolete("Animation list should be used instead of object swapper")]
public class TextMaterialSwap : ObjectSwap
{
	// =============================
	// PROTECTED
	// =============================
	[SerializeField] protected TextMeshPro mesh;
	[SerializeField] protected List<MaterialState> swapperStates;

	/// <summary>
	/// Swaps the font material on the target textmeshpro mesh
	/// </summary>
	/// <param name="state"></param>
	public override void swap(string state)
	{
		if (mesh == null)
		{
			mesh = GetComponent<TextMeshPro>();
		}

		if (mesh != null && swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; ++i)
			{
				if (swapperStates[i].state == state)
				{
					if (mesh.GetComponent<TextMeshPro>() != null)
					{
						MaterialState materialState = swapperStates[i] as MaterialState;

						if (materialState != null && materialState.materialToUse != null)
						{
							mesh.fontMaterial = materialState.materialToUse;
						}
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