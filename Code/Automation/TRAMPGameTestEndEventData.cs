using UnityEngine;
using System.Collections;

// This is a data containter for the information about a spin
// that is then submitted to Splunk
#if ZYNGA_TRAMP
public class TRAMPGameTestEndEventData
{
	public System.DateTime EndTime;
	public string GameKey;
	public string GameName;
}
#endif
