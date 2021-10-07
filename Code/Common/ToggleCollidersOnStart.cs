using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Script made for som01 Portal when we ran into an issue where UICamera's Raycast would
 * very rarely fail to hit colliders it should have been intersecting.  Adding this
 * to toggle the colliders, since that seemed to fix them and make them able to be
 * collided with again.
 *
 * Creation Date: 3/30/2020
 * Original Author: Scott Lepthien
 */
public class ToggleCollidersOnStart : TICoroutineMonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
		Collider[] colliders = ChallengeGame.instance.gameObject.GetComponentsInChildren<Collider>();
		if (colliders.Length > 0)
		{
			foreach (Collider collider in colliders)
			{
				// only toggle already active colliders
				if (collider.enabled)
				{
					RoutineRunner.instance.StartCoroutine(toggleCollider(collider));
				}
			}
		}
    }

	private IEnumerator toggleCollider(Collider collider)
	{
		collider.enabled = false;
		yield return null;
		collider.enabled = true;
	}
}
