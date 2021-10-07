using UnityEngine;
using System.Collections;

/*
Initialize collection settings, for example, reset a collection or cycle collections.
*/
public class ChallengeGameInitCollectionSettingsModule : ChallengeGameModule
{	
	[System.Serializable]
	public class CollectionSetting
	{
		public string name;
		
		public bool shouldResetCollection; // Every time you play the pickem, should it reset the collection to the first sound?
		public bool shouldCycleCollection; // After it plays every sound in the collection, should it cycle again, or stop playing?
	}

	public CollectionSetting[] collectionSettings;
	
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
		
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
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
