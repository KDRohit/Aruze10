using UnityEngine;

/*
 * Script that enables the saving of an Animator state, so that it will restore if the
 * object becomes disabled and later is enabled again.
 *
 * Original Creator: Scott Lepthien
 * Creation Date: 2/11/2020
 */
[RequireComponent(typeof(Animator))]
public class RestoreAnimatorStateOnReenable : MonoBehaviour
{
	private void Awake()
	{
		Animator animator = GetComponent<Animator>();
		animator.keepAnimatorControllerStateOnDisable = true;
	}
}
