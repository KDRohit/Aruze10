using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/**
Inspector class used to edit UISpriteAnimators.
*/
[CustomEditor(typeof(UISpriteAnimator))]
public class UISpriteAnimatorInspector : TICoroutineMonoBehaviourEditor
{
	private bool previewAnimInEditor = false;					// Controls if the animation preview slider control will be shown
	private int currentPreviewFrame = 0;						// Current frame being previewed

	private List<int> frames = new List<int>();					// The list of frames that can be viewed, store my own copy here since UISpriteAnimator builds it every time you play the animation
	private long usedHashedFrameTotal = -1;						// Track a total value of hashed frame values so we can detect if frameData was modified which means the preview should be updated

	/// Draw the inspector widget.
	public override void OnInspectorGUI ()
	{
		UISpriteAnimator animator = target as UISpriteAnimator;

		bool isShowPreviewModified = previewAnimInEditor;

		// disable showing preview if the game is running, to avoid strangness with altering the animation when it plays
		if (!Application.isPlaying)
		{
			previewAnimInEditor = EditorGUILayout.Toggle("Preview Animation", previewAnimInEditor);
		}
		else
		{
			previewAnimInEditor = false;
		}

		isShowPreviewModified = (isShowPreviewModified != previewAnimInEditor);

		if (previewAnimInEditor == true)
		{
			long newHashedFrameTotal = getHashedFrameDataTotal(animator);

			// if frameData has changed or the preview was just turned on, then rebuild the frame info to be used for the slider
			if (isShowPreviewModified || (usedHashedFrameTotal != newHashedFrameTotal))
			{
				initFramData(animator);
				usedHashedFrameTotal = newHashedFrameTotal;
				currentPreviewFrame = 0;
				changePreviewFrame(animator, 0);
			}
		}

		if (previewAnimInEditor)
		{
			// preview is turned on, show a slider assuming there are actually frames to show
			if (frames.Count > 0)
			{
				currentPreviewFrame = EditorGUILayout.IntSlider ("Current Frame", currentPreviewFrame, 0, frames.Count - 1);
				changePreviewFrame(animator, frames[currentPreviewFrame]);
			}
			else
			{
				EditorGUILayout.LabelField("Frame Count is 0!");
			}
		}
		else
		{
			// preview is turned off, reset the animation to display as normal
			currentPreviewFrame = 0;
			changePreviewFrame(animator, 0);
		}
		
		base.OnInspectorGUI();
	}

	/// Changes what frame is currently being previewed
	private void changePreviewFrame(UISpriteAnimator animator, int frameNum)
	{
		animator.sprite.spriteName = string.Format("{0} {1}", animator.spriteBaseName, frameNum);
		animator.sprite.MakePixelPerfect();
	}

	/// Create a preview set of frames that will match up with how the frames will be built at run-time
	private void initFramData(UISpriteAnimator animator)
	{
		List<UISpriteAnimator.FrameMap> frameMap = new List<UISpriteAnimator.FrameMap>();

		UISpriteAnimator.updateFrameMap(frameMap, animator.frameData, animator.gameObject);

		// Flatten the frame map once before animating, for performance during animation.
		frames.Clear();
		frames = UISpriteAnimator.buildFrameList(frameMap);
	}

	/// Calculate a total hashed amount based on the frames in animator.frameData, used to determine if the frame data was modified
	private long getHashedFrameDataTotal(UISpriteAnimator animator)
	{
		long total = 0;

		foreach (string rawData in animator.frameData)
		{
			total += rawData.GetHashCode();
		}

		return total;
	}
}