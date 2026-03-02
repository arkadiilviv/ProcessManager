using System;

namespace ProcessManager.Core
{
    /// <summary>
    /// Custom simplified priority levels that map to Windows process priorities.
    /// </summary>
    public enum PriorityLevel
    {
        /// <summary>
        /// Maps to Windows Idle priority. Lowest priority, runs only when system is idle.
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// Maps to Windows Below Normal priority. Lower than normal priority.
        /// </summary>
        Medium = 1,
        
        /// <summary>
        /// Maps to Windows Above Normal priority. Higher than normal priority.
        /// </summary>
        High = 2,
        
        /// <summary>
        /// Maps to Windows High priority. Very high priority, can impact system performance.
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Extension methods for PriorityLevel enum.
    /// </summary>
    public static class PriorityLevelExtensions
    {
        /// <summary>
        /// Maps the custom priority level to the corresponding Windows ProcessPriorityClass.
        /// </summary>
        /// <param name="priority">The custom priority level.</param>
        /// <returns>The corresponding Windows ProcessPriorityClass.</returns>
        public static System.Diagnostics.ProcessPriorityClass ToProcessPriorityClass(this PriorityLevel priority)
        {
            return priority switch
            {
                PriorityLevel.Low => System.Diagnostics.ProcessPriorityClass.Idle,
                PriorityLevel.Medium => System.Diagnostics.ProcessPriorityClass.BelowNormal,
                PriorityLevel.High => System.Diagnostics.ProcessPriorityClass.AboveNormal,
                PriorityLevel.Critical => System.Diagnostics.ProcessPriorityClass.High,
                _ => System.Diagnostics.ProcessPriorityClass.Normal
            };
        }

        /// <summary>
        /// Gets a human-readable description of the priority level.
        /// </summary>
        /// <param name="priority">The priority level.</param>
        /// <returns>A description of the priority level.</returns>
        public static string GetDescription(this PriorityLevel priority)
        {
            return priority switch
            {
                PriorityLevel.Low => "Low (Idle) - Runs only when system is idle",
                PriorityLevel.Medium => "Medium (Below Normal) - Lower than normal priority",
                PriorityLevel.High => "High (Above Normal) - Higher than normal priority",
                PriorityLevel.Critical => "Critical (High) - Very high priority, may impact system performance",
                _ => "Unknown priority level"
            };
        }

        /// <summary>
        /// Gets a short display name for the priority level.
        /// </summary>
        /// <param name="priority">The priority level.</param>
        /// <returns>A short display name.</returns>
        public static string GetDisplayName(this PriorityLevel priority)
        {
            return priority switch
            {
                PriorityLevel.Low => "Low",
                PriorityLevel.Medium => "Medium",
                PriorityLevel.High => "High",
                PriorityLevel.Critical => "Critical",
                _ => "Unknown"
            };
        }
    }
}