using UnityEngine;
using System.Collections;

/**
 * Changes the material that a UIWidget uses. This is used to apply a custom shader to an NGUI object.
 */
[ExecuteInEditMode]
public class UIChangeMaterial : TICoroutineMonoBehaviour
{
	public Material customMaterial;

	void Start ()
	{
		UIWidget widget = GetComponent<UIWidget>();
		if(widget != null && customMaterial != null)
			widget.material = customMaterial;
	}
}
