using UnityEngine;
using System.Collections;
using TMPro;
public class VIPBoostCardAnimation : MonoBehaviour
{
	public TextMeshPro timerText;

	void Awake()
	{
		// When this comes in, just start doing stuff.
		VIPStatusBoostEvent.featureTimer.registerLabel(timerText);
	}
}