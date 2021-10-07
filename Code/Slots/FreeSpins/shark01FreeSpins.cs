using UnityEngine;
using System.Collections;

public class shark01FreeSpins : SpinPickFreeSpins 
{
	public GameObject flyingShark;
	public GameObject tornado;
	public GameObject wildTornadoText;
	public UILabel titleText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent titleTextWrapperComponent;

	public LabelWrapper titleTextWrapper
	{
		get
		{
			if (_titleTextWrapper == null)
			{
				if (titleTextWrapperComponent != null)
				{
					_titleTextWrapper = titleTextWrapperComponent.labelWrapper;
				}
				else
				{
					_titleTextWrapper = new LabelWrapper(titleText);
				}
			}
			return _titleTextWrapper;
		}
	}
	private LabelWrapper _titleTextWrapper = null;
	
	
	private PlayingAudio tornadoLoop;
	private PlayingAudio sharkLoop;


	// Constant Variables
	private const float SHARK_FLYING_TIME = 0.7f;
	private const float TORNADO_TRAVEL_TIME = 3.5f;
	// Sound names
	private const string INTRO_VO = "FreespinIntroVOSharknado";
	private const string SUMMARY_SCREEN_VO = "FNOKShouldBeSmoothSailingFromHere";

	public override void initFreespins()
	{
		// Override some of the sound names from the parent.
		REVEAL_AMOUNT_LANDED_SOUND = "TornadoSharkSplatTurnWild";
		REVEAL_OTHER_SOUND = "TornadoSharkWhoosh";
		Audio.play(INTRO_VO);
		base.initFreespins();
	}

	public override IEnumerator startPickem()
	{
		Audio.play("TornadoInitiator");
		tornado.SetActive(true);
		// Let's give the tornado time to travel down and spin before showing anything.
		yield return new TIWaitForSeconds(TORNADO_TRAVEL_TIME);
		sharkLoop = Audio.play("FreespinSharknadoMiniPickBg", 1, 0, 0, float.PositiveInfinity);
		Audio.switchMusicKey("FreespinSharknadoMiniPickBg");

		titleTextWrapper.text = Localize.text("pick_a_shark");
		yield return StartCoroutine(base.startPickem());
	}

	public override void pickemClicked(GameObject go)
	{
		Audio.switchMusicKey("BaseSharknado");
		// TODO: should this be playing the music right away?
		base.pickemClicked(go);
	}
	
	// Our first shark reveal, on the shark we clicked.
	protected override IEnumerator revealPickem(int index)
	{
		// We are pretty lame and don't have a reveal animation here. :(
		pickemObjects[index].SetActive(false);
		Audio.play("PickShark");
		titleTextWrapper.text = Localize.text("free_spins");
		yield return StartCoroutine(base.revealPickem(index));
	}
	
	protected override IEnumerator revealOtherPickem(int index, long value)
	{
		// Another lame no animation reveal :(
		yield return StartCoroutine(base.revealOtherPickem(index, value));
	}

	protected override void cleanUpPickem()
	{
		base.cleanUpPickem();
		tornadoLoop = Audio.play("FreespinSharknadoWildTornadoLoop", 1, 0, 0, float.PositiveInfinity);
		wildTornadoText.SetActive(true);
		Audio.stopSound(sharkLoop);
	}

	protected override IEnumerator handleTWSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < mutations.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < mutations.triggerSymbolNames.GetLength(1); j++)
			{
				if (mutations.triggerSymbolNames[i,j] != null && mutations.triggerSymbolNames[i,j] != "")
				{
					SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
				
					flyingShark.transform.localPosition = Vector3.zero;
					flyingShark.SetActive(true);
									
					Vector3 startPosition = flyingShark.transform.position;
					Vector3 endPosition = symbol.animator.gameObject.transform.position;
					Spline arcSpline = new Spline();
				
					Vector3 quarterDistance = (endPosition - startPosition) / 4;
					arcSpline.addKeyframe(0, 0, 0, flyingShark.transform.position);
					arcSpline.addKeyframe(20/4, 0.5f, 0, new Vector3(quarterDistance.x + startPosition.x, quarterDistance.y + startPosition.y + 0.3f, startPosition.z));
					arcSpline.addKeyframe((20/4) * 2, 1, 0, new Vector3(quarterDistance.x * 2 + startPosition.x, quarterDistance.y * 2 + startPosition.y + 0.50f, startPosition.z));
					arcSpline.addKeyframe((20/4) * 3, 0.5f, 0, new Vector3(quarterDistance.x * 3 + startPosition.x, quarterDistance.y * 3 + startPosition.y + 0.3f, startPosition.z));
					arcSpline.addKeyframe(20, 0, 0, endPosition);
					arcSpline.update();
				
					float elapsedTime = 0.0f;

					while (elapsedTime <= SHARK_FLYING_TIME)
					{
						flyingShark.transform.position = arcSpline.getValue(20 * (elapsedTime/SHARK_FLYING_TIME));
						yield return null;
						elapsedTime += Time.deltaTime;
					}
					
					Audio.play("TornadoSharkSplatTurnWild1");
					flyingShark.transform.position = endPosition;
					flyingShark.SetActive(false);
				
					symbol.mutateTo(mutations.triggerSymbolNames[i,j]);
				}
			}
		}
	}

	protected override void gameEnded()
	{
		wildTornadoText.SetActive(false);
		Audio.stopSound(tornadoLoop);
		Audio.play(SUMMARY_SCREEN_VO);
		base.gameEnded();
	}
}

