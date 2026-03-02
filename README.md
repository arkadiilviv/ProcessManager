# Process Priority Manager

A rich console application built with .NET 10 and Spectre.Console for managing process CPU priorities.
[AI STUDIES]

## Features

- **Two Process Selection Methods**:
  - Search and select from running processes
  - Browse and select .exe files for future monitoring

- **Custom Priority Levels**:
  - Low (maps to Windows Idle)
  - Medium (maps to Windows Below Normal)
  - High (maps to Windows Above Normal)
  - Critical (maps to Windows High)

- **Real-time Monitoring**:
  - Live dashboard showing managed processes
  - Automatic status updates every 2 seconds
  - Visual indicators for priority matches

- **Persistent Storage**:
  - JSON file storage for managed processes
  - Automatic backup and restore functionality
  - Process favorites with custom labels

- **Administrator Privileges**:
  - Automatic elevation detection
  - UAC prompt for administrator rights
  - Graceful handling of permission errors

## Requirements

- .NET 10.0
- Windows (for process management and elevation)
- Administrator privileges (required for process priority modification)

## Installation

1. Clone or download the project
2. Open the solution in Visual Studio or use the .NET CLI
3. Build the project: `dotnet build`
4. Run the application: `dotnet run --project ProcessManager/ProcessManager.csproj`

**Note**: The application requires administrator privileges to modify process priorities. It will prompt for elevation if needed.

## Usage

### Main Menu Options

1. **Manage Processes**
   - Add Process from Running: Search and select from currently running processes
   - Add Process from File: Browse for .exe files to monitor
   - View Managed Processes: See all managed processes with options to modify
   - Apply Priorities to All: Apply preferred priorities to all running managed processes

2. **Monitor Processes**
   - Real-time monitoring dashboard
   - Press Enter to apply priorities to all running processes
   - Press Esc to stop monitoring

3. **Storage Management**
   - View storage information
   - Create backups of process list
   - Restore from backup files
   - Clear all managed processes

4. **About**
   - Application information and version details

### Process Management

#### Adding Processes from Running
1. Navigate to "Manage Processes" → "Add Process from Running"
2. Optionally search for processes by name
3. Select a process from the list
4. Choose a priority level
5. Optionally add a custom label
6. Choose whether to apply the priority immediately

#### Adding Processes from Files
1. Navigate to "Manage Processes" → "Add Process from File"
2. Enter the path to an .exe file
3. Choose a priority level
4. Optionally add a custom label
5. The process will be monitored when it starts running

#### Managing Existing Processes
1. Navigate to "Manage Processes" → "View Managed Processes"
2. Select a process to manage
3. Options include:
   - Apply priority to selected process
   - Update priority for selected process
   - Remove selected process from management

### Real-time Monitoring

1. Navigate to "Monitor Processes"
2. The dashboard shows:
   - Process name and label
   - Current running status
   - Process ID
   - Memory usage
   - CPU usage
   - Current priority
   - Preferred priority (green if matches, yellow if different)
3. Press Enter to apply all priorities
4. Press Esc to stop monitoring

## Configuration

### Priority Level Mapping

The application uses custom priority levels that map to Windows process priorities:

| Custom Level | Windows Priority | Description |
|--------------|------------------|-------------|
| Low | Idle | Lowest priority, runs only when system is idle |
| Medium | Below Normal | Lower than normal priority |
| High | Above Normal | Higher than normal priority |
| Critical | High | Very high priority, may impact system performance |

### Storage Location

Managed processes are stored in:
```
%APPDATA%\ProcessManager\selected-processes.json
```

### Backup Files

Backup files are stored in the same directory with the pattern:
```
selected-processes.json.backup.{timestamp}.json
```

## Technical Architecture

### Project Structure

```
ProcessManager/
├── ProcessManager.csproj
├── Program.cs (Main entry point)
├── Core/
│   ├── ProcessManager.cs (Main business logic)
│   ├── ProcessInfo.cs (Process data model)
│   └── PriorityLevel.cs (Priority enum)
├── Services/
│   ├── ProcessService.cs (Process operations)
│   ├── JsonStorageService.cs (File persistence)
│   └── ElevationService.cs (Admin rights handling)
├── UI/
│   ├── MenuManager.cs (Main menu navigation)
│   ├── ProcessSelector.cs (Process selection UI)
│   ├── ProcessLister.cs (Running processes display)
│   └── MonitoringView.cs (Real-time monitoring UI)
└── Data/
    └── selected-processes.json (Persistent storage)
```

### Dependencies

- **Spectre.Console**: Rich console UI framework
- **System.Text.Json**: JSON serialization
- **System.Diagnostics.Process**: Process management
- **System.Windows.Forms**: MessageBox for elevation prompts

## Development

### Building the Project

```bash
cd ProcessManager
dotnet build
```

### Running Tests

The project includes comprehensive error handling and validation. To test:

1. Run the application with administrator privileges
2. Test process selection from running processes
3. Test file-based process selection
4. Test priority application and monitoring
5. Test storage management features

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Troubleshooting

### Administrator Privileges Required

If you see the message "Administrator privileges required", restart the application with administrator privileges:
- Right-click the application and select "Run as administrator"
- Or allow the UAC prompt when the application requests elevation

### Process Not Found

If a process cannot be found or modified:
- Ensure the process is running
- Verify administrator privileges
- Check if the process is protected by the system

### Storage Issues

If storage operations fail:
- Check file permissions in the application data directory
- Verify disk space
- Check for file corruption (use backup/restore)

## License

This project is licensed under the MIT License.

## Support

For issues, questions, or feature requests, please create an issue in the repository.

## Documentation

This application was developed using context7 for documentation and code generation.
