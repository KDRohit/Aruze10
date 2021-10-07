using UnityEngine;
using System.Collections;

public class InboxLimitValue<T>
{
	protected T limit;
	protected T valueRemaining;
	protected T valueCollected;

	public InboxLimitValue(T limit, T valueRemaining)
	{
		this.limit = limit;
		this.valueRemaining = valueRemaining;
	}

	/// <summary>
	/// Set the limit
	/// </summary>
	/// <param name="value"></param>
	public virtual void setLimit(T newValue)
	{
		this.limit = limit;
	}

	/// <summary>
	/// Increase the amount of remaining inbox collections
	/// </summary>
	/// <param name="value"></param>
	public virtual void add(T amount)
	{

	}

	/// <summary>
	/// Decrease the amount of remaining inbox collections
	/// </summary>
	/// <param name="value"></param>
	public virtual void subtract(T amount)
	{

	}

	/// <summary>
	/// Returns the limit
	/// </summary>
	public virtual T currentLimit
	{
		get { return limit; }
	}

	/// <summary>
	/// Amount of collection available
	/// </summary>
	public virtual int amountRemaining
	{
		get { return 0; }
	}

	/// <summary>
	/// Amount collected
	/// </summary>
	public virtual int amountCollected
	{
		get { return 0; }
	}
}