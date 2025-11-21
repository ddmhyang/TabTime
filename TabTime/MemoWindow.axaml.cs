using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json; // System.Text.Json 사용

namespace TabTime
{
    public partial class MemoWindow : Window
    {
        // 데이터 바인딩을 위한 컬렉션
        public ObservableCollection<MemoItem> AllMemos { get; set; }

        public MemoWindow()
        {
            InitializeComponent();

            // 1. 데이터 로드
            LoadMemos();

            // 2. XAML(View)과 데이터 연결
            DataContext = this;

            // 3. 닫힐 때 자동 저장
            Closing += (s, e) => SaveMemos();
        }

        private void LoadMemos()
        {
            if (File.Exists(DataManager.MemosFilePath))
            {
                try
                {
                    var json = File.ReadAllText(DataManager.MemosFilePath);
                    AllMemos = JsonSerializer.Deserialize<ObservableCollection<MemoItem>>(json)
                               ?? new ObservableCollection<MemoItem>();
                }
                catch
                {
                    AllMemos = new ObservableCollection<MemoItem>();
                }
            }
            else
            {
                AllMemos = new ObservableCollection<MemoItem>();
            }
        }

        private void SaveMemos()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(AllMemos, options);
                File.WriteAllText(DataManager.MemosFilePath, json);
            }
            catch { /* 저장 실패 무시 */ }
        }

        // --- 이벤트 핸들러 ---

        private void MemoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            var editorPanel = this.FindControl<Grid>("EditorPanel");
            var contentBox = this.FindControl<TextBox>("MemoContentTextBox");
            var pinCheck = this.FindControl<CheckBox>("PinCheckBox");

            if (listBox?.SelectedItem is MemoItem selectedMemo)
            {
                if (editorPanel != null) editorPanel.IsEnabled = true;
                if (contentBox != null) contentBox.Text = selectedMemo.Content;
                if (pinCheck != null) pinCheck.IsChecked = selectedMemo.IsPinned;
            }
            else
            {
                if (editorPanel != null) editorPanel.IsEnabled = false;
                if (contentBox != null) contentBox.Text = "";
                if (pinCheck != null) pinCheck.IsChecked = false;
            }
        }

        private void MemoContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("MemoListBox");
            var contentBox = sender as TextBox;

            if (listBox?.SelectedItem is MemoItem selectedMemo && contentBox != null)
            {
                selectedMemo.Content = contentBox.Text;
            }
        }

        private void PinCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("MemoListBox");
            var pinCheck = sender as CheckBox;

            if (listBox?.SelectedItem is MemoItem selectedMemo)
            {
                bool isNowPinned = pinCheck?.IsChecked ?? false;

                // 다른 메모들의 고정 상태 해제 (하나만 고정)
                foreach (var memo in AllMemos)
                {
                    memo.IsPinned = false;
                }

                selectedMemo.IsPinned = isNowPinned;

                // 강제 UI 갱신 (Avalonia에서는 프로퍼티 변경 알림이 잘 작동하면 필요 없지만 안전을 위해)
                // listBox.Items = null; 
                // listBox.Items = AllMemos; 
                // (ObservableCollection을 쓰므로 보통은 자동 갱신됩니다)
            }
        }

        private void NewMemoButton_Click(object sender, RoutedEventArgs e)
        {
            var newMemo = new MemoItem { Content = "새 메모" };
            AllMemos.Insert(0, newMemo);

            var listBox = this.FindControl<ListBox>("MemoListBox");
            var contentBox = this.FindControl<TextBox>("MemoContentTextBox");

            if (listBox != null) listBox.SelectedItem = newMemo;
            contentBox?.Focus();
        }

        private void DeleteMemoButton_Click(object sender, RoutedEventArgs e)
        {
            var listBox = this.FindControl<ListBox>("MemoListBox");

            if (listBox?.SelectedItem is MemoItem selectedMemo)
            {
                // Avalonia에는 기본 MessageBox가 없으므로, 일단 확인 없이 바로 삭제합니다.
                // (나중에 팝업창을 추가하면 됩니다)
                AllMemos.Remove(selectedMemo);
            }
        }
    }
}