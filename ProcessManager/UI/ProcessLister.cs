using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using ProcessManager.Core;

namespace ProcessManager.UI
{
    /// <summary>
    /// Handles displaying and managing the list of managed processes.
    /// </summary>
    public class ProcessLister
    {
        private readonly ProcessPriorityManager _processManager;

        /// <summary>
        /// Initializes a new instance of the ProcessLister class.
        /// </summary>
        /// <param name="processManager">The main process manager.</param>
        public ProcessLister(ProcessPriorityManager processManager)
        {
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        }

        /// <summary>
        /// Shows the list of managed processes with options to manage them.
        /// </summary>
        public void ShowManagedProcesses()
        {
            while (true)
            {
                AnsiConsole.Clear();
                
                // Update process status
                _processManager.UpdateProcessStatus();

                var managedProcesses = _processManager.ManagedProcesses.ToList();
                
                if (managedProcesses.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No managed processes found.[/]");
                    AnsiConsole.MarkupLine("Add some processes first using 'Add Process from Running' or 'Add Process from File'.");
                    AnsiConsole.MarkupLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                // Display processes in a table
                var table = new Table()
                    .Title($"Managed Processes ({managedProcesses.Count})")
                    .AddColumn("ID")
                    .AddColumn("Name")
                    .AddColumn("Label")
                    .AddColumn("Status")
                    .AddColumn("Current Priority")
                    .AddColumn("Preferred Priority");

                foreach (var process in managedProcesses)
                {
                    var status = process.IsRunning ? "[green]Running[/]" : "[red]Not Running[/]";
                    var currentPriority = process.CurrentPriority?.ToString() ?? "N/A";
                    var preferredPriority = process.PreferredPriority.GetDisplayName();
                    
                    table.AddRow(
                        process.ProcessId.ToString(),
                        process.Name,
                        string.IsNullOrEmpty(process.Label) ? "-" : process.Label,
                        status,
                        currentPriority,
                        preferredPriority);
                }

                AnsiConsole.Write(table);

                // Show options
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("\nSelect an option:")
                        .AddChoices(new[]
                        {
                            "Apply Priority to Selected",
                            "Update Priority for Selected",
                            "Remove Selected Process",
                            "Apply Priorities to All",
                            "Back to Process Management"
                        }));

                switch (choice)
                {
                    case "Apply Priority to Selected":
                        HandleApplyPriorityToSelected(managedProcesses);
                        break;
                    case "Update Priority for Selected":
                        HandleUpdatePriorityForSelected(managedProcesses);
                        break;
                    case "Remove Selected Process":
                        HandleRemoveSelectedProcess(managedProcesses);
                        break;
                    case "Apply Priorities to All":
                        HandleApplyPrioritiesToAll();
                        break;
                    case "Back to Process Management":
                        return;
                }
            }
        }

        /// <summary>
        /// Handles applying priority to a selected process.
        /// </summary>
        /// <param name="managedProcesses">The list of managed processes.</param>
        private void HandleApplyPriorityToSelected(List<ProcessInfo> managedProcesses)
        {
            var selectedProcess = SelectProcessFromList(managedProcesses, "Select process to apply priority:");
            if (selectedProcess == null)
                return;

            if (!selectedProcess.IsRunning)
            {
                AnsiConsole.MarkupLine($"[yellow]{selectedProcess.GetDisplayName()} is not currently running.[/]");
                AnsiConsole.MarkupLine("Start the process first, then apply the priority.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            try
            {
                var success = _processManager.ApplyPriority(selectedProcess);
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]Applied {selectedProcess.PreferredPriority.GetDisplayName()} priority to {selectedProcess.GetDisplayName()}.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to apply priority to {selectedProcess.GetDisplayName()}.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error applying priority: {ex.Message}[/]");
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles updating the priority for a selected process.
        /// </summary>
        /// <param name="managedProcesses">The list of managed processes.</param>
        private void HandleUpdatePriorityForSelected(List<ProcessInfo> managedProcesses)
        {
            var selectedProcess = SelectProcessFromList(managedProcesses, "Select process to update priority:");
            if (selectedProcess == null)
                return;

            var currentPriority = selectedProcess.PreferredPriority;
            var newPriority = AnsiConsole.Prompt(
                new SelectionPrompt<PriorityLevel>()
                    .Title($"Current priority for {selectedProcess.GetDisplayName()}: {currentPriority.GetDisplayName()}")
                    .AddChoices(Enum.GetValues<PriorityLevel>()));

            var success = _processManager.UpdateProcessPriority(selectedProcess, newPriority);
            if (success)
            {
                AnsiConsole.MarkupLine($"[green]Updated priority for {selectedProcess.GetDisplayName()} from {currentPriority.GetDisplayName()} to {newPriority.GetDisplayName()}.[/]");
                
                // Ask if user wants to apply immediately
                if (selectedProcess.IsRunning)
                {
                    var applyNow = AnsiConsole.Confirm("Apply new priority now?", true);
                    if (applyNow)
                    {
                        try
                        {
                            _processManager.ApplyPriority(selectedProcess);
                            AnsiConsole.MarkupLine($"[green]Applied {newPriority.GetDisplayName()} priority to {selectedProcess.GetDisplayName()}.[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Failed to apply priority: {ex.Message}[/]");
                        }
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to update priority for {selectedProcess.GetDisplayName()}.[/]");
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles removing a selected process from management.
        /// </summary>
        /// <param name="managedProcesses">The list of managed processes.</param>
        private void HandleRemoveSelectedProcess(List<ProcessInfo> managedProcesses)
        {
            var selectedProcess = SelectProcessFromList(managedProcesses, "Select process to remove:");
            if (selectedProcess == null)
                return;

            var confirm = AnsiConsole.Confirm($"Are you sure you want to remove {selectedProcess.GetDisplayName()} from managed processes?", false);
            
            if (confirm)
            {
                var success = _processManager.RemoveProcess(selectedProcess);
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]Removed {selectedProcess.GetDisplayName()} from managed processes.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to remove {selectedProcess.GetDisplayName()}.[/]");
                }
            }

            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Handles applying priorities to all managed processes.
        /// </summary>
        private void HandleApplyPrioritiesToAll()
        {
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
        /// Shows a selection prompt for choosing a process from the list.
        /// </summary>
        /// <param name="processes">The list of processes to choose from.</param>
        /// <param name="promptTitle">The title for the selection prompt.</param>
        /// <returns>The selected process, or null if cancelled.</returns>
        private ProcessInfo SelectProcessFromList(List<ProcessInfo> processes, string promptTitle)
        {
            var processChoices = processes
                .Select(p => new SelectionChoice<ProcessInfo>(
                    p.GetDisplayName(),
                    p,
                    $"{p.Name} ({p.ProcessId}) - {p.PreferredPriority.GetDisplayName()}"))
                .ToList();

            processChoices.Add(new SelectionChoice<ProcessInfo>("Cancel", null, "Return to previous menu"));

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<SelectionChoice<ProcessInfo>>()
                    .Title(promptTitle)
                    .AddChoices(processChoices)
                    .UseConverter(choice => choice?.Description ?? "Unknown"));

            return selected?.Value;
        }

        /// <summary>
        /// Helper class for process selection with rich display.
        /// </summary>
        private class SelectionChoice<T>
        {
            public string DisplayText { get; }
            public T Value { get; }
            public string Description { get; }

            public SelectionChoice(string displayText, T value, string description)
            {
                DisplayText = displayText;
                Value = value;
                Description = description;
            }
        }
    }
}