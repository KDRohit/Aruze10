using UnityEngine;
using System.Collections;
using TMPro;
public class VIPStatusBoostProfileButtonAnimation : MonoBehaviour
{
	public Animator gemCycleAnimation;
	public VIPIconHandler vipIconHandler;
	public UISprite gemSprite; // Linking this so that I can manually set the size until I send this back to art.
	//public TextMeshPro boostedText; Might be useful if we ever need to display it, but thus far we do not.

	private const string IDLE_GEM = "gemIdle";
	private const string SHRINK_GEM = "gemShrink";
	private const string GROW_GEM = "gemGrow";
    private const float GEM_START_TIME = 5.0f;
    private const float GEM_SWITCH_DELAY_TIME = 0.75f;

	private Vector3 originalGemPosition;

	void Awake()
	{
		originalGemPosition = new Vector3(0, 0, gemSprite.transform.localPosition.z);
		
		// Keep the animation from playing if the event isn't on.
		vipIconHandler.setToPlayerLevel();
		gemSprite.transform.localPosition = originalGemPosition;
		gemCycleAnimation.Play(IDLE_GEM); 

		if (VIPStatusBoostEvent.isEnabled())
		{
			// Play the animation loop if the event is on (cycles between the current and boosted gem)
			StartCoroutine(cycleBoostedGem());
		}

	    Server.registerEventDelegate("vip_level_up", onVIPLevelUp, true);
	}

	// This plays once. For...reasons.
	private IEnumerator cycleBoostedGem()
    {
        // Wait a few seconds to show the animation so people actually see it.
        yield return new WaitForSeconds(GEM_START_TIME);

		// Init to 0, it gets set dynamically in the loop below.
		int realGemLevel = SlotsPlayer.instance.vipNewLevel; 
		int fakeGemLevel = SlotsPlayer.instance.vipNewLevel + VIPStatusBoostEvent.fakeLevel;

		vipIconHandler.setLevel(realGemLevel);
		gemSprite.transform.localPosition = originalGemPosition;

		gemCycleAnimation.Play(SHRINK_GEM);

		yield return new WaitForSeconds(GEM_SWITCH_DELAY_TIME);

		vipIconHandler.setLevel(fakeGemLevel);
		gemSprite.transform.localPosition = originalGemPosition;
		
		gemCycleAnimation.Play(GROW_GEM);

		yield return null;
	}

    private void onVIPLevelUp(JSON data)
    {
        int level = data.getInt("vip_level", 0);

        // ON LEVEL UP, SHOW NEW LEVEL
        int fakeGemLevel = level + VIPStatusBoostEvent.fakeLevel;

        if (fakeGemLevel > VIPLevel.maxLevel.levelNumber)
        {
            fakeGemLevel = VIPLevel.maxLevel.levelNumber;
        }

        vipIconHandler.setLevel(fakeGemLevel);

		if (gemSprite != null)
		{
			gemSprite.transform.localPosition = originalGemPosition;
		}
    }
}