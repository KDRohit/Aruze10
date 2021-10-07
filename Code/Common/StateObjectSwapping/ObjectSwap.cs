using System;
using UnityEngine;
using System.Collections;

public abstract class ObjectSwap : MonoBehaviour
{
	/// <summary>
	/// Swap elements in some fashion based on the provided state parameter
	/// </summary>
	/// <param name="state"></param>
	/// <exception cref="NotImplementedException"></exception>
	public abstract void swap(string state);
}