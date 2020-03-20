﻿using Saplin.xOPS.UI.ViewModels;
using Xamarin.Forms;

namespace Saplin.xOPS.UI
{
    public partial class App : Application
    {
        public App()
        {
            Pages.Init();

            InitializeComponent();

            if (Rose)
            {
                ApplyTheme();
            }

            MainPage = Pages._HostPage;

            if (!Pages.StartPage.Skip)
                Pages.ShowPage(Pages.StartPage);
            else Pages.ShowPage(Pages.MainPage);
        }

        //private async void EagerInit()
        //{
        //    //await VmLocator.EagerCreateViewModels();
        //    Pages.Init();
        //    //await Pages.EagerCreatePages();
        //}

        private void ApplyTheme()
        {
            var theme = new Rose();

            foreach (var key in theme.Keys)
            {
                if (Xamarin.Forms.Application.Current.Resources.ContainsKey(key))
                    Xamarin.Forms.Application.Current.Resources[key] = theme[key];
            }
        }

        public bool Rose
        {
            get
            {
                return Resources["Rose"] is bool && (bool)Resources["Rose"];
            }
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
