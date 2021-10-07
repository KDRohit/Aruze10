using UnityEngine;
using System.Collections;
using TMPro;

/**
 * Attach to a prefab with a TextMeshPro on it and initialize it with init to show text slowly moving to the specified destination and fade away.
 */
public class RisingText : TICoroutineMonoBehaviour
{
    public TextMeshPro label;

    private float lifeDuration = 1f; //< how long the text will live for
    private float fadeDuration = 0.5f; //< the amount of the text's life that will be spent fading away
    private bool initialized = false; //< so the update loop doesn't start immediately
    private float spawnTime; //< when the text was initialized and started animating
    private float current; //< the current amount of time that has been spent since init was called
    private Color defaultLabelColor; //< what color the label started as
    private Color defaultLabelColorTransparent; //< the starting color with 100% transparency
    private Vector3 localStartPosition; //< the local position the text should start in once the transform has been reparented
    private Vector3 localEndPosition; //< the local lerp destination

	// Use this for initialization
    public void init(string text, Transform parent, Vector3 localStartPosition, Vector3 localEndPosition, float lifeDuration = 1f, float fadeDuration = 0.5f)
    {
        this.lifeDuration = lifeDuration;
        this.fadeDuration = fadeDuration;
        this.localStartPosition = localStartPosition;
        this.localEndPosition = localEndPosition;


        Vector3 scale = transform.localScale;
        transform.parent = parent;
        transform.localScale = scale;
        transform.localPosition = localStartPosition;
        spawnTime = Time.time;
        label.text = text;
        defaultLabelColor = label.color;
        defaultLabelColorTransparent = new Color(defaultLabelColor.r, defaultLabelColor.g, defaultLabelColor.b, 0);
        initialized = true;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (initialized)
        {
            current = Time.time - spawnTime;

            transform.localPosition = Vector3.Lerp(this.localStartPosition, this.localEndPosition, current / lifeDuration);

            if (current >= lifeDuration - fadeDuration)
            {
                label.color = Color.Lerp(defaultLabelColor, defaultLabelColorTransparent, (current - (lifeDuration - fadeDuration)) / fadeDuration);
            }

            if (current >= lifeDuration)
            {
                // Destroy yourself when you are done.
                Destroy(gameObject);
            }
        }
	}
}
