using UnityEngine;
using System.Collections;
using TMPro;

public class VIPRevampLL : MonoBehaviour
{
//	public Animator animator;	
	public TextMeshPro llIconText;
	public GameObject llIcon;
	
	// =============================
	// CONST
	// =============================
	protected const string CONNECT_LL_LOCALE = "connect_to_ll";

	// if the user is an active LL member
	public void setActive(bool isActive)
	{
		SafeSet.gameObjectActive(llIcon, LinkedVipProgram.instance.isConnected && LinkedVipProgram.instance.shouldSurfaceBranding);
		SafeSet.labelText(llIconText, Localize.text(CONNECT_LL_LOCALE));
		gameObject.SetActive(isActive);		
	}
}
