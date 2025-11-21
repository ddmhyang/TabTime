using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace TabTime
{
    public interface ITimeLogService
    {
        Task SaveTimeLogsAsync(ObservableCollection<TimeLogEntry> logs);
        Task<ObservableCollection<TimeLogEntry>> LoadTimeLogsAsync();
    }
}