using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A bare bones version of AnimationListController.AnimationInformation which is intended
 * to be used in UI serialized fields where you are only dealing with animation states alone
 * and don't want all the other controls our animation list structure provides.  This will have
 * a PropertyDrawer similar to the one AnimationListController.AnimationInformation uses.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 4/7/2021
 */
[System.Serializable]
public class AnimatorStateInformation
{
	public Animator targetAnimator;
	public string ANIMATION_NAME = "";
	public int stateLayer = 0; // if using a different animator layer, play from it instead of the default
}
