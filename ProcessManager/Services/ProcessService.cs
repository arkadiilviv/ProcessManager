using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProcessManager.Core;

namespace ProcessManager.Services
{
    /// <summary>
    /// Service for managing Windows processes and their priorities.
    /// </summary>
    public class ProcessService
    {
        /// <summary>
        /// Gets all currently running processes.
        /// </summary>
        /// <returns>A list of running processes.</returns>
        public List<ProcessInfo> GetRunningProcesses()
        {
            try
            {
                var processes = Process.GetProcesses();
                var processInfos = new List<ProcessInfo>();

                foreach (var process in processes)
                {
                    try
                    {
                        // Skip processes we can't access
                        if (string.IsNullOrEmpty(process.ProcessName))
                            continue;

                        var processInfo = ProcessInfo.FromProcess(process);
                        processInfos.Add(processInfo);
                    }
                    catch (Exception)
                    {
                        // Skip processes we can't access details for
                        continue;
                    }
                }

                return processInfos.OrderBy(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve running processes", ex);
            }
        }

        /// <summary>
        /// Gets a process by its ID.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>The process if found, null otherwise.</returns>
        public Process GetProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                // Process not found
                return null;
            }
            catch (Exception)
            {
                // Other error (access denied, etc.)
                return null;
            }
        }

        /// <summary>
        /// Gets a process by its executable path.
        /// </summary>
        /// <param name="executablePath">The executable path.</param>
        /// <returns>The process if found, null otherwise.</returns>
        public Process GetProcessByPath(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return null;

            try
            {
                var processes = Process.GetProcesses();
                
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainModule?.FileName != null &&
                            string.Equals(process.MainModule.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return process;
                        }
                    }
                    catch (Exception)
                    {
                        // Access denied to this process's main module
                        continue;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the priority of a process.
        /// </summary>
        /// <param name="process">The process to modify.</param>
        /// <param name="priorityLevel">The new priority level.</param>
        /// <exception cref="InvalidOperationException">Thrown when the process cannot be modified.</exception>
        public void SetProcessPriority(Process process, PriorityLevel priorityLevel)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                var priorityClass = priorityLevel.ToProcessPriorityClass();
                process.PriorityClass = priorityClass;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Cannot modify process priority. " +
                    $"Process: {process.ProcessName} (ID: {process.Id}). " +
                    $"Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the current priority level of a process.
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>The current priority level.</returns>
        public PriorityLevel GetCurrentPriority(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                return MapProcessPriorityClassToLevel(process.PriorityClass);
            }
            catch (Exception)
            {
                return PriorityLevel.Medium; // Default fallback
            }
        }

        /// <summary>
        /// Maps a Windows ProcessPriorityClass to our custom PriorityLevel.
        /// </summary>
        /// <param name="priorityClass">The Windows priority class.</param>
        /// <returns>The corresponding PriorityLevel.</returns>
        private PriorityLevel MapProcessPriorityClassToLevel(ProcessPriorityClass priorityClass)
        {
            return priorityClass switch
            {
                ProcessPriorityClass.Idle => PriorityLevel.Low,
                ProcessPriorityClass.BelowNormal => PriorityLevel.Medium,
                ProcessPriorityClass.Normal => PriorityLevel.Medium,
                ProcessPriorityClass.AboveNormal => PriorityLevel.High,
                ProcessPriorityClass.High => PriorityLevel.Critical,
                ProcessPriorityClass.RealTime => PriorityLevel.Critical, // Map to Critical since we don't have RealTime
                _ => PriorityLevel.Medium
            };
        }

        /// <summary>
        /// Checks if the current process has administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        public bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a process can be modified (has required privileges).
        /// </summary>
        /// <param name="process">The process to validate.</param>
        /// <returns>True if the process can be modified, false otherwise.</returns>
        public bool CanModifyProcess(Process process)
        {
            if (process == null)
                return false;

            if (!IsRunningAsAdministrator())
                return false;

            try
            {
                // Try to access a property that requires elevated privileges
                var _ = process.PriorityClass;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets memory usage information for a process.
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>Memory usage in bytes, or null if not available.</returns>
        public long? GetMemoryUsage(Process process)
        {
            if (process == null)
                return null;

            try
            {
                return process.WorkingSet64;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the start time of a process.
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>The start time, or null if not available.</returns>
        public DateTime? GetStartTime(Process process)
        {
            if (process == null)
                return null;

            try
            {
                return process.StartTime;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the executable path of a process.
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>The executable path, or null if not available.</returns>
        public string GetExecutablePath(Process process)
        {
            if (process == null)
                return null;

            try
            {
                return process.MainModule?.FileName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Kills a process.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        /// <returns>True if the process was killed successfully.</returns>
        public bool KillProcess(Process process)
        {
            if (process == null)
                return false;

            try
            {
                if (!CanModifyProcess(process))
                    return false;

                process.Kill();
                process.WaitForExit(5000); // Wait up to 5 seconds
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a process is still running.
        /// </summary>
        /// <param name="process">The process to check.</param>
        /// <returns>True if the process is running, false otherwise.</returns>
        public bool IsProcessRunning(Process process)
        {
            if (process == null)
                return false;

            try
            {
                return !process.HasExited;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}