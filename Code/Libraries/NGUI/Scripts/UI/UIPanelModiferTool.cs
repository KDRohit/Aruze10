using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Zynga made class.
[ExecuteInEditMode]
public class UIPanelModiferTool : TICoroutineMonoBehaviour
{
	public enum ExpansionState
	{
		NONE,
		SQUASH,
		EXPAND
	}

	public Dictionary<Material, int> materialToDepthDic = new Dictionary<Material, int>();

	public ExpansionState state
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
			Update();
		}
	}
	private ExpansionState _state = ExpansionState.NONE;
	private UIPanel panel = null;
	private ExpansionState lastState = ExpansionState.NONE;

	public void Update()
	{
		if (Application.isPlaying || state == ExpansionState.NONE)
		{
			// Only do the update if the game isn't running.
			return;
		}

		if (panel == null)
		{
			// Make sure we have a reference to the panel.
			panel =  gameObject.GetComponent<UIPanel>();
		}


		HashSet<UIWidget> drawnWidgets = getListOfDrawnWidgets();
		updateMaterialToDepthDic(drawnWidgets);

		// Add in all the possibly new materials into the matrialDict.

		foreach (UIWidget widget in drawnWidgets)
		{
			Transform parent = widget.transform.parent;
			widget.transform.parent = panel.transform.parent;

			if (state == ExpansionState.SQUASH)
			{
				Vector3 pos = widget.transform.localPosition;
				pos.z = materialToDepthDic[widget.material];
				widget.transform.localPosition = pos;
				widget.transform.parent = parent;
			}
			else if (state == ExpansionState.EXPAND)
			{
				if (lastState != ExpansionState.EXPAND)
				{
					// We just moved from Squash so we want to set the Z position to be the depth.
					Vector3 pos = widget.transform.localPosition;
					pos.z = -widget.depth;
					widget.transform.localPosition = pos;
				}
				else
				{
					widget.depth = -(int)widget.transform.localPosition.z;
				}
			}

			widget.transform.parent = parent;
		}

		lastState = state;
	}

	private HashSet<UIWidget> getListOfDrawnWidgets()
	{
		HashSet<Transform> children = new HashSet<Transform>(gameObject.GetComponentsInChildren<Transform>(true));
		children.Remove(transform);
		// Go through the list children and see if they have a UI Panel attached to them.
		HashSet<Transform> childrenToRemove = new HashSet<Transform>();
		foreach (Transform child in children)
		{
			if (child.GetComponent<UIPanel>() != null)
			{
				childrenToRemove.Add(child);
				foreach (Transform grandchild in child.GetComponentsInChildren<Transform>(true))
				{
					childrenToRemove.Add(grandchild);
				}
			}
		}

		children.ExceptWith(childrenToRemove);
		children.Add(transform);
		HashSet<UIWidget> childrenWidgets = new HashSet<UIWidget>();
		foreach (Transform child in children)
		{
			UIWidget widget = child.GetComponent<UIWidget>();
			if (widget != null)
			{
				childrenWidgets.Add(widget);
			}
		}

		return childrenWidgets;
	}

	private void updateMaterialToDepthDic(HashSet<UIWidget> drawnWidgets)
	{
		if (drawnWidgets.Count > 0)
		{
			foreach (UIWidget widget in drawnWidgets)
			{
				if (!materialToDepthDic.ContainsKey(widget.material))
				{
					materialToDepthDic.Add(widget.material, 0);
				}
			}
			// Make sure we don't have more materials in the dict than we should.
			HashSet<Material> removedMaterials = new HashSet<Material>();
			foreach(KeyValuePair<Material, int> kvp in materialToDepthDic)
			{
				bool found = false;
				foreach (UIWidget widget in drawnWidgets)
				{
					if (kvp.Key == widget.material)
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					removedMaterials.Add(kvp.Key);
				}
			}
			// Remove the extra materials.
			foreach (Material mat in removedMaterials)
			{
				materialToDepthDic.Remove(mat);
			}
		}
	}
}