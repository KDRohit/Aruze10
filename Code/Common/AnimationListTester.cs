using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class made to allow animators to test AnimationListController.AnimationInformationList's to see
 * how they will look.  Based on a tool script (AnimSequencer.cs) originally written by Carl Gloria.
 *
 * Original Author: Scott Lepthien
 * Creation Date: April 19, 2019
 */
public class AnimationListTester : TICoroutineMonoBehaviour
{
	public AnimationTestSequence[] AnimList;

	void Update()
	{
#if UNITY_EDITOR
		if (AnimList != null)
		{
			AnimationTestSequence seq;
			for (int i = 0; i < AnimList.Length; i++)
			{
				seq = AnimList[i];
				if (Input.GetKeyDown(seq.testKeyCode) && !seq.isPlaying)
				{
					StartCoroutine(seq.play());
				}
			}
		}
#else
		// if this is outside of the Editor then just destroy this script right away
		// because it shouldn't ever be used for actual game stuff.
		if (Application.isPlaying)
		{
			Destroy(this);
		}
#endif	
	}
}

[Serializable]
public class AnimationTestSequence
{
	[Header("Key to play sequence")]
	public KeyCode testKeyCode;

	[Tooltip("Animations to play.")]
	[SerializeField] private AnimationListController.AnimationInformationList animations;

	private bool _isPlaying = false;
	public bool isPlaying
	{
		get { return _isPlaying; }
	}

	public IEnumerator play()
	{
		_isPlaying = true;
		yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(animations));
		_isPlaying = false;
	}
}
