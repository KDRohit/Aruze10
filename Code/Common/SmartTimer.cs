using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Com.States;

/*
SmartTimer is a replacement for using System.Timer. System.Timer using threading which is unable to run on WebGL
The class is pretty straight forward, create a new SmartTimer instance, and call start()/stop() as needed.
Setting repeat to true in the constructor will keep the timer going forever
*/
public class SmartTimer : IResetGame
{
	private float delay;
	private GenericDelegate callback;
	private bool repeat = false;
	private StateMachine stateMachine;
	private string name = "smart timer";
	private TICoroutine timerRoutine = null;

	private const string RUNNING = "running";
	private const string STOPPED = "stopped";
	private const string READY = "ready";

	private static List<SmartTimer> timers = new List<SmartTimer>();
	
    public SmartTimer(float delay, bool repeat = false, GenericDelegate callback = null, string name = "smart timer")
    {
		this.delay = delay;
		this.callback = callback;
		this.repeat = repeat;
		this.name = name;
		
		init();
    }

	private void init()
	{
		stateMachine = new StateMachine(name);
		stateMachine.addState(RUNNING);
		stateMachine.addState(STOPPED);
		stateMachine.addState(READY);
		stateMachine.updateState(READY);
	}

	public void start()
	{
		if (stateMachine.can(RUNNING))
		{
			return;
		}

		stateMachine.updateState(RUNNING);
		timerRoutine = RoutineRunner.instance != null ? RoutineRunner.instance.StartCoroutine(timerLoop()) : null;

		if (!timers.Contains(this))
		{
			timers.Add(this);
		}
	}

	public void stop()
	{
		stateMachine.updateState(STOPPED);

		if (RoutineRunner.instance != null && timerRoutine != null)
		{
			RoutineRunner.instance.StopCoroutine(timerRoutine);
		}
	}

	public void reset()
	{
		stop();
		start();
	}

	public void destroy()
	{
		if (RoutineRunner.instance != null && timerRoutine != null)
		{
			RoutineRunner.instance.StopCoroutine(timerRoutine);
		}

		stateMachine.updateState(STOPPED);
		removeTimer(this);
		isExpired = true;
	}

	private IEnumerator timerLoop()
	{
		if (stateMachine.can(STOPPED))
		{
			yield break;
		}

		yield return new WaitForSeconds(delay);

		if (callback != null)
		{
			callback();
		}

		stateMachine.updateState(STOPPED);
		
		if (repeat)
		{
			start();
		}
		else
		{
			destroy();
		}
	}

	public bool isExpired { get; private set; }

	public bool isRunning
	{
		get { return stateMachine.can(RUNNING); }
	}

	/*=========================================================================================
	STATIC METHODS
	=========================================================================================*/
	private static void removeTimer(SmartTimer timer)
	{
		if (timers.Contains(timer))
		{
			timers.Remove(timer);
		}
	}

	/// <summary>
	/// Static function for creating a smart timer. If you want to quickly create a timer to run, and aren't worried
	/// about controlling it, this static method can be used.
	/// </summary>
	public static SmartTimer create(float delay, bool repeat = false, GenericDelegate callback = null, string name = "smart timer")
	{
		SmartTimer timer = new SmartTimer(delay, repeat, callback, name);
		return timer;
	}

	/// <summary>
	/// IResetGame call
	/// </summary>
	public static void resetStaticClassData()
	{
		timers.Clear();
	}
}
