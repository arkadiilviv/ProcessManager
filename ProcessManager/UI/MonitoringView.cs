using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using ProcessManager.Core;

namespace ProcessManager.UI
{
    /// <summary>
    /// Handles real-time monitoring of managed processes.
    /// </summary>
    public class MonitoringView
    {
        private readonly ProcessPriorityManager _processManager;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isMonitoring;

        /// <summary>
        /// Initializes a new instance of the MonitoringView class.
        /// </summary>
        /// <param name="processManager">The main process manager.</param>
        public MonitoringView(ProcessPriorityManager processManager)
        {
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _cancellationTokenSource = new CancellationTokenSource();
            _isMonitoring = false;
        }

        /// <summary>
        /// Starts the real-time monitoring view.
        /// </summary>
        public void StartMonitoring()
        {
            AnsiConsole.Clear();
            
            if (_processManager.ManagedProcesses.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No managed processes found.[/]");
                AnsiConsole.MarkupLine("Add some processes first using 'Add Process from Running' or 'Add Process from File'.");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            // Reset monitoring state for new session
            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() => _isMonitoring = false);

            try
            {
                AnsiConsole.MarkupLine("[bold]Real-time Process Monitoring[/]");
                AnsiConsole.MarkupLine("Press [red]Esc[/] to stop monitoring, [green]Enter[/] to apply priorities to all running processes.");
                AnsiConsole.MarkupLine("");

                // Start monitoring loop
                Task.Run(() => MonitoringLoop(_cancellationTokenSource.Token));

                // Handle user input
                while (_isMonitoring)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            _cancellationTokenSource.Cancel();
                            break;
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            ApplyPrioritiesToAll();
                        }
                    }
                    
                    Thread.Sleep(100); // Small delay to prevent high CPU usage
                }
            }
            catch (OperationCanceledException)
            {
                // Monitoring was cancelled
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error during monitoring: {ex.Message}[/]");
                AnsiConsole.MarkupLine("Press any key to continue...");
                Console.ReadKey();
            }
            finally
            {
                // Ensure proper cleanup
                _isMonitoring = false;
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
        }

        /// <summary>
        /// The main monitoring loop that updates the display.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task MonitoringLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Update process status
                    _processManager.UpdateProcessStatus();

                    // Clear and redraw the table
                    AnsiConsole.Cursor.SetPosition(0, 3); // Position after header
                    
                    var table = CreateMonitoringTable();
                    AnsiConsole.Write(table);

                    // Wait for next update
                    await Task.Delay(2000, cancellationToken); // Update every 2 seconds
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Ignore errors during monitoring
                    await Task.Delay(1000, cancellationToken);
                }
            }

            // Clear the monitoring area
            AnsiConsole.Cursor.SetPosition(0, 3);
            AnsiConsole.Write(new Rule("[grey]Monitoring stopped[/]"));
        }

        /// <summary>
        /// Creates the monitoring table with current process information.
        /// </summary>
        /// <returns>The monitoring table.</returns>
        private Table CreateMonitoringTable()
        {
            var table = new Table()
                .Title($"Monitoring {_processManager.ManagedProcesses.Count} Process(es)")
                .AddColumn(new TableColumn("Name").Width(25))
                .AddColumn(new TableColumn("Status").Width(12))
                .AddColumn(new TableColumn("PID").Width(8))
                .AddColumn(new TableColumn("Memory").Width(12))
                .AddColumn(new TableColumn("CPU").Width(10))
                .AddColumn(new TableColumn("Current").Width(12))
                .AddColumn(new TableColumn("Preferred").Width(12))
                .Border(TableBorder.Rounded);

            var managedProcesses = _processManager.ManagedProcesses.ToList();
            
            foreach (var process in managedProcesses)
            {
                var status = process.IsRunning ? "[green]Running[/]" : "[red]Not Running[/]";
                var memory = process.MemoryUsage.HasValue 
                    ? $"{process.MemoryUsage.Value / 1024 / 1024:F1} MB" 
                    : "N/A";
                
                var cpu = process.CpuUsage.HasValue 
                    ? $"{process.CpuUsage.Value:F1}%" 
                    : "N/A";
                
                var currentPriority = process.CurrentPriority?.ToString() ?? "N/A";
                var preferredPriority = process.PreferredPriority.GetDisplayName();

                // Color code based on priority match
                var priorityMatch = process.IsRunning && 
                    process.CurrentPriority.HasValue &&
                    MapPriorityClassToLevel(process.CurrentPriority.Value) == process.PreferredPriority;

                var preferredColor = priorityMatch ? "green" : "yellow";
                var preferredDisplay = $"[{preferredColor}]{preferredPriority}[/]";

                table.AddRow(
                    process.GetDisplayName(),
                    status,
                    process.ProcessId.ToString(),
                    memory,
                    cpu,
                    currentPriority,
                    preferredDisplay);
            }

            return table;
        }

        /// <summary>
        /// Maps Windows ProcessPriorityClass to our custom PriorityLevel.
        /// </summary>
        /// <param name="priorityClass">The Windows priority class.</param>
        /// <returns>The corresponding PriorityLevel.</returns>
        private PriorityLevel MapPriorityClassToLevel(System.Diagnostics.ProcessPriorityClass priorityClass)
        {
            return priorityClass switch
            {
                System.Diagnostics.ProcessPriorityClass.Idle => PriorityLevel.Low,
                System.Diagnostics.ProcessPriorityClass.BelowNormal => PriorityLevel.Medium,
                System.Diagnostics.ProcessPriorityClass.Normal => PriorityLevel.Medium,
                System.Diagnostics.ProcessPriorityClass.AboveNormal => PriorityLevel.High,
                System.Diagnostics.ProcessPriorityClass.High => PriorityLevel.Critical,
                System.Diagnostics.ProcessPriorityClass.RealTime => PriorityLevel.Critical,
                _ => PriorityLevel.Medium
            };
        }

        /// <summary>
        /// Applies priorities to all running managed processes.
        /// </summary>
        private void ApplyPrioritiesToAll()
        {
            try
            {
                var updatedCount = _processManager.ApplyPrioritiesToAll();
                
                // Show temporary status message
                var yPos = Console.CursorTop;
                var xPos = Console.CursorLeft;
                
                AnsiConsole.MarkupLine($"[green]Applied priorities to {updatedCount} process(es).[/]");
                
                Thread.Sleep(1000); // Show message for 1 second
                
                // Clear the message
                AnsiConsole.Cursor.SetPosition(xPos, yPos);
                AnsiConsole.Write(new string(' ', 50));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error applying priorities: {ex.Message}[/]");
                Thread.Sleep(2000);
            }
        }
    }
}