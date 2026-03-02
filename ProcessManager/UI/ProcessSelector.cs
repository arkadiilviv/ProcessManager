using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;
using ProcessManager.Core;
using ProcessManager.Services;

namespace ProcessManager.UI
{
    /// <summary>
    /// Handles process selection through different methods.
    /// </summary>
    public class ProcessSelector
    {
        private readonly ProcessService _processService;
        private readonly ProcessPriorityManager _processManager;

        /// <summary>
        /// Initializes a new instance of the ProcessSelector class.
        /// </summary>
        /// <param name="processService">The process service.</param>
        /// <param name="processManager">The main process manager.</param>
        public ProcessSelector(ProcessService processService, ProcessPriorityManager processManager)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        }

        /// <summary>
        /// Adds a process from the list of running processes.
        /// </summary>
        public void AddProcessFromRunning()
        {
            AnsiConsole.Clear();
            
            // Check if we have admin privileges
            if (!_processService.IsRunningAsAdministrator())
            {
                AnsiConsole.MarkupLine("[red]Administrator privileges required to modify process priorities.[/]");
                AnsiConsole.MarkupLine("Please restart the application with administrator privileges.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var runningProcesses = _processService.GetRunningProcesses();
            
            if (runningProcesses.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No running processes found.[/]");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Show search prompt
            var searchTerm = AnsiConsole.Prompt(
                new TextPrompt<string>("Search processes (leave empty to show all):")
                    .AllowEmpty());

            var filteredProcesses = string.IsNullOrWhiteSpace(searchTerm) 
                ? runningProcesses 
                : _processManager.SearchRunningProcesses(searchTerm);

            if (filteredProcesses.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No processes found matching '{searchTerm}'.[/]");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Display processes in a table
            var table = new Table()
                .Title($"Found {filteredProcesses.Count} process(es)")
                .AddColumn("ID")
                .AddColumn("Name")
                .AddColumn("Executable Path")
                .AddColumn("Memory")
                .AddColumn("Priority");

            foreach (var process in filteredProcesses.Take(20)) // Limit to first 20
            {
                var memory = process.MemoryUsage.HasValue 
                    ? $"{process.MemoryUsage.Value / 1024 / 1024:F1} MB" 
                    : "N/A";
                
                var priority = process.CurrentPriority?.ToString() ?? "N/A";
                
                table.AddRow(
                    process.ProcessId.ToString(),
                    process.Name,
                    process.ExecutablePath,
                    memory,
                    priority);
            }

            if (filteredProcesses.Count > 20)
            {
                table.AddRow("...", "...", "...", "...", "...");
            }

            AnsiConsole.Clear();
            AnsiConsole.Write(table);

            // Select process
            var selectedId = AnsiConsole.Prompt(
                new TextPrompt<int>("Enter Process ID to add (0 to cancel):")
                    .ValidationErrorMessage("[red]Invalid Process ID[/]")
                    .Validate(id => id >= 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Process ID must be 0 or greater[/]")));

            if (selectedId == 0)
                return;

            var selectedProcess = filteredProcesses.FirstOrDefault(p => p.ProcessId == selectedId);
            if (selectedProcess == null)
            {
                AnsiConsole.MarkupLine("[red]Process not found.[/]");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Set priority and label
            var selectedPriority = AnsiConsole.Prompt(
                new SelectionPrompt<PriorityLevel>()
                    .Title("Select priority level:")
                    .AddChoices(Enum.GetValues<PriorityLevel>()));

            var label = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter a label for this process (optional):")
                    .AllowEmpty());

            // Add to managed processes
            selectedProcess.PreferredPriority = selectedPriority;
            selectedProcess.Label = label;

            var success = _processManager.AddProcess(selectedProcess);
            
            if (success)
            {
                AnsiConsole.MarkupLine($"[green]Added {selectedProcess.GetDisplayName()} to managed processes.[/]");
                
                // Ask if user wants to apply priority immediately
                var applyNow = AnsiConsole.Confirm("Apply priority now?", true);
                if (applyNow)
                {
                    try
                    {
                        _processManager.ApplyPriority(selectedProcess);
                        AnsiConsole.MarkupLine($"[green]Applied {selectedPriority.GetDisplayName()} priority to {selectedProcess.GetDisplayName()}.[/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to apply priority: {ex.Message}[/]");
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Process {selectedProcess.GetDisplayName()} is already managed.[/]");
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Adds a process by selecting an executable file.
        /// </summary>
        public void AddProcessFromFile()
        {
            AnsiConsole.Clear();
            
            // Check if we have admin privileges
            if (!_processService.IsRunningAsAdministrator())
            {
                AnsiConsole.MarkupLine("[red]Administrator privileges required to modify process priorities.[/]");
                AnsiConsole.MarkupLine("Please restart the application with administrator privileges.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Get executable path
            var executablePath = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter the path to the executable file:")
                    .ValidationErrorMessage("[red]Invalid file path[/]")
                    .Validate(path => 
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return ValidationResult.Error("[red]File path cannot be empty[/]");
                        
                        if (!File.Exists(path))
                            return ValidationResult.Error("[red]File does not exist[/]");
                        
                        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            return ValidationResult.Error("[red]File must be an executable (.exe)[/]");
                        
                        return ValidationResult.Success();
                    }));

            // Check if already managed
            var existingProcess = _processManager.FindManagedProcessByPath(executablePath);
            if (existingProcess != null)
            {
                AnsiConsole.MarkupLine($"[yellow]Process {existingProcess.GetDisplayName()} is already managed.[/]");
                
                var updatePriority = AnsiConsole.Confirm("Update priority level?", false);
                if (updatePriority)
                {
                    var newPriority = AnsiConsole.Prompt(
                        new SelectionPrompt<PriorityLevel>()
                            .Title("Select new priority level:")
                            .AddChoices(Enum.GetValues<PriorityLevel>()));

                    var updateSuccess = _processManager.UpdateProcessPriority(existingProcess, newPriority);
                    if (updateSuccess)
                    {
                        AnsiConsole.MarkupLine($"[green]Updated priority for {existingProcess.GetDisplayName()} to {newPriority.GetDisplayName()}.[/]");
                    }
                }

                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Set priority and label
            var priority = AnsiConsole.Prompt(
                new SelectionPrompt<PriorityLevel>()
                    .Title("Select priority level:")
                    .AddChoices(Enum.GetValues<PriorityLevel>()));

            var label = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter a label for this process (optional):")
                    .AllowEmpty());

            // Create process info
            var processInfo = new ProcessInfo
            {
                ProcessId = 0, // Will be updated when process starts
                Name = Path.GetFileName(executablePath),
                ExecutablePath = executablePath,
                PreferredPriority = priority,
                Label = label,
                IsRunning = false
            };

            // Add to managed processes
            var success = _processManager.AddProcess(processInfo);
            
            if (success)
            {
                AnsiConsole.MarkupLine($"[green]Added {processInfo.GetDisplayName()} to managed processes.[/]");
                AnsiConsole.MarkupLine("This process will be monitored when it starts running.");
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}