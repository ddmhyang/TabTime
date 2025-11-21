using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TabTime
{
    // 결과 타입 정의
    public enum BulkEditResult { None, ChangeTask, Delete }

    public partial class BulkEditLogsWindow : Window
    {
        public BulkEditResult Result { get; private set; } = BulkEditResult.None;
        public TaskItem SelectedTask { get; private set; }

        public BulkEditLogsWindow() { InitializeComponent(); }

        public BulkEditLogsWindow(List<TimeLogEntry> logs, ObservableCollection<TaskItem> tasks) : this()
        {
            var taskCombo = this.FindControl<ComboBox>("TaskComboBox");
            if (taskCombo != null)
            {
                taskCombo.ItemsSource = tasks;
                taskCombo.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var changeRadio = this.FindControl<RadioButton>("ChangeTaskRadio");
            var taskCombo = this.FindControl<ComboBox>("TaskComboBox");

            if (changeRadio?.IsChecked == true)
            {
                Result = BulkEditResult.ChangeTask;
                SelectedTask = taskCombo?.SelectedItem as TaskItem;

                // 과목을 선택 안 했으면 취소 취급
                if (SelectedTask == null)
                {
                    Close(false);
                    return;
                }
            }
            else
            {
                Result = BulkEditResult.Delete;
            }

            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}