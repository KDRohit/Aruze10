using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class SegmentBenchmarker
{
	private static Dictionary<string, BenchData> benchmarks = new Dictionary<string, BenchData>();
	private static List<BenchData> completedBenchmarks = new List<BenchData>();

	// =============================
	// CONST
	// =============================
	public const double NANO_CONVERSION = 1000000000;
	public const double NANO_TO_MS_CONVERSION = 1000000;

	/// <summary>
	/// Starts benchmark
	/// </summary>
	/// <param name="testname">name of test</param>
	public static void startBenchmark(string testname)
	{
#if !ZYNGA_PRODUCTION
		if (benchmarks.ContainsKey(testname))
		{
			int index = 1;
			while (benchmarks.ContainsKey(testname + index))
			{
				index++;
			}

			Debug.LogWarningFormat("Benchmark already exists with name {0}, updated to {1}", testname, testname + index);

			testname = testname + index;
		}

		BenchData data = new BenchData(testname);
		benchmarks.Add(testname, data);
		data.start();
#endif
	}

	/// <summary>
	/// Stops benchmark
	/// </summary>
	/// <param name="testname">name of test</param>
	public static void endBenchmark(string testname)
	{
#if !ZYNGA_PRODUCTION
		Stopwatch offset = new Stopwatch();
		offset.Start();

		if (benchmarks.ContainsKey(testname))
		{
			BenchData data = benchmarks[testname];
			data.end(offset);
			offset.Stop();
			completedBenchmarks.Add(data);
			Debug.Log("Completed Bench: " + data.getReport());
		}
#endif
	}

	/*=========================================================================================
	DATA STRUCTURES
	=========================================================================================*/
	private class BenchData
	{
		// =============================
		// PRIVATE
		// =============================
		private Stopwatch stopwatch;
		private double seconds;
		private double ms;
		private double nano;

		// =============================
		// PUBLIC
		// =============================
		public string name;

		public BenchData(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// Starts benchmarking
		/// </summary>
		public void start()
		{
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		/// <summary>
		/// Ends benchmark
		/// </summary>
		/// <param name="offset">Pass in a stopwatch to offset time, this will make the calculations more accurate
		/// if any overhead was involved before end()</param>
		public void end(Stopwatch offset)
		{
			stopwatch.Stop();
			long ticks  = stopwatch.ElapsedTicks - offset.ElapsedTicks;
			seconds     = ticks / (float)Stopwatch.Frequency;
			ms          = stopwatch.ElapsedMilliseconds - offset.ElapsedMilliseconds;
			nano        = ticks / (float)Stopwatch.Frequency * NANO_CONVERSION;
		}

		/// <summary>
		/// Returns the summary in a nicely formatted string
		/// </summary>
		/// <returns></returns>
		public string getReport()
		{
			return string.Format("{0} complete | Time: {1}", name, getFormattedTime());
		}

		/// <summary>
		/// Formatted time
		/// </summary>
		/// <returns></returns>
		public string getFormattedTime()
		{
			if (seconds > 0 || ms > 0 || nano > 0)
			{
				if (seconds >= 1)
				{
					return string.Format("{0}s", seconds);
				}
				if (ms > 0)
				{
					return string.Format("{0}ms", ms);
				}

				return string.Format("{0}ms", nano / NANO_TO_MS_CONVERSION);

			}
			return "N/A";
		}
	}
}