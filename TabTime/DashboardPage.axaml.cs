using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TabTime
{
    public partial class DashboardPage : UserControl
    {
        private MainWindow _parentWindow;
        private AppSettings _settings;

        // 타임라인 그리기 관련 변수
        private readonly double _blockWidth = 35, _blockHeight = 17;
        private readonly double _hourLabelWidth = 30;
        private DateTime _currentDateForTimeline = DateTime.Today;

        // 드래그 관련
        private Point _dragStartPoint;
        private Rectangle _selectionBox;
        private bool _isDragging = false;

        // 딕셔너리 등
        private readonly Dictionary<string, SolidColorBrush> _taskBrushCache = new();

        public DashboardPage()
        {
            InitializeComponent();

            // ViewModel 연결
            this.DataContext = new DashboardViewModel();

            // 타임라인 배경 그리기
            InitializeTimeTableBackground();

            // 화면 로드 시 실행 (AttachedToVisualTree가 WPF의 Loaded와 비슷)
            AttachedToVisualTree += DashboardPage_AttachedToVisualTree;

            // 드래그 박스 초기화
            _selectionBox = new Rectangle
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.Parse("#321E90FF")), // 반투명 파랑
                IsVisible = false
            };
            SelectionCanvas.Children.Add(_selectionBox);
        }

        private void DashboardPage_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            // 초기 데이터 로드
            if (DataContext is DashboardViewModel vm)
            {
                // 여기서 VM의 데이터를 새로고침하거나 할 수 있음
                RecalculateAllTotals();
                RenderTimeTable();
            }
        }

        // --- 네비게이션 ---
        public void SetParentWindow(MainWindow window) => _parentWindow = window;

        private async void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null && sender is Button button && button.Tag is string pageName)
            {
                await _parentWindow.NavigateToPage(pageName);
            }
        }

        // --- 과목 추가/삭제 ---
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskInput.Text)) return;

            // ViewModel을 통해 데이터 추가 (여기서는 간소화)
            if (DataContext is DashboardViewModel vm)
            {
                var newTask = new TaskItem { Text = TaskInput.Text };
                vm.TaskItems.Add(newTask);
                TaskInput.Clear();

                // 저장 로직 호출 필요
                new TaskService().SaveTasksAsync(vm.TaskItems);
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TaskItem task)
            {
                if (DataContext is DashboardViewModel vm)
                {
                    vm.TaskItems.Remove(task);
                    new TaskService().SaveTasksAsync(vm.TaskItems);
                }
            }
        }

        private void TaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AddTaskButton_Click(sender, new RoutedEventArgs());
        }

        // --- 색상 변경 (마우스 이벤트 -> 포인터 이벤트) ---
        private void ChangeTaskColor_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Avalonia에서는 색상 피커를 직접 구현해야 하거나 라이브러리를 써야 합니다.
            // 일단 임시로 랜덤 색상으로 변경하게 해둘게요!
            if (sender is Border border && border.Tag is TaskItem task)
            {
                var rnd = new Random();
                var color = Color.FromRgb((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
                task.ColorBrush = new SolidColorBrush(color);
            }
        }

        // --- 타임라인 렌더링 ---
        private void InitializeTimeTableBackground()
        {
            TimeTableContainer.Children.Clear();
            for (int hour = 0; hour < 24; hour++)
            {
                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };

                var label = new TextBlock
                {
                    Text = $"{hour:00}",
                    Width = _hourLabelWidth,
                    Height = _blockHeight,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 8,
                    Foreground = Brushes.Gray
                };
                rowPanel.Children.Add(label);

                for (int m = 0; m < 6; m++)
                {
                    var block = new Border
                    {
                        Width = _blockWidth,
                        Height = _blockHeight,
                        Background = Brushes.WhiteSmoke,
                        BorderThickness = new Thickness(1, 0, (m + 1) % 6 == 0 ? 1 : 0, 1),
                        BorderBrush = Brushes.LightGray,
                        Margin = new Thickness(1, 0, 1, 0)
                    };
                    rowPanel.Children.Add(block);
                }
                TimeTableContainer.Children.Add(rowPanel);
            }
        }

        private void RenderTimeTable()
        {
            if (DataContext is not DashboardViewModel vm) return;

            // 기존 타임바 삭제 (SelectionBox 제외)
            var toRemove = SelectionCanvas.Children.OfType<Border>().ToList();
            foreach (var item in toRemove) SelectionCanvas.Children.Remove(item);

            // ViewModel의 로그를 가져와서 그리기
            var logs = vm.TimeLogEntries.Where(l => l.StartTime.Date == _currentDateForTimeline.Date).ToList();

            foreach (var log in logs)
            {
                // 좌표 계산 로직 (WPF와 유사)
                double pixelsPerMin = _blockWidth / 10.0;
                int cellIndex = log.StartTime.Minute / 10;
                double offsetInCell = (log.StartTime.Minute % 10) * pixelsPerMin;

                double left = _hourLabelWidth + (cellIndex * (_blockWidth + 2)) + 1 + offsetInCell;
                double top = log.StartTime.Hour * (_blockHeight + 2) + 1;
                double width = log.Duration.TotalMinutes * pixelsPerMin;

                var bar = new Border
                {
                    Width = width,
                    Height = _blockHeight,
                    Background = Brushes.LightBlue, // 나중에 Task 색상으로 변경
                    CornerRadius = new CornerRadius(2),
                    Tag = log
                };

                Canvas.SetLeft(bar, left);
                Canvas.SetTop(bar, top);
                SelectionCanvas.Children.Add(bar);
            }
        }

        private void RecalculateAllTotals()
        {
            if (DataContext is DashboardViewModel vm)
            {
                // 여기서 총 시간 계산 로직 호출 가능
                // vm.RecalculateAllTotalsFromLogs();
            }
        }

        // --- 드래그 선택 (Pointer 이벤트 사용) ---
        private void SelectionCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(SelectionCanvas);
            if (point.Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _dragStartPoint = point.Position;
                Canvas.SetLeft(_selectionBox, _dragStartPoint.X);
                Canvas.SetTop(_selectionBox, _dragStartPoint.Y);
                _selectionBox.Width = 0;
                _selectionBox.Height = 0;
                _selectionBox.IsVisible = true;
                e.Pointer.Capture(SelectionCanvas);
            }
        }

        private void SelectionCanvas_PointerMoved(object sender, PointerEventArgs e)
        {
            if (!_isDragging) return;

            var currentPos = e.GetPosition(SelectionCanvas);
            var x = Math.Min(currentPos.X, _dragStartPoint.X);
            var y = Math.Min(currentPos.Y, _dragStartPoint.Y);
            var w = Math.Abs(currentPos.X - _dragStartPoint.X);
            var h = Math.Abs(currentPos.Y - _dragStartPoint.Y);

            Canvas.SetLeft(_selectionBox, x);
            Canvas.SetTop(_selectionBox, y);
            _selectionBox.Width = w;
            _selectionBox.Height = h;
        }

        private void SelectionCanvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _selectionBox.IsVisible = false;
                e.Pointer.Capture(null);

                var selectionRect = new Rect(Canvas.GetLeft(_selectionBox), Canvas.GetTop(_selectionBox), _selectionBox.Width, _selectionBox.Height);

                // 너무 작으면 무시
                if (selectionRect.Width < 10 && selectionRect.Height < 10) return;

                var selectedLogs = new List<TimeLogEntry>();
                foreach (var child in SelectionCanvas.Children.OfType<Border>())
                {
                    if (child.Tag is TimeLogEntry logEntry)
                    {
                        var logRect = new Rect(Canvas.GetLeft(child), Canvas.GetTop(child), child.Bounds.Width, child.Bounds.Height);
                        if (selectionRect.Intersects(logRect))
                        {
                            selectedLogs.Add(logEntry);
                        }
                    }
                }

                if (selectedLogs.Any() && DataContext is DashboardViewModel vm)
                {
                    var distinctLogs = selectedLogs.Distinct().ToList();

                    // 일괄 수정 창 띄우기
                    var bulkWin = new BulkEditLogsWindow(distinctLogs, vm.TaskItems);
                    var result = await bulkWin.ShowDialog<bool>((Window)VisualRoot);

                    if (result)
                    {
                        if (bulkWin.Result == BulkEditResult.Delete)
                        {
                            foreach (var log in distinctLogs) vm.TimeLogEntries.Remove(log);
                        }
                        else if (bulkWin.Result == BulkEditResult.ChangeTask && bulkWin.SelectedTask != null)
                        {
                            foreach (var log in distinctLogs) log.TaskText = bulkWin.SelectedTask.Text;
                        }

                        await new TimeLogService().SaveTimeLogsAsync(vm.TimeLogEntries);
                        RecalculateAllTotals();
                        RenderTimeTable();
                    }
                }
            }
        }

        // --- 날짜 변경 ---
        private void PrevDayButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDateForTimeline = _currentDateForTimeline.AddDays(-1);
            UpdateDateDisplay();
        }
        private void NextDayButton_Click(object sender, RoutedEventArgs e)
        {
            _currentDateForTimeline = _currentDateForTimeline.AddDays(1);
            UpdateDateDisplay();
        }
        private void UpdateDateDisplay()
        {
            CurrentDateDisplay.Text = _currentDateForTimeline.ToString("yyyy-MM-dd");
            CurrentDayDisplay.Text = _currentDateForTimeline.ToString("ddd");
            RenderTimeTable();
        }

        // --- 할일(Todo) 및 기타 ---
        private void AddTodoButton_Click(object sender, RoutedEventArgs e) { /* 구현 필요 */ }
        private void TodoInput_KeyDown(object sender, KeyEventArgs e) { /* 구현 필요 */ }
        private async void AddManualLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DashboardViewModel vm) return;

            // 기본값 설정 (현재 시간부터 1시간)
            var now = DateTime.Now;
            var templateLog = new TimeLogEntry
            {
                StartTime = now,
                EndTime = now.AddHours(1),
                TaskText = vm.TaskItems.FirstOrDefault()?.Text ?? "과목 없음"
            };

            // 창 띄우기
            var win = new AddLogWindow(vm.TaskItems, templateLog);
            var result = await win.ShowDialog<bool>((Window)VisualRoot);

            if (result)
            {
                if (win.IsDeleted) return; // 삭제 버튼 눌렀으면 무시

                if (win.NewLogEntry != null)
                {
                    // 뷰모델에 추가 요청
                    vm.TimeLogEntries.Add(win.NewLogEntry);
                    await new TimeLogService().SaveTimeLogsAsync(vm.TimeLogEntries);

                    // 화면 갱신
                    RecalculateAllTotals();
                    RenderTimeTable();
                }
            }
        }
        private void ChangeImageButton_Click(object sender, RoutedEventArgs e) { /* 구현 필요 */ }

        private async void PinnedMemo_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            var memoWindow = new MemoWindow();

            // 창을 띄우고 닫힐 때까지 기다립니다 (await)
            await memoWindow.ShowDialog((Window)VisualRoot);

            // 창이 닫히면 대시보드의 메모 데이터를 새로고침합니다.
            // (LoadMemosAsync 같은 메서드가 있다면 호출)
            // await LoadMemosAsync(); 
        }

    }
}