using CommunityToolkit.Mvvm.ComponentModel;

namespace SecureBird_Data_Store.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected async Task RunAsync(Func<Task> action, string busyMessage = "Working...")
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = busyMessage;
        try
        {
            await action();
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
