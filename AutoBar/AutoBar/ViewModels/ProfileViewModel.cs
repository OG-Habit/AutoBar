﻿using AutoBar.Models;
using AutoBar.Views;
using Newtonsoft.Json;
using System;
using Xamarin.Forms;

namespace AutoBar.ViewModels
{
    public class ProfileViewModel
    {
        public Command LogoutCommand { get; }
        public Command EwalletClicked { get; }
        public Command QRCodeClicked { get; }
        public Command ReportClicked { get; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string ImageLink { get; }


        public ProfileViewModel()
        {
            LogoutCommand = new Command(OnLogoutClicked);
            EwalletClicked = new Command(OnEwalletClicked);
            QRCodeClicked = new Command(OnQRCodeClicked);
            ReportClicked = new Command(OnReportClicked);
            ImageLink = "default_pic";
            SetUserDetails();
        }
        async void SetUserDetails()
        {
            var UserString = await Xamarin.Essentials.SecureStorage.GetAsync("user");
            Customer u = JsonConvert.DeserializeObject<Customer>(UserString);
            Name = u.UserDetails.FullName;
            Email = u.UserDetails.Email;
        }
        private async void OnLogoutClicked(object obj)
        {
            bool retryBool = await App.Current.MainPage.DisplayAlert("Log out", "Would you like to log out?", "Yes", "No");
            if (retryBool)
            {
                // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
                await Xamarin.Essentials.SecureStorage.SetAsync("isLogged", "0");
                await Shell.Current.GoToAsync($"//LoginPage");
            }
        }

        private async void OnEwalletClicked(object obj)
        {
            await Shell.Current.GoToAsync($"{nameof(EwalletPage)}");
        }

        private async void OnQRCodeClicked(object obj)
        {
            await Shell.Current.GoToAsync($"{nameof(QRCodePage)}");
        }

        private async void OnReportClicked(object obj)
        {
            bool retryBool = await App.Current.MainPage.DisplayAlert("Lost Card", "Report and deactivate your lost card?", "Yes", "No");
            if (retryBool)
            {
                bool retriBool = await App.Current.MainPage.DisplayAlert("New Card", "Request for a new card?", "Yes", "No");
                if (retriBool)
                {
                    Console.WriteLine("Lost Card Reported and Deactivated");
                }
            }
        }
    }
}
