using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using ProcessManager.Core;
using ProcessManager.Services;
using ProcessManager.UI;

namespace ProcessManager
{
    /// <summary>
    /// Main program entry point for the Process Priority Manager application.
    /// </summary>
    public class Program
    {
        private static ProcessPriorityManager _processManager;
        private static MenuManager _menuManager;
        private static ElevationService _elevationService;

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static async Task Main(string[] args)
        {
            try
            {
                AnsiConsole.Write(
                    new FigletText("Process Priority Manager")
                        .Color(Color.Blue));

                // Initialize services
                InitializeServices();

                // Check for elevation
                if (!_elevationService.IsRunningAsAdministrator())
                {
                    var elevated = _elevationService.RequestElevation();
                    if (!elevated)
                    {
                        AnsiConsole.MarkupLine("[red]Administrator privileges are required to modify process priorities.[/]");
                        AnsiConsole.MarkupLine("Please restart the application with administrator privileges.");
                        AnsiConsole.MarkupLine("Press any key to exit...");
                        Console.ReadKey();
                        return;
                    }
                    // If elevation was successful, the application would have restarted
                    return;
                }

                // Start background monitoring
                var monitoringTask = _processManager.StartBackgroundMonitoringAsync();

                // Start the main application
                _menuManager.Start();
                
                // Wait for monitoring to complete (it runs indefinitely)
                await monitoringTask;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An unexpected error occurred: {ex.Message}[/]");
                AnsiConsole.MarkupLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Initializes all required services for the application.
        /// </summary>
        private static void InitializeServices()
        {
            try
            {
                // Initialize services
                _elevationService = new ElevationService();
                var storageService = new JsonStorageService();
                var processService = new ProcessService();

                // Initialize the main process manager
                _processManager = new ProcessPriorityManager(storageService);

                // Initialize the UI manager
                _menuManager = new MenuManager(
                    _processManager,
                    processService,
                    storageService,
                    _elevationService);

                AnsiConsole.MarkupLine("[green]✓[/] Services initialized successfully");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to initialize services: {ex.Message}[/]");
                throw;
            }
        }
    }
}
