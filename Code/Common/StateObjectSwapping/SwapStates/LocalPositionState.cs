using UnityEngine;

/// <summary>
/// Assigned Vector3 local position to use for state driven tech leveraging ObjectSwap classes
/// </summary>
[System.Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class LocalPositionState : SwapperState
{
	[Tooltip("If 'Use Relative' is true, then '0' values will not change the object's current position on that axis")]
    public Vector3 localPosition;
	[Tooltip("False: Affected object will move to the above position\n\nTrue: Affected object's current position will be offset by this amount")]
	public bool useRelativePosition;
}
