using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.HitItRich.EUE
{
	public class EueFtueExperiment : EosExperiment
	{
		public int maxLevel { get; private set; } 
		public EueFtueExperiment(string name) : base(name)
		{
		}

		protected override void init(JSON data)
		{
			maxLevel = getEosVarWithDefault(data, "level_to_unlock_eue_feature", 5);
		}

		public override void reset()
		{
			maxLevel = 5;
		}
	}    
}

