using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
	public interface IProvider
	{
		ProvidableObject provide(FeatureConfig featureConfig, ProvidableObjectConfig providableObjectConfig, JSON json = null, bool logError = true);

	}
}
