using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Threading.Tasks;

namespace TabTime
{
    public partial class MainWindow : Window
    {
        // 화면(페이지)들을 담아둘 변수
        private DashboardPage _dashboardPage;
        private SettingsPage _settingsPage;
        // private MiniTimerWindow _miniTimer; // (미니 타이머는 나중에 구현)

        private DashboardViewModel _mainViewModel;

        public MainWindow()
        {
            InitializeComponent();

            // 1. 대시보드 페이지 생성 및 초기화
            _dashboardPage = new DashboardPage();

            // 2. 기본 화면 설정
            MainFrame.Content = _dashboardPage;

            // 3. 이벤트 연결 (Opened는 WPF의 Loaded와 같습니다)
            Opened += MainWindow_Opened;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Opened(object sender, System.EventArgs e)
        {
            // ToggleMiniTimer(); // 나중에 구현
        }

        private void MainWindow_Closing(object sender, WindowClosingEventArgs e) // CancelEventArgs -> WindowClosingEventArgs
        {
            // _miniTimer?.Close();

            if (_dashboardPage.DataContext is DashboardViewModel vm)
            {
                vm.Shutdown();
            }
            // Avalonia는 App Shutdown을 자동으로 처리하므로 명시적 호출 불필요
        }

        // --- 네비게이션 (화면 전환) ---
        private async void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageName)
            {
                await NavigateToPage(pageName);
            }
        }

        public async Task NavigateToPage(string pageName)
        {
            switch (pageName)
            {
                case "Dashboard":
                    MainFrame.Content = _dashboardPage;
                    // await _dashboardPage.LoadAllDataAsync(); // (필요 시 구현)
                    break;

                case "Settings":
                    if (_settingsPage == null)
                    {
                        _settingsPage = new SettingsPage();
                        // _settingsPage.SetParentWindow(this);
                    }
                    // _settingsPage.LoadData();
                    MainFrame.Content = _settingsPage;
                    break;
            }
        }

        // --- 윈도우 컨트롤 (타이틀바 기능) ---

        // 드래그 이동
        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e); // ✨ WPF: DragMove() -> Avalonia: BeginMoveDrag(e)
            }
        }

        // 최소화
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // 닫기
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}