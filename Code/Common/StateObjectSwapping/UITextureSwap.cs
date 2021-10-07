using UnityEngine;
using System.Collections.Generic;
using System.Text;

[System.Obsolete("Animation list should be used instead of object swapper")]
public class UITextureSwap : ObjectSwap
{
	[SerializeField] protected UITexture target;
	[SerializeField] protected List<TextureState> swapperStates;

	private Dictionary<string, Material> createdMaterials = null;
	private Dictionary<string, TextureState> stateLookup = null;

	private void initInternalData()
	{
		createdMaterials = new Dictionary<string, Material>();
		stateLookup = new Dictionary<string, TextureState>();
		if (swapperStates != null)
		{
			for (int i = 0; i < swapperStates.Count; i++)
			{	
				//add to lookup
				stateLookup.Add(swapperStates[i].state, swapperStates[i]);
			}
		}
	}

	private void OnDestroy()
	{
		if (createdMaterials != null)
		{
			foreach (Material mat in createdMaterials.Values)
			{
				Destroy(mat);
			}
			createdMaterials = null;
		}
		
	}

	/// <summary>
	/// Swaps the UI texture on target UITexture based on the TextureState assignment
	/// Creates one material for each state then swaps back to them on subsequent swaps
	/// </summary>
	/// <param name="state"></param>
	public override void swap(string state)
	{
		if (target == null || target.gameObject == null)
		{
			Debug.LogError("Invalid state switch");
			return;
		}
		
		if (swapperStates != null)
		{
			if (createdMaterials == null || stateLookup == null)
			{
				initInternalData();
			}
			
			TextureState stateData = null;
			if (stateLookup.TryGetValue(state, out stateData))
			{
				Material mat = null;
				if (createdMaterials.TryGetValue(state, out mat))
				{
					target.material = mat;
					enablePanel();
					return;
				}
				
				//if the artist supplied a material we'll assume that it's managed elsewhere
				if (stateData.materialToUse != null)
				{
					mat = new Material(stateData.materialToUse);
				}
				else
				{
					mat = new Material(target.material);
				}

				mat.mainTexture = stateData.textureToUse;
				createdMaterials[state] = mat;
				target.material = mat;
				enablePanel();

			}
		}
	}

	private void enablePanel()
	{
		if (target.panel != null)
		{
			target.panel.enabled = false;
			target.panel.enabled = true;
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