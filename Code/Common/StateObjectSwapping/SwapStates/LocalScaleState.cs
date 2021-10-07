using UnityEngine;

/// <summary>
/// Assigned Vector3 local position to use for state driven tech leveraging ObjectSwap classes
/// </summary>
[System.Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class LocalScaleState : SwapperState
{
	public Vector3 localScale;
}