using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace ProcessManager.Services
{
    /// <summary>
    /// Service for handling administrator elevation and privilege management.
    /// </summary>
    public class ElevationService
    {
        private readonly ProcessService _processService;

        /// <summary>
        /// Initializes a new instance of the ElevationService class.
        /// </summary>
        public ElevationService()
        {
            _processService = new ProcessService();
        }

        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        public bool IsRunningAsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to restart the current application with administrator privileges.
        /// </summary>
        /// <returns>True if restart was initiated, false if already running as admin or restart failed.</returns>
        public bool RestartAsAdministrator()
        {
            if (IsRunningAsAdministrator())
                return false;

            try
            {
                var exeName = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exeName))
                    return false;

                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = exeName,
                    Verb = "runas" // Request elevation
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception)
            {
                // Elevation failed (user declined UAC prompt or other error)
                return false;
            }
        }

        /// <summary>
        /// Shows a dialog to the user requesting administrator elevation.
        /// </summary>
        /// <returns>True if elevation was successful, false otherwise.</returns>
        public bool RequestElevation()
        {
            if (IsRunningAsAdministrator())
                return true;

            Console.WriteLine("This application requires administrator privileges to modify process priorities.");
            Console.WriteLine();
            Console.Write("Would you like to restart with administrator privileges? (y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response == "y" || response == "yes")
            {
                return RestartAsAdministrator();
            }

            return false;
        }

        /// <summary>
        /// Validates that the application has the required privileges to perform process management operations.
        /// </summary>
        /// <returns>True if privileges are sufficient, false otherwise.</returns>
        public bool ValidatePrivileges()
        {
            return IsRunningAsAdministrator();
        }

        /// <summary>
        /// Gets a message describing the current privilege status.
        /// </summary>
        /// <returns>A message describing the privilege status.</returns>
        public string GetPrivilegeStatusMessage()
        {
            if (IsRunningAsAdministrator())
            {
                return "[green]✓[/] Running with administrator privileges";
            }
            else
            {
                return "[red]✗[/] Administrator privileges required - some operations will be limited";
            }
        }

        /// <summary>
        /// Checks if a specific process can be modified by the current user.
        /// </summary>
        /// <param name="processId">The process ID to check.</param>
        /// <returns>True if the process can be modified, false otherwise.</returns>
        public bool CanModifyProcess(int processId)
        {
            if (!IsRunningAsAdministrator())
                return false;

            try
            {
                var process = Process.GetProcessById(processId);
                return _processService.CanModifyProcess(process);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of processes that cannot be modified due to insufficient privileges.
        /// </summary>
        /// <param name="processIds">The list of process IDs to check.</param>
        /// <returns>A list of process IDs that cannot be modified.</returns>
        public int[] GetUnmodifiableProcesses(int[] processIds)
        {
            if (!IsRunningAsAdministrator())
                return processIds;

            var unmodifiable = new System.Collections.Generic.List<int>();

            foreach (var processId in processIds)
            {
                if (!CanModifyProcess(processId))
                {
                    unmodifiable.Add(processId);
                }
            }

            return unmodifiable.ToArray();
        }

        /// <summary>
        /// Attempts to elevate privileges and returns a success indicator.
        /// </summary>
        /// <returns>True if already elevated or successfully elevated, false otherwise.</returns>
        public bool EnsureElevated()
        {
            if (IsRunningAsAdministrator())
                return true;

            return RequestElevation();
        }
    }
}