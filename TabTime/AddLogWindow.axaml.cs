using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TabTime
{
    public partial class AddLogWindow : Window
    {
        public TimeLogEntry NewLogEntry { get; private set; }
        public bool IsDeleted { get; private set; } = false;

        // 디자이너 프리뷰용 생성자
        public AddLogWindow() { InitializeComponent(); }

        public AddLogWindow(ObservableCollection<TaskItem> tasks, TimeLogEntry initialLog) : this()
        {
            var taskCombo = this.FindControl<ComboBox>("TaskComboBox");
            var startDate = this.FindControl<DatePicker>("StartDatePicker");
            var startTime = this.FindControl<TimePicker>("StartTimePicker");
            var endDate = this.FindControl<DatePicker>("EndDatePicker");
            var endTime = this.FindControl<TimePicker>("EndTimePicker");

            // 1. 과목 리스트 연결
            if (taskCombo != null)
            {
                taskCombo.ItemsSource = tasks;
                // 기존 로그의 과목을 선택
                var selected = tasks.FirstOrDefault(t => t.Text == initialLog.TaskText);
                taskCombo.SelectedItem = selected ?? tasks.FirstOrDefault();
            }

            // 2. 날짜/시간 초기화 (DateTime -> DateTimeOffset/TimeSpan 변환)
            if (startDate != null) startDate.SelectedDate = new DateTimeOffset(initialLog.StartTime);
            if (startTime != null) startTime.SelectedTime = initialLog.StartTime.TimeOfDay;

            if (endDate != null) endDate.SelectedDate = new DateTimeOffset(initialLog.EndTime);
            if (endTime != null) endTime.SelectedTime = initialLog.EndTime.TimeOfDay;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var taskCombo = this.FindControl<ComboBox>("TaskComboBox");
            var startDate = this.FindControl<DatePicker>("StartDatePicker");
            var startTime = this.FindControl<TimePicker>("StartTimePicker");
            var endDate = this.FindControl<DatePicker>("EndDatePicker");
            var endTime = this.FindControl<TimePicker>("EndTimePicker");

            // 선택된 값 가져오기
            var selectedTask = taskCombo?.SelectedItem as TaskItem;
            if (selectedTask == null) return;

            // 날짜 + 시간 합치기
            var sDate = startDate?.SelectedDate?.DateTime ?? DateTime.Today;
            var sTime = startTime?.SelectedTime ?? TimeSpan.Zero;
            var startDateTime = sDate.Date + sTime;

            var eDate = endDate?.SelectedDate?.DateTime ?? DateTime.Today;
            var eTime = endTime?.SelectedTime ?? TimeSpan.Zero;
            var endDateTime = eDate.Date + eTime;

            // 유효성 검사 (종료 시간이 시작 시간보다 빠르면 안 됨)
            if (endDateTime <= startDateTime)
            {
                // Avalonia에는 MessageBox가 없으므로 간단히 종료 시간을 시작 시간 + 1시간으로 보정하거나 무시
                endDateTime = startDateTime.AddHours(1);
            }

            NewLogEntry = new TimeLogEntry
            {
                TaskText = selectedTask.Text,
                StartTime = startDateTime,
                EndTime = endDateTime
            };

            Close(true);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            IsDeleted = true;
            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}