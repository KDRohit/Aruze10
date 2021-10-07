using UnityEngine;
using System.Collections;

public class ListScrollerPerspective : ListScroller
{
	protected override void spaceItems()
	{
		base.spaceItems();

		applyTransforms();
	}

	private void applyTransforms()
	{
		this.gameObject.transform.localRotation = Quaternion.Euler(15, 0, 0);
	}
}