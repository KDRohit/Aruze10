using UnityEngine;
using System.Collections;

public class VIPRevampTiers : MonoBehaviour
{
	// =============================
	// PUBLIC
	// =============================
	public VIPRevampBenefitsPanel[] panels;

	void Awake()
	{
		for (int i = 0; i < panels.Length; ++i)
		{
			panels[i].setVIPLevel(i);
		}
	}
}