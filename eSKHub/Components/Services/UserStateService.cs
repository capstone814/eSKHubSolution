using Blazored.SessionStorage;

namespace eSKHub.Components.Services
{
    public class UserStateService(ISessionStorageService sessionStorage)
    {
        private readonly ISessionStorageService _sessionStorage = sessionStorage;

        public bool IsAuthenticated { get; private set; } = false;
        public string? Username { get; private set; }

        public string? Fullname { get; private set; }
        public string? Role { get; private set; }
        public string? Position { get; private set; }

        public string? Area { get; private set; }

        public async Task InitializeAsync()
        {
            try
            {
                var username = await _sessionStorage.GetItemAsync<string>("username");
                if (!string.IsNullOrWhiteSpace(username))
                {
                    IsAuthenticated = true;
                    Username = username;

                    var fullname = await _sessionStorage.GetItemAsync<string>("fullname");
                    Fullname = fullname;

                    var role = await _sessionStorage.GetItemAsync<string>("role");
                    Role = role;

                    var position = await _sessionStorage.GetItemAsync<string>("position");
                    Position = position;

                    var area = await _sessionStorage.GetItemAsync<string>("area");
                    Area = area;
                }
            }
            catch (InvalidOperationException)
            {
                // JS interop not available during prerendering
            }
        }

        public async Task Login(string username)
        {
            IsAuthenticated = true;
            Username = username;
            await _sessionStorage.SetItemAsync("username", username);
        }

        public async Task Logout()
        {
            IsAuthenticated = false;
            Username = null;
            await _sessionStorage.RemoveItemAsync("username");
        }

        public async Task SetUsername(string username)
        {
            Username = username;
            await _sessionStorage.SetItemAsync("username", username);
        }

        public async Task SetFullname(string fullname)
        {
            Fullname = fullname;
            await _sessionStorage.SetItemAsync("fullname", fullname);
        }

        public async Task SetPosition(string position)
        {
            Position = position;
            await _sessionStorage.SetItemAsync("position", position);
        }
        public async Task SetArea(string area)
        {
            Area = area;
            await _sessionStorage.SetItemAsync("area", area);
        }

        public async Task SetRole(string role)
        {
            Role = role;
            await _sessionStorage.SetItemAsync("role", role);
        }
    }
}