using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace TabTime
{
    public interface ITaskService
    {
        Task SaveTasksAsync(ObservableCollection<TaskItem> tasks);
        Task<ObservableCollection<TaskItem>> LoadTasksAsync();
    }
}