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

		public MainWindow()
        {
            InitializeComponent();
			TextLogger.instance.output = textOut;
			AppDispatcher.instance.mWin = this;
			AppDispatcher.instance.process();

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
	}
}
