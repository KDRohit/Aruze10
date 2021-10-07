using UnityEngine;
using System.Collections;


//This class uses the AudioListController.AudioInformationList to properly time multiple sounds with the portal transition, rather then just having one sound play like the old portals.
public class Lis01Portal : PickPortal
{
	//This list includes five foleys
	[SerializeField] private AudioListController.AudioInformationList tranistionSounds;
	
	public override void init()
	{		
		StartCoroutine(AudioListController.playListOfAudioInformation(tranistionSounds));
		base.init();
	}
}
