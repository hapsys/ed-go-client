using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using EdGo.EdgoClient;

namespace EdGo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

		private Client client = Client.instance;

		private bool started = false;

        public System.Windows.Forms.NotifyIcon trayIcon; //Icon for system tray.


        public MainWindow()
        {
            InitializeComponent();
            //Create tray icon and setup initial settings for it
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Icon = Properties.Resources.stoped;
            trayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info; //Shows tip on systim tray icon with caugtion about continuing working.
            trayIcon.BalloonTipText = "ED-GO Client still work in tray and continue tracking your flight journal!";
            trayIcon.BalloonTipTitle = "I am track your logs!";
            trayIcon.Click += ToggleMinimizeState; //Handle click at tray icon
            trayIcon.Visible = false;

            if (Properties.Settings.Default.StartMinimized)
            {
                WindowState = WindowState.Minimized;
                trayIcon.Visible = true;
                this.Visibility = Visibility.Hidden;
            }

            TextLogger.instance.output = textOut;
			AppDispatcher.instance.mWin = this;
			AppDispatcher.instance.process();

            if (Properties.Settings.Default.AutoStartProc) AppDispatcher.instance.pressStart();
        }
        
        // Toggle state between Normal and Minimized when click at trayIcon.
        private void ToggleMinimizeState(object sender, EventArgs e)
        {
            bool isMinimized = this.WindowState == WindowState.Minimized;
            if (isMinimized)
            {
                Show();
                WindowState = WindowState.Normal;
                trayIcon.Visible = false;
            }
        }

        //Event for window state chenged, it added in MainWindow.xaml
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(3000);
                this.Hide();
            }
            else
            {
                Show();
                WindowState = WindowState.Normal;
                trayIcon.Visible = false;
            }
        }

        private void edgoSettings_Click(object sender, RoutedEventArgs e)
        {
			AppDispatcher.instance.showClientSettings();
        }

		private void start()
		{
			IDictionary<string, string> startInfo = client.getLastInfo();
			if (startInfo != null) { 
			foreach (string key in startInfo.Keys)
				{
					Console.WriteLine(key + "=>" + startInfo[key]);
				}
			}

		}
		private void startStop(bool state)
		{
			started = state;
			if (started)
			{
				buttonProcess.Content = "Stop";
				//start();
			}
			else
			{
				buttonProcess.Content = "Start";
			}

		}

		private void buttonProcess_Click(object sender, RoutedEventArgs e)
		{
			//startStop(!started);
			AppDispatcher.instance.pressStart();
		}

		public void showStopButton()
		{
			buttonProcess.Content = "Stop";
		}
		public void showStartButton()
		{
			buttonProcess.Content = "Start";
		}
		public void setStartStopState(bool state = true) 
		{
			buttonProcess.IsEnabled = state;
			buttonCompanion.IsEnabled = state;
		}

		private void textOut_TextChanged(object sender, TextChangedEventArgs e)
		{
			textOut.ScrollToEnd();
		}

		public bool showNewPilotDialog(String name)
		{
			bool result = true;

			string message = "New pilot name \"" + name + "\"\nIs new E:D account?";
			string caption = "Found unused pilot name!!!";
			MessageBoxButton buttons = MessageBoxButton.YesNo;
			MessageBoxImage icon = MessageBoxImage.Question;
			MessageBoxResult defaultResult = MessageBoxResult.Yes;
			result = MessageBox.Show(message, caption, buttons, icon, defaultResult).Equals(defaultResult);

			return result;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			//System.Windows.Application.Current.Exit();
			System.Environment.Exit(1);
		}

		private void buttonCompanion_Click(object sender, RoutedEventArgs e)
		{
			AppDispatcher.instance.showCompanionWindow();
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
    }

}
