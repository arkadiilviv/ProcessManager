using System;
using System.Diagnostics;

namespace ProcessManager.Core
{
    /// <summary>
    /// Represents information about a process that can be managed by the application.
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier of the process.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the name of the process (executable name).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full path to the executable file.
        /// </summary>
        public string ExecutablePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the preferred priority level for this process.
        /// </summary>
        public PriorityLevel PreferredPriority { get; set; }

        /// <summary>
        /// Gets or sets a custom label for this process for easier identification.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current priority class of the running process.
        /// This is only valid when the process is currently running.
        /// </summary>
        public ProcessPriorityClass? CurrentPriority { get; set; }

        /// <summary>
        /// Gets or sets whether the process is currently running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the memory usage of the running process in bytes.
        /// This is only valid when the process is currently running.
        /// </summary>
        public long? MemoryUsage { get; set; }

        /// <summary>
        /// Gets or sets the CPU usage percentage of the running process.
        /// This is only valid when the process is currently running.
        /// </summary>
        public double? CpuUsage { get; set; }

        /// <summary>
        /// Gets or sets the time when the process was started.
        /// This is only valid when the process is currently running.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the ProcessInfo class.
        /// </summary>
        public ProcessInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ProcessInfo class with basic information.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="name">The process name.</param>
        /// <param name="executablePath">The executable path.</param>
        /// <param name="preferredPriority">The preferred priority level.</param>
        public ProcessInfo(int processId, string name, string executablePath, PriorityLevel preferredPriority)
        {
            ProcessId = processId;
            Name = name;
            ExecutablePath = executablePath;
            PreferredPriority = preferredPriority;
        }

        /// <summary>
        /// Creates a ProcessInfo instance from a running System.Diagnostics.Process.
        /// </summary>
        /// <param name="process">The running process.</param>
        /// <param name="preferredPriority">The preferred priority level (defaults to Normal).</param>
        /// <returns>A new ProcessInfo instance.</returns>
        public static ProcessInfo FromProcess(Process process, PriorityLevel preferredPriority = PriorityLevel.Medium)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            var info = new ProcessInfo
            {
                ProcessId = process.Id,
                Name = process.ProcessName,
                PreferredPriority = preferredPriority,
                IsRunning = true
            };

            try
            {
                info.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
                info.CurrentPriority = process.PriorityClass;
                info.StartTime = process.StartTime;
                
                // Get memory usage
                var workingSet = process.WorkingSet64;
                if (workingSet > 0)
                    info.MemoryUsage = workingSet;
            }
            catch (Exception)
            {
                // If we can't get some information, that's okay - we'll have basic info
                info.ExecutablePath = string.Empty;
            }

            return info;
        }

        /// <summary>
        /// Updates the current process information from a running process.
        /// </summary>
        /// <param name="process">The running process.</param>
        public void UpdateFromProcess(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            if (process.Id != ProcessId)
                throw new ArgumentException("Process ID mismatch", nameof(process));

            IsRunning = true;
            CurrentPriority = process.PriorityClass;

            try
            {
                MemoryUsage = process.WorkingSet64;
                StartTime = process.StartTime;
            }
            catch (Exception)
            {
                // If we can't update some information, that's okay
            }
        }

        /// <summary>
        /// Marks the process as not running and clears runtime information.
        /// </summary>
        public void MarkAsNotRunning()
        {
            IsRunning = false;
            CurrentPriority = null;
            MemoryUsage = null;
            CpuUsage = null;
            StartTime = null;
        }

        /// <summary>
        /// Gets a display name for the process, using the label if available, otherwise the name.
        /// </summary>
        /// <returns>The display name.</returns>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(Label) ? Name : $"{Label} ({Name})";
        }

        /// <summary>
        /// Determines whether this process info is equal to another based on process ID and executable path.
        /// </summary>
        /// <param name="other">The other ProcessInfo to compare with.</param>
        /// <returns>True if the processes are considered equal.</returns>
        public bool Equals(ProcessInfo other)
        {
            if (other == null) return false;
            return ProcessId == other.ProcessId && 
                   string.Equals(ExecutablePath, other.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string representation of the process info.</returns>
        public override string ToString()
        {
            var displayName = GetDisplayName();
            var status = IsRunning ? "Running" : "Not Running";
            var priority = PreferredPriority.GetDisplayName();
            
            return $"{displayName} - {status} - Preferred: {priority}";
        }
    }
}