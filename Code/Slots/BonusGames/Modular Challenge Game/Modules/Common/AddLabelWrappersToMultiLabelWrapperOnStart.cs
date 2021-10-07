using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * Component to add to GameObjects with LabelWrappers controlled by a MultiLabelWrapperComponent that get cloned at runtime.
 * Most notable example is for AnimatedParticleEffects. This ensures any new LabelWrappers created will still be synced
 * to the root MultiLabelWrapper.
 *
 * Author: Caroline 4/2020
 */
public class AddLabelWrappersToMultiLabelWrapperOnStart : MonoBehaviour
{
	[SerializeField] private List<LabelWrapperComponent> labelsToAdd;
	[SerializeField] private MultiLabelWrapperComponent multiLabelParent;
	
	// using Start because only called once and we don't want to add the same labels multiple times
	void Start()
	{
		if (multiLabelParent != null)
		{
			multiLabelParent.addLabelWrappersToSyncLabels(labelsToAdd);
		}
	}

	// be sure to cleanup labels from parent so we don't get null references
	private void OnDestroy()
	{
		if (multiLabelParent != null)
		{
			multiLabelParent.removeLabelWrappersFromSyncLabels(labelsToAdd);
		}
	}
}
