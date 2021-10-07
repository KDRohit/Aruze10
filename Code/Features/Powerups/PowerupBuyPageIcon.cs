using System;
using UnityEngine;
using System.Collections;
using TMPro;

public class PowerupBuyPageIcon : MonoBehaviour
{
	[SerializeField] private TextMeshPro bonusLabel;

	private void Awake()
	{
		bonusLabel.text = BuyPageBonusPowerup.salePercent.ToString() + "%";
	}
}