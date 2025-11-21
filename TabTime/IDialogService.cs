namespace TabTime
{
    public interface IDialogService
    {
        void ShowAlert(string message, string title);
        bool AskConfirmation(string message, string title);
    }
}