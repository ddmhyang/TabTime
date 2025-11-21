using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json; // Json 처리를 위해 필요

namespace TabTime
{
    public class TimeLogService : ITimeLogService
    {
        public async Task SaveTimeLogsAsync(ObservableCollection<TimeLogEntry> logs)
        {
            await Task.Run(() => DataManager.SaveTimeLogs(logs));
        }

        public async Task<ObservableCollection<TimeLogEntry>> LoadTimeLogsAsync()
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(DataManager.TimeLogFilePath))
                    return new ObservableCollection<TimeLogEntry>();

                try
                {
                    string json = File.ReadAllText(DataManager.TimeLogFilePath);
                    return JsonConvert.DeserializeObject<ObservableCollection<TimeLogEntry>>(json)
                           ?? new ObservableCollection<TimeLogEntry>();
                }
                catch
                {
                    return new ObservableCollection<TimeLogEntry>();
                }
            });
        }
    }
}