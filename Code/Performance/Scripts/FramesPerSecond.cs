using UnityEngine;
using System.Collections;

public class FramesPerSecond : TICoroutineMonoBehaviour
{
	// Attach this to any object to make a frames/second indicator.
	//
	// It calculates frames/second over each updateInterval,
	// so the display does not keep changing wildly.
	//
	// It is also fairly accurate at very low FPS counts (<10).
	// We do this not by simply counting frames per interval, but
	// by accumulating FPS for each frame. This way we end up with
	// corstartRect overall FPS even if the interval renders something like
	// 5.5 frames.

	private int frequency = 1; // updates per second
	public int nbDecimal = 1; // How many decimal do you want to display
	public Color color = Color.white; // The color of the GUI, depending of the FPS ( R < 10, Y < 30, G >= 30 )

	private float accum   = 0f; // FPS accumulated over the interval
	private int   frames  = 0; // Frames drawn over the interval
	private string sFPS = ""; // The fps formatted into a string.

	public string FPS
	{
		get
		{
			return sFPS;
		}
	}

	protected override void OnDisable()
	{
		this.accum = 0;
		this.frames = 0;
		sFPS = "";
		
		base.OnDisable();
	}

	void Update()
	{
		accum += Time.deltaTime;
		++frames;

		if (accum >= 1/frequency)
		{
			CalculateFPS();
		}
	}

	private void CalculateFPS()
	{
		// Update the FPS
		float fps = 0;
		if (frames > 0)
		{
			fps = frames/(accum/Time.timeScale);
			frames = 0;
		}
		accum -= 1 / frequency;

		long memory = MemoryHelper.GetMemoryResidentBytes() / (1024 * 1024);
		sFPS = string.Format("FPS: {0:0.0}  Memory: {1}", fps, memory);

		float targetFPS;
#if UNITY_WEBGL
			targetFPS = 60f;
#else
		targetFPS = Application.targetFrameRate;
#endif

		//Update the color
		color = (fps >= (targetFPS * 0.75)) ?
			Color.green :
			((fps > (targetFPS * 0.5)) ?
				Color.yellow :
				Color.red);

		accum = 0.0F;
		frames = 0;
	}
	
}
