using UnityEngine;
// Simple class that when attached to an object will cause the art check tool to 
// not check this object or its children.
public class IgnoreDuringArtCheck : TICoroutineMonoBehaviour
{
	public bool skipChildren = true;
}