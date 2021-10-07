using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Scheduler
{
	public class CollectionsAlbumDialogTask : DialogTask
	{
		private bool waitingForCache = true;
		
		public CollectionsAlbumDialogTask(string dialogKey, Dict args = null) : base(dialogKey, args)
		{
		}
		
		public override void execute()
		{
			if (Collectables.missingBundles != null)
			{
				for (int i = 0; i < Collectables.missingBundles.Count; i++)
				{
					AssetBundleManager.downloadAndCacheBundle(Collectables.missingBundles[i]);
				}

				RoutineRunner.instance.StartCoroutine(waitForAssetBundlesAndExecute());
			}
			else
			{
				base.execute();
			}
		}

		private IEnumerator waitForAssetBundlesAndExecute()
		{
			while (waitingForCache)
			{
				waitingForCache = false;
				for (int i = 0; i < Collectables.missingBundles.Count; i++)
				{
					if (!AssetBundleManager.isBundleCached(Collectables.missingBundles[i]))
					{
						waitingForCache = true;
					}
				}
				yield return null;
			}
			
			base.execute();
		}
	}
}
