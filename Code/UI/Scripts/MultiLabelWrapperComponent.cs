using UnityEngine;
using System.Collections.Generic;

/**
Sync labels so they always have the same text.
*/

[ExecuteInEditMode]
public class MultiLabelWrapperComponent : LabelWrapperComponent
{
	[SerializeField] private LabelWrapperComponent[] syncLabels = new LabelWrapperComponent[1];
	private MultiLabelWrapper _multiLabelWrapper = new MultiLabelWrapper();
	// list of LabelWrapperComponents we want to update which get added or removed at runtime and aren't serialized
	private List<LabelWrapperComponent> dynamicallyAddedSyncLabels = new List<LabelWrapperComponent>();
	// pre-allocated list to reduce garbage when refreshing MultiLabelWrapper
	private List<LabelWrapper> labelWrappers = new List<LabelWrapper>();

	public override LabelWrapper labelWrapper
	{
		get
		{
			if (_multiLabelWrapper != null)
			{
				// trying to access multilabelwrapper before awake, make sure it's initialized
				if (!_multiLabelWrapper.isInitialized)
				{
					refreshMultiLabelWrapper();
				}
				return _multiLabelWrapper;
			}

			return _labelWrapper;
		}
	}
	
	// Remember what the text was, that way if you change it in an inspector field,
	// it can change the other sync labels text, too.
	private string textWas = "";
	
	public override string text
	{
		set
		{
			if (!isValidMultiLabel())
			{
				return;
			}
			
			labelWrapper.text = value;
			
			textWas = value;
		}

		get
		{
			return base.labelWrapper.text;
		}
	}

	protected new void OnEnable()
	{
		// Make sure all the labels are synced.
		if (labelWrapper != null)
		{
			text = labelWrapper.text;
		}
	}
	
	private bool isValidMultiLabel()
	{
		bool isValid = true;
		
		if (Application.isPlaying)
		{
			if (syncLabels == null || syncLabels.Length == 0)
			{
				Debug.LogWarning("MultiLabelWrapperComponent doesn't have any sync labels.", gameObject);
				return false;
			}
			
			foreach (LabelWrapperComponent syncLabel in syncLabels)
			{
				if (syncLabel == null)
				{
					Debug.LogWarning("MultiLabelWrapperComponent sync label is null.", syncLabel);
					isValid = false;
				}
				else
				{
					if (syncLabel is MultiLabelWrapperComponent)
					{
						Debug.LogWarning("Nested MultiLabelWrapperComponents are not allowed", syncLabel);
						isValid = false;
					}
				}
			}
		}
		
		return isValid;
	}

	protected override void Awake()
	{
		base.Awake();

		if (_multiLabelWrapper != null)
		{
			if (!_multiLabelWrapper.isInitialized)
			{
				refreshMultiLabelWrapper();
			}
		}
	}
	
	private void refreshMultiLabelWrapper()
	{
		labelWrappers.Clear();
		if (syncLabels != null)
		{
			for (int i = 0; i < syncLabels.Length; i++)
			{
				if (syncLabels[i] != null)
				{
					if (syncLabels[i] is MultiLabelWrapperComponent)
					{
#if UNITY_EDITOR
						//Syncing to another MultiLabelWrapper component can cause a stackOverflowException if the wrappers are trying to sync each other infinitely .
						Debug.LogError("Skipping syncing with another MultiLabelWrapperComponent. Please update syncLabels array in prefab");
						Debug.Break();
#endif
						continue;
					}
				
					labelWrappers.Add(syncLabels[i].labelWrapper);	
				}
			}
		}
		
		
		for (int i = 0; i < dynamicallyAddedSyncLabels.Count; i++)
		{
			if (dynamicallyAddedSyncLabels[i] is MultiLabelWrapperComponent)
			{
#if UNITY_EDITOR
				//Syncing to another MultiLabelWrapper component can cause a stackOverflowException if the wrappers are trying to sync each other infinitely .
				Debug.LogError("Skipping syncing with another MultiLabelWrapperComponent. Please update dynamicallyAddedSyncLabels list.");
				Debug.Break();
#endif
				continue;
			}
			
			labelWrappers.Add(dynamicallyAddedSyncLabels[i].labelWrapper);
		}
		_multiLabelWrapper.setLabelWrappers(_labelWrapper, labelWrappers);
	}

	public void addLabelWrappersToSyncLabels(List<LabelWrapperComponent> labelsToAdd)
	{
		dynamicallyAddedSyncLabels.AddRange(labelsToAdd);
		refreshMultiLabelWrapper();
	}

	public void removeLabelWrappersFromSyncLabels(List<LabelWrapperComponent> labelsToRemove)
	{
		foreach (LabelWrapperComponent label in labelsToRemove)
		{
			dynamicallyAddedSyncLabels.Remove(label);
		}
		refreshMultiLabelWrapper();
	}
	
	protected override void Update()
	{
		base.Update();
	
#if UNITY_EDITOR
		//mimic non-editor behavior when playing
		if(Application.isPlaying)
		{
			return;
		}
		
		refreshMultiLabelWrapper();
		
		// If you change the main label wrapper text in the editor,
		// then change all the sync labels text, too.
		if (labelWrapper.text != textWas)
		{
			text = labelWrapper.text;
		}
#endif
	}
}
