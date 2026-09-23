public class LoadingService
{
    public event Func<Task>? OnChangeAsync;
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            // Fire async event if subscribed
            _ = OnChangeAsync?.Invoke();
        }
    }
}
