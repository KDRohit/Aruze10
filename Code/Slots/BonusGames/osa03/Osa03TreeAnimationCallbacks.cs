// Small class whose entire purpose in life is to flag that the tree throw animation is ready
using UnityEngine;
using System.Collections;

public class Osa03TreeAnimationCallbacks : MonoBehaviour {
	
	[SerializeField] private GameObject treeApple;

	private bool _isThrowReady = false;

	public bool isThrowReady
	{
		get { return _isThrowReady; }
	}

	public void flagThrowIsReady()
	{
		_isThrowReady = true;
	}

	public void treeReloadBeginCallback()
	{
		_isThrowReady = false;
		treeApple.SetActive(true);
	}
}
