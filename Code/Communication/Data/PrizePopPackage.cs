using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Package class for subscription based items
public class PrizePopPackage 
{
	public PurchasablePackage purchasePackage { get; private set; }
	
	public PrizePopPackage(string packageName)
	{
		purchasePackage = PurchasablePackage.find(packageName);
		if (purchasePackage == null)
		{
			Debug.LogError($"Prizse Pop package [{packageName}] is missing");
		}
	}
	
	public override string ToString()
	{
		return string.Format(
			"PrizePopPackage:[package_key:{0}]",
			purchasePackage.keyName.ToString());
	}
}
