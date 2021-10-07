using System.Collections;
using Com.HitItRich.Feature.TimedBonus;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Com.HitItRich.Feature.VirtualPets
{
	public class VirtualPetsCollectButtonOverlay : TICoroutineMonoBehaviour
	{	
		[SerializeField] private ClickHandler clickHandler;
		[SerializeField] private TextMeshPro pointsLabel;
		[SerializeField] private UIMeterNGUI meter;
		[SerializeField] private GameObject coinTarget;
		[SerializeField] private float maxMeterAnimationTime;
		[SerializeField] private AnimationListController.AnimationInformationList onAnimation;
		[SerializeField] private AnimationListController.AnimationInformationList collectAnimation;

		public UnityEvent onAnimationCompleteEvent;

		private long totalWin = 0;

		private void Awake()
		{
			if (clickHandler != null)
			{
				clickHandler.registerEventDelegate(onClick);
			}

			if (coinTarget != null && OverlayTopHIRv2.instance != null)
			{
				Vector3 overlayCoinTransform = Overlay.instance.topV2.coinAnchor.position;
				coinTarget.transform.position = overlayCoinTransform;
			}

			if (onAnimation != null)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(onAnimation));
			}
		}

		public void onBonusDataRecieved(TimedBonusFeature data)
		{
			if (data == null || data.lastCollectedBonus == null)
			{
				Debug.LogError("Invalid bonus data");
				NGUIExt.enableAllMouseInput(); 
				return;
			}
			
			totalWin = data.lastCollectedBonus.totalWin;

			StartCoroutine(winAnimation());
		}

		private IEnumerator winAnimation()
		{
			if (pointsLabel != null)
			{
				string creditDisplay = CreditsEconomy.convertCredits(totalWin);
				pointsLabel.text = creditDisplay;	
			}

			float time = maxMeterAnimationTime;
			if (meter != null)
			{
				meter.setState(totalWin, totalWin, true, time);	
			}

			if (collectAnimation != null)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(collectAnimation));
			}
			else
			{
				yield return new WaitForSeconds(time);	
			}
			
			//re-enable  input
			NGUIExt.enableAllMouseInput();
			
			//add credits
			SlotsPlayer.addNonpendingFeatureCredits(totalWin, "pet bonus collect", true);
			
			onAnimationCompleteEvent.Invoke();
		}

		public void onClick(Dict args)
		{
			NGUIExt.disableAllMouseInput();
			
			TimedBonusFeature.petCollectBonus(GameTimer.currentTime);
			
			StatsManager.Instance.LogCount(
				counterName: "bottom_nav",
				kingdom: "pet",
				phylum: "db_icon",
				family: "collect_fetch_db",
				milestone: VirtualPetsFeature.instance.isHyper ? "hyper_on" : "hyper_off",
				val: VirtualPetsFeature.instance.currentEnergy,
				genus: "click"
			);
		}
		
		

		private void OnDestroy()
		{
			if (clickHandler != null)
			{
				clickHandler.unregisterEventDelegate(onClick);
			}
		}
	}    
}

