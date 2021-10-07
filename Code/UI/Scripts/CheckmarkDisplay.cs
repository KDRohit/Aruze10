using UnityEngine;
using System.Collections;

/*
Simple controller to set up a display-only check mark to be checked or unchecked.
*/

public class CheckmarkDisplay : MonoBehaviour
{
	public GameObject uncheckedUI;
	public GameObject checkedUI;
	
	public void setChecked(bool isChecked)
	{
		SafeSet.gameObjectActive(uncheckedUI, !isChecked);
		SafeSet.gameObjectActive(checkedUI, isChecked);
	}
}
