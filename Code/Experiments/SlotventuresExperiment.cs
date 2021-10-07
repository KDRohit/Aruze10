using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotventuresExperiment : EosVideoExperiment 
{
	public int levelLock { get; private set; }
	public bool isEUE { get; private set; } // if this is the EUE slotventures campaign
	public bool useDirectToMachine { get; private set; } // if we are sending the user straight to the current slotventures machine on lobby load
	public bool useDirectToLobby { get; private set; } // if we sending the user straight to the slotventures lobby on load
	public int maxDirectToLobbyLoads { get; private set; } // maximum number of times we are forcing the user to load into the slotventures lobby
	public SlotventuresExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		levelLock             = getEosVarWithDefault(data, "level_lock", 10);
		isEUE                 = getEosVarWithDefault(data, "is_eue", false);
		useDirectToMachine    = getEosVarWithDefault(data, "direct_to_machine", false);
		useDirectToLobby      = getEosVarWithDefault(data, "direct_to_lobby", false);
		maxDirectToLobbyLoads = getEosVarWithDefault(data, "max_direct_to_lobby", 0);
	}

	public override void reset()
	{
		base.reset();
		levelLock = 10;
		isEUE = false;
		useDirectToMachine = false;
		useDirectToLobby = false;
		maxDirectToLobbyLoads = 0;
	}
}
