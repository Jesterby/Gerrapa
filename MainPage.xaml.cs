using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App3
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Helpers.CreateHttpClient(ref httpClient);
            cts = new CancellationTokenSource();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // If the navigation is external to the app do not clean up.
            // This can occur on Phone when suspending the app.
            if (e.NavigationMode == NavigationMode.Forward && e.Uri == null)
            {
                return;
            }

            Dispose();
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            {
                Uri resourceAddress;

                // The value of 'AddressField' is set by the user and is therefore untrusted input. If we can't create a
                // valid, absolute URI, we'll notify the user about the incorrect input.
                if (!Helpers.TryGetUri(AddressField.Text, out resourceAddress))
                {
                    rootPage.NotifyUser("Invalid URI.", NotifyType.ErrorMessage);
                    return;
                }

                Helpers.ScenarioStarted(StartButton, CancelButton, OutputField);
                rootPage.NotifyUser("In progress", NotifyType.StatusMessage);

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(resourceAddress).AsTask(cts.Token);

                    await Helpers.DisplayTextResultAsync(response, OutputField, cts.Token);
                    response.EnsureSuccessStatusCode();

                    XElement element = XElement.Parse(await response.Content.ReadAsStringAsync().AsTask(cts.Token));
                    OutputList.ItemsSource = (
                        from c in element.Elements("item")
                        select c.Attribute("name").Value);

                    rootPage.NotifyUser("Completed", NotifyType.StatusMessage);
                }
                catch (TaskCanceledException)
                {
                    rootPage.NotifyUser("Request canceled.", NotifyType.ErrorMessage);
                }
                catch (Exception ex)
                {
                    rootPage.NotifyUser("Error: " + ex.Message, NotifyType.ErrorMessage);
                }
                finally
                {
                    Helpers.ScenarioCompleted(StartButton, CancelButton);
                }
            }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            cts.Dispose();

            // Re-create the CancellationTokenSource.
            cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }

            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }
        }
    }
    }
}
