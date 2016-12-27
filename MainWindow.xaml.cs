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
using System.Runtime.InteropServices;
using System.Threading;
using EdGo.EdgoClient;

namespace EdGo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

		private readonly Client _client = Client.instance;

		public MainWindow()
        {
            InitializeComponent();
			TextLogger.instance.output = textOut;
			AppDispatcher.instance.mWin = this;
			AppDispatcher.instance.process();
		    AppDispatcher.instance.ProcessEndEvent += OnProcessEnd;
            AppDispatcher.instance.ChangeStateProcessEvent += OnChangeStateProcess;

        }

        private void OnChangeStateProcess(bool state = true)
        {
            Dispatcher.Invoke(() =>
            {
                buttonProcess.IsEnabled = state;
            });
        }

        private void OnProcessEnd(bool started)
        {
            Dispatcher.Invoke(() =>
            {
                buttonProcess.Content = started ? "Stop" : "Start";
            });
        }

        private void edgoSettings_Click(object sender, RoutedEventArgs e)
        {
			AppDispatcher.instance.showClientSettings();
        }

		private void buttonProcess_Click(object sender, RoutedEventArgs e)
		{
            var thread = new Thread(AppDispatcher.instance.pressStart);
            thread.Start();
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
			Environment.Exit(1);
		}

		private void buttonCompanion_Click(object sender, RoutedEventArgs e)
		{
           var companianWindow = new Companian();
           companianWindow.ShowDialog();
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
	}

}
