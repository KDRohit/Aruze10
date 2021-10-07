using System;
using UnityEngine;

/// <summary>
/// Assigned material to use for state driven tech leveraging ObjectSwap classes
/// </summary>
[Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class MaterialState : SwapperState
{
	public Material materialToUse;
}