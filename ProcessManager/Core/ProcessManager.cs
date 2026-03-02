using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProcessManager.Services;

namespace ProcessManager.Core
{
    /// <summary>
    /// Main service for managing process priorities and monitoring.
    /// </summary>
    public class ProcessPriorityManager
    {
        private readonly ProcessService _processService;
        private readonly JsonStorageService _storageService;
        private readonly List<ProcessInfo> _managedProcesses;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the list of managed processes.
        /// </summary>
        public IReadOnlyList<ProcessInfo> ManagedProcesses => _managedProcesses;

        /// <summary>
        /// Initializes a new instance of the ProcessPriorityManager class.
        /// </summary>
        /// <param name="storageService">The storage service for persistence.</param>
        public ProcessPriorityManager(JsonStorageService storageService)
        {
            _processService = new ProcessService();
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _managedProcesses = new List<ProcessInfo>();
            
            LoadManagedProcesses();
        }

        /// <summary>
        /// Loads managed processes from persistent storage.
        /// </summary>
        private void LoadManagedProcesses()
        {
            try
            {
                var processes = _storageService.LoadProcesses();
                lock (_lock)
                {
                    _managedProcesses.Clear();
                    _managedProcesses.AddRange(processes);
                }
                
                // Update current status of loaded processes
                UpdateProcessStatus();
            }
            catch (Exception ex)
            {
                // Log error but don't fail - start with empty list
                Console.WriteLine($"Warning: Could not load saved processes: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves managed processes to persistent storage.
        /// </summary>
        private void SaveManagedProcesses()
        {
            try
            {
                lock (_lock)
                {
                    _storageService.SaveProcesses(_managedProcesses);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the operation
                Console.WriteLine($"Warning: Could not save processes: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the current status of all managed processes.
        /// </summary>
        public void UpdateProcessStatus()
        {
            lock (_lock)
            {
                foreach (var processInfo in _managedProcesses)
                {
                    try
                    {
                        var process = _processService.GetProcessById(processInfo.ProcessId);
                        if (process != null)
                        {
                            processInfo.UpdateFromProcess(process);
                        }
                        else
                        {
                            processInfo.MarkAsNotRunning();
                        }
                    }
                    catch (Exception)
                    {
                        processInfo.MarkAsNotRunning();
                    }
                }
            }
        }

        /// <summary>
        /// Gets all currently running processes.
        /// </summary>
        /// <returns>A list of running processes.</returns>
        public List<ProcessInfo> GetRunningProcesses()
        {
            return _processService.GetRunningProcesses();
        }

        /// <summary>
        /// Searches for running processes by name.
        /// </summary>
        /// <param name="searchTerm">The search term to match against process names.</param>
        /// <returns>A list of matching processes.</returns>
        public List<ProcessInfo> SearchRunningProcesses(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetRunningProcesses();

            var allProcesses = GetRunningProcesses();
            return allProcesses
                .Where(p => p.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>
        /// Adds a process to the managed list.
        /// </summary>
        /// <param name="processInfo">The process information to add.</param>
        /// <returns>True if the process was added, false if it already exists.</returns>
        public bool AddProcess(ProcessInfo processInfo)
        {
            if (processInfo == null)
                throw new ArgumentNullException(nameof(processInfo));

            lock (_lock)
            {
                // Check if process already exists
                var existing = _managedProcesses.FirstOrDefault(p => 
                    p.ProcessId == processInfo.ProcessId || 
                    string.Equals(p.ExecutablePath, processInfo.ExecutablePath, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Update existing process
                    existing.PreferredPriority = processInfo.PreferredPriority;
                    existing.Label = processInfo.Label;
                }
                else
                {
                    // Add new process
                    _managedProcesses.Add(processInfo);
                }

                SaveManagedProcesses();
                return true;
            }
        }

        /// <summary>
        /// Removes a process from the managed list.
        /// </summary>
        /// <param name="processInfo">The process to remove.</param>
        /// <returns>True if the process was removed, false if it wasn't found.</returns>
        public bool RemoveProcess(ProcessInfo processInfo)
        {
            if (processInfo == null)
                throw new ArgumentNullException(nameof(processInfo));

            lock (_lock)
            {
                var removed = _managedProcesses.Remove(processInfo);
                if (removed)
                {
                    SaveManagedProcesses();
                }
                return removed;
            }
        }

        /// <summary>
        /// Updates the preferred priority for a managed process.
        /// </summary>
        /// <param name="processInfo">The process to update.</param>
        /// <param name="newPriority">The new preferred priority.</param>
        /// <returns>True if the update was successful.</returns>
        public bool UpdateProcessPriority(ProcessInfo processInfo, PriorityLevel newPriority)
        {
            if (processInfo == null)
                throw new ArgumentNullException(nameof(processInfo));

            lock (_lock)
            {
                var existing = _managedProcesses.FirstOrDefault(p => 
                    p.ProcessId == processInfo.ProcessId || 
                    string.Equals(p.ExecutablePath, processInfo.ExecutablePath, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.PreferredPriority = newPriority;
                    SaveManagedProcesses();
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Applies the preferred priority to a running process.
        /// </summary>
        /// <param name="processInfo">The process to update.</param>
        /// <returns>True if the priority was successfully applied.</returns>
        public bool ApplyPriority(ProcessInfo processInfo)
        {
            if (processInfo == null)
                throw new ArgumentNullException(nameof(processInfo));

            if (!processInfo.IsRunning)
                return false;

            try
            {
                var process = _processService.GetProcessById(processInfo.ProcessId);
                if (process != null)
                {
                    _processService.SetProcessPriority(process, processInfo.PreferredPriority);
                    processInfo.UpdateFromProcess(process);
                    return true;
                }
            }
            catch (Exception)
            {
                // Priority change failed
            }

            return false;
        }

        /// <summary>
        /// Applies preferred priorities to all running managed processes.
        /// </summary>
        /// <returns>The number of processes that were successfully updated.</returns>
        public int ApplyPrioritiesToAll()
        {
            int updatedCount = 0;
            lock (_lock)
            {
                foreach (var processInfo in _managedProcesses)
                {
                    if (ApplyPriority(processInfo))
                    {
                        updatedCount++;
                    }
                }
            }
            return updatedCount;
        }

        /// <summary>
        /// Finds a managed process by executable path.
        /// </summary>
        /// <param name="executablePath">The executable path to search for.</param>
        /// <returns>The managed process if found, null otherwise.</returns>
        public ProcessInfo FindManagedProcessByPath(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return null;

            lock (_lock)
            {
                return _managedProcesses.FirstOrDefault(p => 
                    string.Equals(p.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets a process by its ID, either from managed processes or by looking it up.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>The process information if found, null otherwise.</returns>
        public ProcessInfo GetProcessById(int processId)
        {
            lock (_lock)
            {
                var managed = _managedProcesses.FirstOrDefault(p => p.ProcessId == processId);
                if (managed != null)
                    return managed;

                var process = _processService.GetProcessById(processId);
                return process != null ? ProcessInfo.FromProcess(process) : null;
            }
        }

        /// <summary>
        /// Starts monitoring a process by executable path.
        /// </summary>
        /// <param name="executablePath">The path to the executable.</param>
        /// <param name="preferredPriority">The preferred priority level.</param>
        /// <param name="label">Optional label for the process.</param>
        /// <returns>True if monitoring was started successfully.</returns>
        public bool StartMonitoringProcess(string executablePath, PriorityLevel preferredPriority, string label = "")
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                throw new ArgumentException("Executable path cannot be empty", nameof(executablePath));

            try
            {
                // Check if process is already running
                var runningProcess = _processService.GetProcessByPath(executablePath);
                if (runningProcess != null)
                {
                    var processInfo = ProcessInfo.FromProcess(runningProcess, preferredPriority);
                    processInfo.Label = label;
                    return AddProcess(processInfo);
                }
                else
                {
                    // Create a placeholder process info for future monitoring
                    var processInfo = new ProcessInfo
                    {
                        ProcessId = 0, // Will be updated when process starts
                        Name = Path.GetFileName(executablePath),
                        ExecutablePath = executablePath,
                        PreferredPriority = preferredPriority,
                        Label = label,
                        IsRunning = false
                    };
                    return AddProcess(processInfo);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Stops monitoring a process.
        /// </summary>
        /// <param name="processInfo">The process to stop monitoring.</param>
        /// <returns>True if monitoring was stopped.</returns>
        public bool StopMonitoringProcess(ProcessInfo processInfo)
        {
            return RemoveProcess(processInfo);
        }

        /// <summary>
        /// Clears all managed processes.
        /// </summary>
        public void ClearAllProcesses()
        {
            lock (_lock)
            {
                _managedProcesses.Clear();
                SaveManagedProcesses();
            }
        }

        /// <summary>
        /// Starts background monitoring of managed processes to maintain their priorities.
        /// </summary>
        /// <returns>A task representing the monitoring operation.</returns>
        public async Task StartBackgroundMonitoringAsync()
        {
            while (true)
            {
                try
                {
                    // Update process status
                    UpdateProcessStatus();

                    // Check and restore priorities for running processes
                    lock (_lock)
                    {
                        foreach (var processInfo in _managedProcesses)
                        {
                            if (processInfo.IsRunning)
                            {
                                try
                                {
                                    var process = _processService.GetProcessById(processInfo.ProcessId);
                                    if (process != null)
                                    {
                                        // Check if priority needs to be restored
                                        var currentPriority = _processService.GetCurrentPriority(process);
                                        var expectedPriority = processInfo.PreferredPriority;

                                        if (currentPriority != expectedPriority)
                                        {
                                            // Priority was changed, restore it
                                            _processService.SetProcessPriority(process, processInfo.PreferredPriority);
                                            processInfo.UpdateFromProcess(process);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    // Process might have terminated, will be marked as not running in next update
                                }
                            }
                        }
                    }

                    // Wait for 1 minute before next check
                    await Task.Delay(60000);
                }
                catch (Exception)
                {
                    // Log error but continue monitoring
                    await Task.Delay(5000); // Wait 5 seconds before retrying
                }
            }
        }

        /// <summary>
        /// Stops background monitoring.
        /// </summary>
        public void StopBackgroundMonitoring()
        {
            // In a real implementation, you would use a CancellationToken to stop the monitoring loop
            // For now, this is a placeholder that could be expanded with proper cancellation support
        }
    }
}
