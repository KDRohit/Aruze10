using UnityEngine;
using System.Collections;

/*
Simply show and hide the appropriate GameObject whether using Loyalty Lounge or not.
*/

public class VIPLoyaltyIconHandler : MonoBehaviour
{
	public GameObject vipIcon;
	public GameObject llIcon;

	public void Awake()
	{
		vipIcon.SetActive(!LinkedVipProgram.instance.shouldSurfaceBranding);
		llIcon.SetActive(LinkedVipProgram.instance.shouldSurfaceBranding);
	}
}