using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace TabTime
{
    public partial class InputWindow : Window
    {
        // 입력된 값을 저장할 속성
        public string ResponseText { get; private set; } = "";

        public InputWindow()
        {
            InitializeComponent();
        }

        // 생성자: 제목과 기본값을 받아서 세팅
        public InputWindow(string prompt, string defaultText = "") : this()
        {
            // XAML에 있는 컨트롤 찾기
            var promptBlock = this.FindControl<TextBlock>("PromptText");
            var inputBox = this.FindControl<TextBox>("InputTextBox");

            if (promptBlock != null) promptBlock.Text = prompt;
            if (inputBox != null) inputBox.Text = defaultText;

            // 창이 열리면 텍스트박스에 포커스 주고 전체 선택
            this.Opened += (s, e) =>
            {
                inputBox?.Focus();
                inputBox?.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = this.FindControl<TextBox>("InputTextBox");
            ResponseText = inputBox?.Text ?? "";

            // ✨ [핵심] true를 반환하며 창 닫기
            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // false를 반환하며 창 닫기
            Close(false);
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }
    }
}