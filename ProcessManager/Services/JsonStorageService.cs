using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Core;

namespace ProcessManager.Services
{
    /// <summary>
    /// Service for managing persistent storage of process information using JSON.
    /// </summary>
    public class JsonStorageService
    {
        private readonly string _storageFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the JsonStorageService class.
        /// </summary>
        /// <param name="storageFilePath">The path to the JSON storage file.</param>
        public JsonStorageService(string storageFilePath = null)
        {
            _storageFilePath = storageFilePath ?? GetDefaultStoragePath();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_storageFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Gets the default storage file path.
        /// </summary>
        /// <returns>The default storage file path.</returns>
        private string GetDefaultStoragePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDir = Path.Combine(appData, "ProcessManager");
            return Path.Combine(appDir, "selected-processes.json");
        }

        /// <summary>
        /// Loads the list of managed processes from JSON storage.
        /// </summary>
        /// <returns>The list of managed processes.</returns>
        public List<ProcessInfo> LoadProcesses()
        {
            if (!File.Exists(_storageFilePath))
            {
                return new List<ProcessInfo>();
            }

            try
            {
                var json = File.ReadAllText(_storageFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<ProcessInfo>();
                }

                var storageData = JsonSerializer.Deserialize<ProcessStorageData>(json, _jsonOptions);
                return storageData?.Processes ?? new List<ProcessInfo>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load processes from {_storageFilePath}", ex);
            }
        }

        /// <summary>
        /// Saves the list of managed processes to JSON storage.
        /// </summary>
        /// <param name="processes">The list of processes to save.</param>
        public void SaveProcesses(List<ProcessInfo> processes)
        {
            if (processes == null)
                throw new ArgumentNullException(nameof(processes));

            try
            {
                var storageData = new ProcessStorageData
                {
                    Processes = processes,
                    LastUpdated = DateTime.Now
                };

                var json = JsonSerializer.Serialize(storageData, _jsonOptions);
                File.WriteAllText(_storageFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save processes to {_storageFilePath}", ex);
            }
        }

        /// <summary>
        /// Backs up the current storage file.
        /// </summary>
        /// <returns>True if backup was successful.</returns>
        public bool BackupStorage()
        {
            if (!File.Exists(_storageFilePath))
                return true; // Nothing to backup

            try
            {
                var backupPath = _storageFilePath + $".backup.{DateTime.Now:yyyyMMddHHmmss}.json";
                File.Copy(_storageFilePath, backupPath, overwrite: true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Restores from a backup file.
        /// </summary>
        /// <param name="backupFileName">The backup file name to restore from.</param>
        /// <returns>True if restore was successful.</returns>
        public bool RestoreFromBackup(string backupFileName)
        {
            if (string.IsNullOrWhiteSpace(backupFileName))
                throw new ArgumentException("Backup file name cannot be empty", nameof(backupFileName));

            var backupPath = Path.Combine(Path.GetDirectoryName(_storageFilePath), backupFileName);
            
            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"Backup file not found: {backupPath}");

            try
            {
                File.Copy(backupPath, _storageFilePath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore from backup: {backupPath}", ex);
            }
        }

        /// <summary>
        /// Gets a list of available backup files.
        /// </summary>
        /// <returns>List of backup file names.</returns>
        public List<string> GetBackupFiles()
        {
            var directory = Path.GetDirectoryName(_storageFilePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return new List<string>();

            try
            {
                var pattern = Path.GetFileName(_storageFilePath) + ".backup.*.json";
                var backupFiles = Directory.GetFiles(directory, pattern);
                return backupFiles.Select(Path.GetFileName).ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Clears all stored processes.
        /// </summary>
        public void ClearStorage()
        {
            if (File.Exists(_storageFilePath))
            {
                File.Delete(_storageFilePath);
            }
        }

        /// <summary>
        /// Gets information about the storage file.
        /// </summary>
        /// <returns>Storage information.</returns>
        public StorageInfo GetStorageInfo()
        {
            if (!File.Exists(_storageFilePath))
            {
                return new StorageInfo
                {
                    Exists = false,
                    FilePath = _storageFilePath,
                    FileSize = 0,
                    LastModified = null,
                    ProcessCount = 0
                };
            }

            var fileInfo = new FileInfo(_storageFilePath);
            var processes = LoadProcesses();

            return new StorageInfo
            {
                Exists = true,
                FilePath = _storageFilePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                ProcessCount = processes.Count
            };
        }
    }

    /// <summary>
    /// Data structure for JSON serialization of process storage.
    /// </summary>
    internal class ProcessStorageData
    {
        [JsonPropertyName("processes")]
        public List<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Information about the storage file.
    /// </summary>
    public class StorageInfo
    {
        /// <summary>
        /// Gets or sets whether the storage file exists.
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the number of processes stored.
        /// </summary>
        public int ProcessCount { get; set; }
    }
}