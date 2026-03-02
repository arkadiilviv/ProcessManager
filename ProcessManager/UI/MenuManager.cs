using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using ProcessManager.Core;
using ProcessManager.Services;

namespace ProcessManager.UI
{
    /// <summary>
    /// Manages the main menu navigation and user interface flow.
    /// </summary>
    public class MenuManager
    {
        private readonly ProcessPriorityManager _processManager;
        private readonly ProcessService _processService;
        private readonly JsonStorageService _storageService;
        private readonly ElevationService _elevationService;
        private readonly ProcessSelector _processSelector;
        private readonly ProcessLister _processLister;
        private readonly MonitoringView _monitoringView;

        /// <summary>
        /// Initializes a new instance of the MenuManager class.
        /// </summary>
        /// <param name="processManager">The main process manager service.</param>
        /// <param name="processService">The process service.</param>
        /// <param name="storageService">The JSON storage service.</param>
        /// <param name="elevationService">The elevation service.</param>
        public MenuManager(
            ProcessPriorityManager processManager,
            ProcessService processService,
            JsonStorageService storageService,
            ElevationService elevationService)
        {
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _elevationService = elevationService ?? throw new ArgumentNullException(nameof(elevationService));
            
            _processSelector = new ProcessSelector(_processService, _processManager);
            _processLister = new ProcessLister(_processManager);
            _monitoringView = new MonitoringView(_processManager);
        }

        /// <summary>
        /// Starts the main application loop.
        /// </summary>
        public void Start()
        {
            ShowWelcomeScreen();
            
            while (true)
            {
                var choice = ShowMainMenu();
                
                switch (choice)
                {
                    case "Manage Processes":
                        HandleManageProcesses();
                        break;
                    case "Monitor Processes":
                        HandleMonitorProcesses();
                        break;
                    case "Storage Management":
                        HandleStorageManagement();
                        break;
                    case "About":
                        ShowAbout();
                        break;
                    case "Exit":
                        return;
                }
            }
        }

        /// <summary>
        /// Shows the welcome screen with application information.
        /// </summary>
        private void ShowWelcomeScreen()
        {
            AnsiConsole.Clear();
            
            var welcomePanel = new Panel(
                "[bold blue]Process Priority Manager[/]\n" +
                "A rich console application for managing process CPU priorities\n\n" +
                $"{_elevationService.GetPrivilegeStatusMessage()}\n" +
                $"Managed processes: {_processManager.ManagedProcesses.Count}")
            {
                Header = new PanelHeader("Welcome", Justify.Center),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1, 2, 1)
            };

            AnsiConsole.Write(welcomePanel);
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Shows the main menu and returns the user's choice.
        /// </summary>
        /// <returns>The selected menu option.</returns>
        private string ShowMainMenu()
        {
            AnsiConsole.Clear();
            
            // Update process status
            _processManager.UpdateProcessStatus();

            var menu = new SelectionPrompt<string>()
                .Title("[bold]Main Menu[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "Manage Processes",
                    "Monitor Processes", 
                    "Storage Management",
                    "About",
                    "Exit"
                });

            return AnsiConsole.Prompt(menu);
        }

        /// <summary>
        /// Handles the process management menu.
        /// </summary>
        private void HandleManageProcesses()
        {
            while (true)
            {
                var choice = ShowManageProcessesMenu();
                
                switch (choice)
                {
                    case "Add Process from Running":
                        _processSelector.AddProcessFromRunning();
                        break;
                    case "Add Process from File":
                        _processSelector.AddProcessFromFile();
                        break;
                    case "View Managed Processes":
                        _processLister.ShowManagedProcesses();
                        break;
                    case "Apply Priorities to All":
                        HandleApplyPrioritiesToAll();
                        break;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        /// <summary>
        /// Shows the process management menu.
        /// </summary>
        /// <returns>The selected menu option.</returns>
        private string ShowManageProcessesMenu()
        {
            AnsiConsole.Clear();
            
            var menu = new SelectionPrompt<string>()
                .Title("[bold]Process Management[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "Add Process from Running",
                    "Add Process from File",
                    "View Managed Processes",
                    "Apply Priorities to All",
                    "Back to Main Menu"
                });

            return AnsiConsole.Prompt(menu);
        }

        /// <summary>
        /// Handles applying priorities to all managed processes.
        /// </summary>
        private void HandleApplyPrioritiesToAll()
        {
            if (_processManager.ManagedProcesses.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No managed processes found.[/]");
                AnsiConsole.MarkupLine("Add some processes first using 'Add Process from Running' or 'Add Process from File'.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var runningProcesses = _processManager.ManagedProcesses.Where(p => p.IsRunning).ToList();
            
            if (runningProcesses.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No managed processes are currently running.[/]");
                AnsiConsole.MarkupLine("Start some processes or wait for them to start.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var updatedCount = _processManager.ApplyPrioritiesToAll();
            
            AnsiConsole.MarkupLine($"[green]Successfully updated {updatedCount} process(es).[/]");
            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles the process monitoring view.
        /// </summary>
        private void HandleMonitorProcesses()
        {
            _monitoringView.StartMonitoring();
        }

        /// <summary>
        /// Handles the storage management menu.
        /// </summary>
        private void HandleStorageManagement()
        {
            while (true)
            {
                var choice = ShowStorageManagementMenu();
                
                switch (choice)
                {
                    case "View Storage Info":
                        ShowStorageInfo();
                        break;
                    case "Backup Storage":
                        HandleBackupStorage();
                        break;
                    case "Restore from Backup":
                        HandleRestoreFromBackup();
                        break;
                    case "Clear All Processes":
                        HandleClearAllProcesses();
                        break;
                    case "Back to Main Menu":
                        return;
                }
            }
        }

        /// <summary>
        /// Shows the storage management menu.
        /// </summary>
        /// <returns>The selected menu option.</returns>
        private string ShowStorageManagementMenu()
        {
            AnsiConsole.Clear();
            
            var menu = new SelectionPrompt<string>()
                .Title("[bold]Storage Management[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "View Storage Info",
                    "Backup Storage",
                    "Restore from Backup",
                    "Clear All Processes",
                    "Back to Main Menu"
                });

            return AnsiConsole.Prompt(menu);
        }

        /// <summary>
        /// Shows storage information.
        /// </summary>
        private void ShowStorageInfo()
        {
            var storageInfo = _storageService.GetStorageInfo();
            
            var table = new Table()
                .Title("Storage Information")
                .AddColumn("Property")
                .AddColumn("Value");

            table.AddRow("File Exists", storageInfo.Exists ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("File Path", storageInfo.FilePath);
            table.AddRow("File Size", storageInfo.FileSize > 0 ? $"{storageInfo.FileSize} bytes" : "0 bytes");
            table.AddRow("Last Modified", storageInfo.LastModified?.ToString() ?? "N/A");
            table.AddRow("Process Count", storageInfo.ProcessCount.ToString());

            AnsiConsole.Clear();
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles backing up the storage file.
        /// </summary>
        private void HandleBackupStorage()
        {
            try
            {
                var success = _storageService.BackupStorage();
                if (success)
                {
                    AnsiConsole.MarkupLine("[green]Backup created successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Backup failed.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Backup failed: {ex.Message}[/]");
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles restoring from a backup file.
        /// </summary>
        private void HandleRestoreFromBackup()
        {
            var backupFiles = _storageService.GetBackupFiles();
            
            if (backupFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No backup files found.[/]");
                AnsiConsole.MarkupLine("Create a backup first using 'Backup Storage'.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var selectedBackup = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select backup to restore:")
                    .AddChoices(backupFiles));

            var confirm = AnsiConsole.Confirm($"Are you sure you want to restore from {selectedBackup}?", false);
            
            if (confirm)
            {
                try
                {
                    _storageService.RestoreFromBackup(selectedBackup);
                    AnsiConsole.MarkupLine("[green]Restore completed successfully.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Restore failed: {ex.Message}[/]");
                }
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles clearing all managed processes.
        /// </summary>
        private void HandleClearAllProcesses()
        {
            var confirm = AnsiConsole.Confirm(
                "Are you sure you want to clear all managed processes? This cannot be undone.", 
                false);

            if (confirm)
            {
                _processManager.ClearAllProcesses();
                AnsiConsole.MarkupLine("[green]All managed processes cleared.[/]");
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Shows the about screen with application information.
        /// </summary>
        private void ShowAbout()
        {
            AnsiConsole.Clear();
            
            var aboutText = new Panel(
                "[bold]Process Priority Manager[/]\n\n" +
                "Version: 1.0.0\n" +
                "Framework: .NET 10\n" +
                "UI Library: Spectre.Console\n\n" +
                "Features:\n" +
                "• Manage process CPU priorities\n" +
                "• Two process selection methods\n" +
                "• Real-time monitoring\n" +
                "• Persistent storage\n" +
                "• Administrator privilege handling\n\n" +
                "Developed with context7 documentation")
            {
                Header = new PanelHeader("About", Justify.Center),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1, 2, 1)
            };

            AnsiConsole.Write(aboutText);
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}