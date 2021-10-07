using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MemoryHelper: MonoBehaviour
{	

#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
	public static extern uint GetMemoryResidentBytes ();
	[DllImport ("__Internal")]
	public static extern void PerformMemoryWarning();
	[DllImport("__Internal")]
	public static extern long UnityGetAvailableDiskSpace();
#elif UNITY_STANDALONE_OSX
	[DllImport ("DebugMemory")]
	public static extern uint GetMemoryResidentBytes ();
#elif UNITY_STANDALONE_WIN
	[StructLayout(LayoutKind.Sequential, Size=44)]
	private struct PROCESS_MEMORY_COUNTERS_EX
	{
	    public uint cb;
	    public uint PageFaultCount;
	    public uint PeakWorkingSetSize;
	    public uint WorkingSetSize;
	    public uint QuotaPeakPagedPoolUsage;
	    public uint QuotaPagedPoolUsage;
	    public uint QuotaPeakNonPagedPoolUsage;
	    public uint QuotaNonPagedPoolUsage;
	    public uint PagefileUsage;
	    public uint PeakPagefileUsage;
		public uint PrivateUsage;
	};

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetCurrentProcess();
	
	[DllImport("psapi.dll", SetLastError=true)]
	static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS_EX counters, uint cb);
	
	public static UInt32 GetMemoryResidentBytes() { 
		IntPtr hCurrentProcess = GetCurrentProcess();
		PROCESS_MEMORY_COUNTERS_EX pmc;
		pmc.cb = 44;
		if (GetProcessMemoryInfo(hCurrentProcess, out pmc, pmc.cb ))
		{
			return pmc.PrivateUsage;
		}
		return 0; 
	}
#else
	public static long GetMemoryResidentBytes () { return UnityEngine.Profiling.Profiler.usedHeapSizeLong;}
#endif

	private static float lazyLoadShutOffPercent = 0.5f;

#if !UNITY_IPHONE || UNITY_EDITOR
	public static void PerformMemoryWarning() 
	{
		Debug.Log("TestHelpers::PerformMemoryWarning - This OS doesn't have a memory warning equivalent! Ignoring...");
	}
#endif
	
	void Awake() 
	{
		GameObject.DontDestroyOnLoad(this.gameObject);
		lazyLoadShutOffPercent = Data.liveData.getFloat("LAZY_LOAD_MEMORY_CAP_PERCENT", 0.5f);
#if UNITY_WEBGL
		webGlUpdateRate = Data.liveData.getFloat("WEBGL_MEMORY_UPDATE_RATE", 10.0f);
#endif
	}
	
	float lastTestingTime = 0;
	bool deltaTimeStarted = false;
	
	public float TimeSinceLastCallToTime()
	{
		if (!deltaTimeStarted)
		{
			deltaTimeStarted = true;
			lastTestingTime = Time.realtimeSinceStartup;
			return 0;
		}
			
		float newTime = Time.realtimeSinceStartup;
		
		float deltaTime = newTime - lastTestingTime;
			
		lastTestingTime = newTime;
		
		return deltaTime;
	}
	
	public string testReport = "";
	
	void ResetReport()
	{
		testReport = "";
	}
	
	public void ReportAddLine(string toAdd)
	{
		testReport += toAdd + "\n";
	}
		
	public const uint MEMORY_WARNING_LEVEL = 300000000;//256000000;
	
	public struct MemoryRecord
	{
		public long memoryLevel;
		public float timeOfCheck;
		
		public MemoryRecord(long nLevel, float nTime)
		{
			memoryLevel = nLevel;
			timeOfCheck = nTime;
		}
	}

	public	List<MemoryRecord> memoryRecords = new List<MemoryRecord>();

	public void ResetMemoryRecords()
	{
		memoryRecords.Clear();
	}
	
	public long MemoryGetAndRecord(float testingCurrentTime = 0)
	{
		long currentMemory = GetMemoryResidentBytes();
			
		if(currentMemory > MEMORY_WARNING_LEVEL)
		{
			memoryRecords.Add(new MemoryRecord(currentMemory, testingCurrentTime));
		}
		
		return currentMemory;
	}
	
	public void PrintMemoryRecords()
	{
		string memoryRecordsToPrint = "";
		
		foreach(MemoryRecord entry in memoryRecords)
		{
	  	  	memoryRecordsToPrint = "ERROR: Memory - " + (entry.memoryLevel / 1000000).ToString() + " MB";
			
			if (entry.timeOfCheck != 0)
			{
				memoryRecordsToPrint +=  "  : Time - " + ((entry.timeOfCheck / 60)).ToString("F2");
			}
			
			memoryRecordsToPrint += "/n";
		}
			
	}

	public static bool inMemoryDanger()
	{
		if (AssetBundleManager.inMemoryDanger)
		{
			return true; //If we've already checked for the current memory use and saw we were close to crashing then return true.
		}
		long currentMBUsage = (MemoryHelper.GetMemoryResidentBytes()/1024)/1024;
		bool inDanger = currentMBUsage >= (SystemInfo.systemMemorySize * lazyLoadShutOffPercent); //If we're using over 1/2 the memory then we're almost certainly going to crash so trying to give us a small cushion.
		if (inDanger)
		{
			AssetBundleManager.inMemoryDanger = inDanger;
			Debug.LogError("LAZY LOAD SHUTOFF Currently using " + currentMBUsage + " MBs out of " + SystemInfo.systemMemorySize * lazyLoadShutOffPercent + " which is putting us in danger of crashing.");
		}
		return inDanger;
	}
	
#if UNITY_WEBGL
	private static float webGlUpdateRate = 10.0f; //How often, in seconds, should we grab the memory values from the emscripten variables
	private static float lastWebGlMemoryUpdateTime = 0.0f;
	private static uint lastWebGlReservedSize = 0;
	private static uint lastWebGlDynamicSize = 0;
	
	public static uint totalReservedMemorySize
	{
		get
		{
			if (Time.realtimeSinceStartup - lastWebGlMemoryUpdateTime > webGlUpdateRate)
			{
				updateWebGlMemoryRecords();
			}

			return lastWebGlReservedSize;
		}
	}

	public static uint dynamicMemorySize
	{
		get
		{
			if (Time.realtimeSinceStartup - lastWebGlMemoryUpdateTime > webGlUpdateRate)
			{
				updateWebGlMemoryRecords();
			}

			return lastWebGlDynamicSize;
		}
	}

	private static void updateWebGlMemoryRecords()
	{
		lastWebGlMemoryUpdateTime = Time.realtimeSinceStartup;
		lastWebGlReservedSize = WebGLFunctions.GetTotalMemorySize();
		lastWebGlDynamicSize = WebGLFunctions.GetDynamicMemorySize();
	}
#endif
}

