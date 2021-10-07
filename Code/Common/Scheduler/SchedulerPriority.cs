using UnityEngine;

namespace Com.Scheduler
{
	public class SchedulerPriority : IResetGame
	{
		// =============================
		// INTERNAL
		// =============================
		public int rating { get; internal set; }
		public int time { get; internal set; } // the run time when the priority was instantiated
		public static int uniqueId { get; private set; }

		// =============================
		// CONST
		// =============================
		public enum PriorityType
		{
			SINGLETON	= 1 << 0,
			LOW 		= 1 << 1,
			MEDIUM 		= 1 << 2,
			HIGH 		= 1 << 3,
			IMMEDIATE 	= 1 << 4,
			BLOCKING 	= 1 << 5,
			MAINTENANCE = 1 << 6
		}

		/*=========================================================================================
		CONSTRUCTOR
		=========================================================================================*/
		public SchedulerPriority()
		{
			rating = (int)PriorityType.LOW;
			time = uniqueId++;
		}

		/*=========================================================================================
		PRIORITY METHODS
		=========================================================================================*/
		/// <summary>
		/// Add one of the priority type enum properties to increase the rating
		/// </summary>
		/// <param name="value"></param>
		public void addToRating(PriorityType value)
		{
			rating |= (int)value;
		}

		/// <summary>
		/// Remove one of the priority type enum properties to increase the rating
		/// </summary>
		/// <param name="value"></param>
		public void removeFromRating(PriorityType value)
		{
			rating &= ~((int)value);
		}

		/// <summary>
		/// Returns true if the rating has the passed in value. E.g. rating & value = return value
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool isType(PriorityType value)
		{
			return (rating & (int)value) == (int)value;
		}

		/// Implements IResetGame
		public static void resetStaticClassData()
		{
			uniqueId = 0;
		}
	}
}