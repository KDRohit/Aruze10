using UnityEngine;
using System.Collections;
using TMPro;

/**
Syncs text mesh pro instances, so they the all have the same text values. Assign a text mesh pro parent,
and add to the subLabels list any text mesh pro instances that should sync to the parent
*/

[ExecuteInEditMode]
public class TMPSync : MonoBehaviour
{
	[SerializeField] private TextMeshPro primaryLabel;
	[SerializeField] private TextMeshPro[] subLabels = new TextMeshPro[1];
	private string currentText;

	void Awake()
	{
		if (primaryLabel == null)
		{
			primaryLabel = GetComponent<TextMeshPro>();
		}

		currentText = primaryLabel != null ? primaryLabel.text : "";
		syncLabels();
	}

	void Update()
	{
		if (primaryLabel != null && primaryLabel.isActiveAndEnabled)
		{
			if (!string.Equals(currentText, primaryLabel.text))
			{
				syncLabels();
				currentText = primaryLabel.text;
			}

			syncVisuals();
		}
	}

	private void syncLabels()
	{
		if (primaryLabel != null)
		{
			for (int i = 0; i < subLabels.Length; ++i)
			{
				if (subLabels[i] != null)
				{
					subLabels[i].text = primaryLabel.text;
				}
			}
		}
	}

	private void syncVisuals()
	{
		if (primaryLabel != null)
		{
			for (int i = 0; i < subLabels.Length; ++i)
			{
				if (subLabels[i] != null && subLabels[i].color != primaryLabel.color)
				{
					subLabels[i].color = CommonColor.adjustAlpha(subLabels[i].color, primaryLabel.alpha);
				}
			}
		}
	}
}