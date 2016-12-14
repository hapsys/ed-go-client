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
using System.Windows.Shapes;
using EdGo.EdgoClient;

namespace EdGo
{
    /// <summary>
    /// Логика взаимодействия для ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {

		Client client = Client.instance;
        public ClientWindow()
        {
            InitializeComponent();
			getSettings();
			showButtons();
        }
		private void showButtons()
		{
			if (client.isTested)
			{
				buttonTest.IsEnabled = false;
				buttonOk.IsEnabled = true;
			}
			else if (client.canTest)
			{
				buttonTest.IsEnabled = true;
				buttonOk.IsEnabled = false;
			}
			else if (!client.canTest && !client.isTested)
			{
				buttonTest.IsEnabled = false;
				buttonOk.IsEnabled = false;
			}
		}

		private void getSettings()
		{
			inputURL.Text = client.url;
			inputUserID.Text = client.userId;
			inputUserKey.Text = client.userKey;
		}
		private void inputs_KeyUp(object sender, KeyEventArgs e)
		{
			client.url = inputURL.Text;
			client.userId = inputUserID.Text;
			client.userKey = inputUserKey.Text;
			client.isTested = false;
			showButtons();
		}

		private void buttonTest_Click(object sender, RoutedEventArgs e)
		{
			this.IsEnabled = false;
			if (client.httpTest())
			{
				MessageBox.Show("Test OK", "Testing connection", MessageBoxButton.OK);
			} else
			{
				MessageBox.Show("Test FAILED!", "Testing connection", MessageBoxButton.OK);
			}
			this.IsEnabled = true;
			showButtons();
		}

		private void buttonOk_Click(object sender, RoutedEventArgs e)
		{
			client.saveDefault();
			this.Close();
			AppDispatcher.instance.hideClientSettings();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			client.retunDefault();
			AppDispatcher.instance.hideClientSettings();
		}
	}
}
