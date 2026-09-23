// RoleBasePage.cs
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using eSKHub.Components.Services;
using eSKHub.Model;

namespace eSKHub.Components.Pages
{
    public abstract class RoleBasePage : ComponentBase
    {
        [Inject] protected UserStateService UserState { get; set; } = default!;
        [Inject] protected IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected string RequiredRole { get; set; } = "User";

        protected override async Task OnInitializedAsync()
        {
            await UserState.InitializeAsync();

            if (!UserState.IsAuthenticated)
            {
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }

            using var db = await DbFactory.CreateDbContextAsync();
            var currentUser = await db.Users.FirstOrDefaultAsync(u => u.Username == UserState.Username);

            if (currentUser == null)
            {
                await UserState.Logout();
                NavigationManager.NavigateTo("/login", forceLoad: true);
                return;
            }

            // Check role
            if (RequiredRole == "Admin" && currentUser.Role != "Admin")
            {
                NavigationManager.NavigateTo("/user/home", forceLoad: true);
                return;
            }
            else if (RequiredRole == "User" && currentUser.Role == "Admin")
            {
                NavigationManager.NavigateTo("/", forceLoad: true);
                return;
            }

            await OnPageInitializedAsync();
        }

        protected virtual Task OnPageInitializedAsync() => Task.CompletedTask;
    }
}