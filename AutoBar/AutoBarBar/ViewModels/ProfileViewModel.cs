﻿using Xamarin.Forms;
using static AutoBarBar.Constants;

namespace AutoBarBar.ViewModels
{
    public class ProfileViewModel
    {
        public Command LogoutCommand { get; }
        public string Name { get; }
        public string Email { get; }
        public string ImageLink { get; }

        public ProfileViewModel()
        {
            LogoutCommand = new Command(OnLogoutClicked);
            Name = "Test Testing Tester";
            Email = "test@email.com";
            ImageLink = "default_pic";
        }

        private async void OnLogoutClicked(object obj)
        {
            bool retryBool = await App.Current.MainPage.DisplayAlert("Log out", "Would you like to log out?", "Yes", "No");
            if (retryBool)
            {
                // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
                await Xamarin.Essentials.SecureStorage.SetAsync($"{KEY_ISLOGGED}", "0");
                await Shell.Current.GoToAsync($"//LoginPage");
            }  
        }
    }
}
