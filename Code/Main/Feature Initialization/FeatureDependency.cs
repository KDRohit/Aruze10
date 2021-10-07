using UnityEngine;
using System.Collections;

namespace Com.Initialization
{
	public abstract class FeatureDependency : IFeatureDependency
	{
		/// <summary>
		/// Returns true when the feature is initialized successfully
		/// </summary>
		public virtual bool isInitialized { get; protected set; }

		/// <summary>
		/// Returns true if the feature failed initialization. This should be set when all criteria is met
		/// but for X reasons is disabled
		/// </summary>
		public virtual bool isSkipped { get; protected set; }

		/// <summary>
		/// Main entry for the feature
		/// </summary>
		public virtual void init()
		{
			isInitialized = true;
		}

		/// <summary>
		/// Set criteria needed for initialization
		/// </summary>
		public virtual bool canInitialize
		{
			get { return !isInitialized; }
		}
	}
}