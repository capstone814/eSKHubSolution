// Components/Services/ThemeService.cs
using Microsoft.JSInterop;

namespace eSKHub.Components.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private bool _isDarkMode = false;
        private bool _initialized = false;

        public event Action? OnThemeChanged;

        public bool IsDarkMode => _isDarkMode;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                _isDarkMode = await _jsRuntime.InvokeAsync<bool>("themeStorage.getDarkMode");
            }
            catch
            {
                _isDarkMode = false;
            }

            _initialized = true;
        }

        public async Task ToggleDarkModeAsync()
        {
            _isDarkMode = !_isDarkMode;

            try
            {
                await _jsRuntime.InvokeVoidAsync("themeStorage.setDarkMode", _isDarkMode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save theme preference: {ex.Message}");
            }

            OnThemeChanged?.Invoke();
        }

        public async Task SetDarkModeAsync(bool isDarkMode)
        {
            if (_isDarkMode == isDarkMode) return;

            _isDarkMode = isDarkMode;

            try
            {
                await _jsRuntime.InvokeVoidAsync("themeStorage.setDarkMode", _isDarkMode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save theme preference: {ex.Message}");
            }

            OnThemeChanged?.Invoke();
        }
    }
}