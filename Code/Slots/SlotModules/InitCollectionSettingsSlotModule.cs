using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Initialize collection settings, for example, reset a collection or cycle collections.
Intended to be extended into other SlotModules that want to use the data/functionality this provides.

Original Author: Scott Lepthien
Creation Date: 04/27/2018
*/
public class InitCollectionSettingsSlotModule : SlotModule 
{
	[System.Serializable]
	public class CollectionSetting
	{
		public string name;
		[Tooltip("Should the collection be reset to start from the beginning again")]
		public bool shouldResetCollection;
		[Tooltip("Should the collection cycle back to the start upon completion")]
		public bool shouldCycleCollection;
	}

	[SerializeField] private CollectionSetting[] collectionSettings;

	// Apply the settings specified to the collections
	protected void applyCollectionSettings()
	{
		foreach (CollectionSetting collectionSetting in collectionSettings)
		{
			if (collectionSetting.shouldResetCollection)
			{
				Audio.resetCollectionBySoundMapOrSoundKey(collectionSetting.name);
			}
			
			Audio.setCollectionCycling(collectionSetting.name, collectionSetting.shouldCycleCollection);
		}
	}
}
