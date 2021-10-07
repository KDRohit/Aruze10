// Defined to enable OnGUI buttons for sizing.
// Run the app and play a slot game.
// In the Hierarchy, select an expanding reel symbol.
// (eg, in Dark Desires, they are LLS01 M1 LG .. LLS01 M4 LG).
// In the inspector, enable the ExpandingReelSymbolBase derived script.
// Size buttons and a squished symbol will appear in the game.
// Press the buttons to preview the symbol at different sizes.
//#define ENABLE_SIZING_UI

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is the base controller for expanding reel symbols, allows for one texture to be used for 1x1, 1x2, 1x3, 3x3, etc..
Can come in two versions, one where the sizes are built using sliced sprites in code, and ones where the sizes are pre-built by art
*/
public abstract class ExpandingReelSymbolBase : TICoroutineMonoBehaviour
{
	private const int BUTTON_Y_SPACING = 60;

	[SerializeField] public float fadeTime;				// Time the symbol takes to fade in and out
	[SerializeField] private string majorName = "";		// Used for appending onto a sound call to generate the full sound name, example: "M1", "M2", "M3", etc...

#if UNITY_EDITOR && ENABLE_SIZING_UI
	private List<SupportedSize> supportedSizeList = null;	// Supported sizes cached from the init function
#endif

	protected const string BLACKOUT_SOUND = "WinFlourish4OfAKind";
	protected const string LLS_EXPANDING_SOUND = "LWExpandReels";
	protected const string EE01_EXPANDING_SOUND = "BBExpandReels";
	protected const string DUCKDYN02_EXPANDING_SOUND = "FreespinSymbolInitDuck02";
	protected const string ZYNGA02_EXPANDING_SOUND = "FreespinSymbolInitCastleville";

	protected const string MAJOR_SYMBOL_EXPAND_ANIM_SOUND = "major_symbol_expand_anim";
	protected const string MAJOR_SYMBOL_EXPAND_FREESPIN_ANIM_SOUND = "major_symbol_expand_freespin_anim";
	protected const string MAJOR_SYMBOL_EXPAND_SYMBOL_SPECIFIC_SOUND = "major_symbol_expand_";
	
    protected virtual void Awake()
    {
        this.init();
    }

#if UNITY_EDITOR && ENABLE_SIZING_UI
    void OnGUI()
    {
    	int buttonYPos = 70;
    	foreach (SupportedSize sizeInfo in supportedSizeList)
    	{
    		if (GUI.Button(new Rect(10, buttonYPos, 80, 50), sizeInfo.sizeKey))
			{
				transform.position = ReelGame.activeGame.getOverlayPosFromSize(sizeInfo.cellsWide, sizeInfo.cellsHigh);
				setSize(ReelGame.activeGame, sizeInfo.cellsWide, sizeInfo.cellsHigh);
			}

			buttonYPos += BUTTON_Y_SPACING;
    	}
	}
#endif

    /// Init function, handle stuff needed for the expanded symbols to function
    public virtual void init()
    {
#if UNITY_EDITOR && ENABLE_SIZING_UI
		supportedSizeList = getSupportedSizeList();
#endif

		this.enabled = false;
	}

    /// Sets the size for the overlay
	abstract public void setSize(ReelGame reelGame, int cellsWide, int cellsHigh);

	// Play the sound for the expanding symbol
	protected void playExpandingSymbolSound(int cellsWide, bool isFreespins)
	{
		if (cellsWide == 5)
		{
			Audio.play(ExpandingReelSymbolBase.BLACKOUT_SOUND);
		}
		else
		{
			if (isFreespins && Audio.canSoundBeMapped(MAJOR_SYMBOL_EXPAND_FREESPIN_ANIM_SOUND))
			{
				// play the free spin version
				Audio.play(Audio.soundMap(MAJOR_SYMBOL_EXPAND_FREESPIN_ANIM_SOUND));
			}
			else
			{
				// play the base/standard version
				Audio.play(Audio.soundMap(MAJOR_SYMBOL_EXPAND_ANIM_SOUND));
			}

			// check if we should try playing a symbol specific sound in addition to the expand sound
			if (majorName != "")
			{
				Audio.play(Audio.soundMap(MAJOR_SYMBOL_EXPAND_SYMBOL_SPECIFIC_SOUND + majorName));
			}
		}
	}

	/// Get a list of the supported sizes for this symbol
	abstract protected List<SupportedSize> getSupportedSizeList();

	/// Set the alpha for the symbol
	abstract protected void setSymbolAlpha(float alpha); 

	/// fade the Expanded symbol in
	public IEnumerator doShow()
	{
		setSymbolAlpha(0.0f);

		float remaining = fadeTime;
		while (remaining > 0)
		{
			float percent = 1f - remaining / fadeTime;

			setSymbolAlpha(percent);

			this.enabled = true;
			yield return null;

			remaining -= Time.deltaTime;
		}

		setSymbolAlpha(1.0f);
	}

	// fade the Expanded symbol out
	public IEnumerator doHide()
	{
		float remaining = fadeTime;
		while (remaining > 0)
		{
			float percent = 1f - remaining / fadeTime;
			
			setSymbolAlpha(1.0f - percent);
			
			yield return null;

			remaining -= Time.deltaTime;
		}

		setSymbolAlpha(0.0f);
		
		this.enabled = false;
	}

	/// This will be used to hold data for the supported sizes of this expanding symbol
	public class SupportedSize
	{	
		public SupportedSize(int width, int height, string key)
		{
			cellsWide = width;
			cellsHigh = height;
			sizeKey = key;
		}

		public int cellsWide; // Defines the number of cells wide this data is authored for
		public int cellsHigh; // Defines the number of cells high this data is authored for
		public string sizeKey = null;	// key of the supported size represented as "widthxheight"
	}
}
