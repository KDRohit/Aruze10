using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LottoBlastProgressPath : TICoroutineMonoBehaviour
{
	[SerializeField] private DynamicCellSizeUIGrid grid;
	[SerializeField] private LottoBlastProgressNode[] nodes;
	[SerializeField] private LottoBlastProgressNode finalNode;
	[SerializeField] private bool recalculate = false;

	public int maxNodes
	{
		get { return nodes.Length; }
	}

	public void setup(List<int> levels, List<long> xpValues, bool isBuffActive)
	{
		
		if (grid != null && nodes != null)
		{
			for (int i = levels.Count-1; i < nodes.Length; i++)
			{
				nodes[i].gameObject.SetActive(false);
			}
			grid.Reposition();
			
			for (int i = 0; i < nodes.Length; i++)
			{
				if (nodes[i].gameObject.activeSelf)
				{
					nodes[i].setup(levels[i], xpValues[i+1], xpValues[i], grid.cellWidth, isBuffActive);
				}
			}
		}
		
		//The final Node is fixed and has no meter
		finalNode.setup(levels[levels.Count-1], 0, 0, 0.0f, isBuffActive);
	}
}
