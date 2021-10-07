using UnityEngine;
using System.Collections;

/**
Class for allowing the rendering of a paybox that goes around the perimeter of a game object
*/
public class GameObjectPayBoxScript : OutcomeDisplayScript 
{
	/// Initialize a box to go around the passed in GameObject
	public void init(GameObject targetObj)
	{
		PaylineBoxDrawer box = new PaylineBoxDrawer(targetObj.transform.position);
		Vector3 targetObjScale = targetObj.transform.localScale;
		box.boxSize = new Vector2(targetObjScale.x / 2.0f, targetObjScale.y / 2.0f);

		prepareCombineParts(combineInstances, box.refreshShape());

		// Combine all the meshes.
		combineMeshes();

		this.color = Color.red;
		this.alpha = 0;			// Make it invisible by default, until the show() method is called.
	}

	/// Allow the color to be changed to match other payline objects
	public void changeColor(Color color)
	{
		this.color = color;
	}
}
