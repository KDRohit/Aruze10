using UnityEngine;
public class ToggleManager : MonoBehaviour
{
	public ToggleHandler[] handlers;

	public delegate void onToggleChanged(ToggleHandler handler);
	
	public event onToggleChanged onChanged;
	
	public void init (onToggleChanged callback)
	{
		onChanged += callback;
		for (int i = 0; i < handlers.Length; i++)
		{
			handlers[i].init(this, i);
		}
	}

	public void toggle(ToggleHandler handler)
	{
		toggle(handler.index);
	}
	
	public void toggle(int index)
	{	
		for (int i = 0; i < handlers.Length; i++)
		{
			handlers[i].setToggle(index == i);
		}
		onChanged(handlers[index]);
	}
}