using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Avalonia.Media; // ✨ WPF의 System.Windows.Media 대신 이걸 씁니다!

namespace TabTime // ✨ 프로젝트 이름에 맞춰 네임스페이스 변경
{
    /// <summary>
    /// 하나의 과목에 대한 데이터를 나타내는 모델 클래스입니다.
    /// (WPF 버전과 거의 같지만, 색상 처리 부분만 Avalonia용으로 변경되었습니다.)
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        private TimeSpan _totalTime;
        /// <summary>
        /// 이 과목의 총 학습 시간입니다.
        /// JsonIgnore 특성을 사용하여 이 데이터는 파일에 저장되지 않도록 합니다.
        /// </summary>
        [JsonIgnore]
        public TimeSpan TotalTime
        {
            get => _totalTime;
            set
            {
                if (_totalTime != value)
                {
                    _totalTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalTimeFormatted));
                }
            }
        }

        /// <summary>
        /// TotalTime을 UI에 표시하기 위한 포맷된 문자열입니다.
        /// </summary>
        [JsonIgnore]
        public string TotalTimeFormatted => $"{(int)TotalTime.TotalHours:00}:{TotalTime.Minutes:00}:{TotalTime.Seconds:00}";

        // ✨ [변경] WPF의 'Brush' 대신 Avalonia의 'IBrush'를 사용합니다.
        // 기본값 설정 방식도 Avalonia의 Brushes를 사용합니다.
        private IBrush _colorBrush = Brushes.Gray;

        [JsonIgnore] // 파일에 저장할 필요 없는 UI 전용 속성
        public IBrush ColorBrush
        {
            get => _colorBrush;
            set
            {
                _colorBrush = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Text;
        }
    }
}