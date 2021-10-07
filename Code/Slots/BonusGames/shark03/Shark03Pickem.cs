using UnityEngine;
using System.Collections;

public class Shark03Pickem : GenericPickemOutcomePickemGame 
{	
	protected override void doSpecialOnFighterBeforeTween(GameObject fighterParent, GameObject location)
	{
		Vector3 xDirection = new Vector3(1.0f, 0.0f, 0.0f);
		if (fighterParent.transform.position.x > location.transform.position.x)
		{
			fighterParent.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
		}
		
		if (fighterParent.transform.position.x > location.transform.position.x)
		{
			xDirection = new Vector3(-1.0f, 0.0f, 0.0f);
		}
		
		Vector3 locationExcludeZ = new Vector3(location.transform.position.x, location.transform.position.y, 0.0f);
		Vector3 fighterExcludeZ = new Vector3(fighterParent.transform.position.x, fighterParent.transform.position.y, 0.0f);
		Vector3 moveDirection = locationExcludeZ - fighterExcludeZ;
		
		float angleBetween = Vector3.Angle(xDirection, moveDirection);
		Vector3 crossProduct = Vector3.Cross(xDirection, moveDirection);

		fighterParent.transform.Rotate(crossProduct, angleBetween);

	}
}
