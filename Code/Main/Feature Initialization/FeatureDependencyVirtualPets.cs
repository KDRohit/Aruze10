using System.Collections;
using System.Collections.Generic;
using Com.Initialization;
using UnityEngine;

namespace Com.HitItRich.Feature.VirtualPets
{
	public class FeatureDependencyVirtualPets : FeatureDependency
	{
		/// <inheritdoc/>
		public override void init()
		{
			base.init();
			VirtualPetsFeature.instantiateFeature(Data.login.getJSON(VirtualPetsFeature.LOGIN_DATA_KEY));
		}

		/// <inheritdoc/>
		public override bool isSkipped
		{
			get { return Data.hasLoginData && Data.login.getJSON(VirtualPetsFeature.LOGIN_DATA_KEY) == null; }
		}

		public override bool canInitialize
		{
			get
			{
				return base.canInitialize &&
				       Data.hasLoginData &&
				       Data.login.getJSON(VirtualPetsFeature.LOGIN_DATA_KEY) != null &&
				       ExperimentWrapper.VirtualPets.isInExperiment;
			}
		}
	}    
}

