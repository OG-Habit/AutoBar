﻿using Xamarin.Forms;

namespace AutoBarBar.ViewModels
{
    public class ProfileViewModel
    {
        public Command LogoutCommand { get; }
        public string Name { get; }
        public string Email { get; }

        public ProfileViewModel()
        {
            LogoutCommand = new Command(OnLogoutClicked);
            Name = "Test Testing Tester";
            Email = "test@email.com";
        }

        private async void OnLogoutClicked(object obj)
        {
            // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
            await Xamarin.Essentials.SecureStorage.SetAsync("isLogged", "0");
            await Shell.Current.GoToAsync($"//LoginPage");
        }
    }
}