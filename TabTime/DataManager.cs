using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json; // TaskService 등에서 사용

namespace TabTime
{
    public static class DataManager
    {
        // ✨ [변경] 저장 경로를 'TabTime'으로 변경
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TabTime");

        public static readonly string SettingsFilePath = Path.Combine(AppDataPath, "settings.json");
        public static readonly string TasksFilePath = Path.Combine(AppDataPath, "tasks.json");
        public static readonly string TimeLogFilePath = Path.Combine(AppDataPath, "timelogs.json");
        public static readonly string TodosFilePath = Path.Combine(AppDataPath, "todos.json");
        public static readonly string MemosFilePath = Path.Combine(AppDataPath, "memos.json");

        // ▼▼▼ [수정] EventHandler -> Action으로 변경 
        public static event Action SettingsUpdated;

        static DataManager()
        {
            if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
        }

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsFilePath)) return new AppSettings();
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
                SettingsUpdated?.Invoke();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}"); }
        }

        // --- 저장 메서드들 ---

        public static void SaveTasks(ObservableCollection<TaskItem> tasks)
        {
            try
            {
                string json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
                File.WriteAllText(TasksFilePath, json);
            }
            catch { }
        }

        public static void SaveTimeLogs(ObservableCollection<TimeLogEntry> logs)
        {
            try
            {
                string json = JsonConvert.SerializeObject(logs, Formatting.Indented);
                File.WriteAllText(TimeLogFilePath, json);
            }
            catch { }
        }

        public static void SaveTimeLogsImmediately(ObservableCollection<TimeLogEntry> logs)
        {
            SaveTimeLogs(logs);
        }
    }
}