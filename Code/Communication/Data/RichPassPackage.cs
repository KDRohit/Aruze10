using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Package class for subscription based items
public class RichPassPackage 
{
	public PurchasablePackage purchasePackage { get; private set; }
	
	public RichPassPackage(string packageName)
	{
		purchasePackage = PurchasablePackage.find(packageName);
	}
	
	public override string ToString()
	{
		return string.Format(
			"RichPassPackage:[package_key:{0}]",
			purchasePackage.keyName.ToString());
	}
}
