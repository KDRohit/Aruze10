using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Zynga.Core.Util;

namespace Com.Initialization
{
	public class FeatureInitializer : IResetGame
	{
		/// <summary>
		/// List of features to start
		/// </summary>
		private static List<IFeatureDependency> initFeatures = new List<IFeatureDependency>();

		/// <summary>
		/// List of features that failed to start
		/// </summary>
		private static List<IFeatureDependency> failedFeatures = new List<IFeatureDependency>();

		/*=========================================================================================
		INIT
		=========================================================================================*/
		public static void init()
		{
			Type featureInterface = typeof(IFeatureDependency);
			Assembly asm = ReflectionHelper.GetAssemblyByName(ReflectionHelper.ASSEMBLY_NAME_MAP[ReflectionHelper.ASSEMBLIES.PRIMARY]);
			foreach (Type type in asm.GetTypes())
			{
				//ignore structs and other data types
				if (!type.IsClass() || type.IsAbstract)
				{
					continue;
				}
				
				//Add feature to init list if it inherits from the feature dependency interface
				if(featureInterface.IsAssignableFrom(type))
				{
					IFeatureDependency feature = Activator.CreateInstance(type) as IFeatureDependency;
					initFeatures.Add(feature);
				}
			}

			if (initFeatures.Count > 0)
			{
				if (RoutineRunner.instance != null)
				{
					RoutineRunner.instance.StartCoroutine(startup());
				}
				else
				{
					RoutineRunner.addCallback(onRunnerReady);
				}
			}
		}

		public static void onRunnerReady()
		{
			RoutineRunner.instance.StartCoroutine(startup());
		}

		/*=========================================================================================
		FEATURE HANDLING
		=========================================================================================*/
		public static IEnumerator startup()
		{
			while (!isDone)
			{
				for (int i = 0; i < initFeatures.Count; ++i)
				{
					IFeatureDependency featureDependency = initFeatures[i];

					if (failedFeatures.Contains(featureDependency))
					{
						continue;
					}

					if (featureDependency != null)
					{
						if (!featureDependency.isInitialized && featureDependency.canInitialize)
						{
							featureDependency.init();
						}
						else if (featureDependency.isSkipped)
						{
							failedFeatures.Add(featureDependency);
							Debug.LogWarningFormat("FeatureInitializer: Failed to start feature - {0}", featureDependency);
						}
					}
				}

				yield return null;
			}

			clean();
		}

		/// <summary>
		/// Removes all the failed features from the initilization list
		/// </summary>
		private static void clean()
		{
			for (int i = 0; i < failedFeatures.Count; ++i)
			{
				if (failedFeatures[i] != null && !failedFeatures[i].isInitialized && !failedFeatures[i].canInitialize)
				{
					remove(failedFeatures[i]);
				}
			}
		}

		/// <summary>
		/// Adds to the initialization list
		/// </summary>
		/// <param name="d"></param>
		public static void add(IFeatureDependency d)
		{
			if (initFeatures.Contains(d))
			{
				initFeatures.Add(d);
			}
		}

		/// <summary>
		/// Removes from the initialization list
		/// </summary>
		/// <param name="d"></param>
		public static void remove(IFeatureDependency d)
		{
			if (initFeatures.Contains(d))
			{
				initFeatures.Remove(d);
			}
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		/// <summary>
		/// Returns true once all FeatureDependency classes have been initialized
		/// </summary>
		public static bool isDone
		{
			get
			{
				for (int i = 0; i < initFeatures.Count; ++i)
				{
					if (!initFeatures[i].isInitialized && !initFeatures[i].isSkipped)
					{
						return false;
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Implements IResetGame
		/// </summary>
		public static void resetStaticClassData()
		{
			initFeatures.Clear();
			failedFeatures.Clear();
		}
	}
}