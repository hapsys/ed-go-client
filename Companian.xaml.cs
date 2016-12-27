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

using EddiCompanionAppService;

namespace EdGo
{
	/// <summary>
	/// Логика взаимодействия для Window1.xaml
	/// </summary>
	public partial class Companian : Window
	{
		public Companian()
		{
			InitializeComponent();
		}

		private void input_KeyUp(object sender, KeyEventArgs e)
		{
			CompanionAppCredentials.Instance().email = inputEmail.Text;
			CompanionAppCredentials.Instance().password = inputPassword.Text;
		}
		private void inputCode_KeyUp(object sender, KeyEventArgs e)
		{
			CompanionAppCredentials.Instance().code = inputCode.Text;
		}

		private void buttonNext_Click(object sender, RoutedEventArgs e)
		{
			AppDispatcher.instance.companionNext();
		}

		public void setState(CompanionAppService.State state)
		{
			switch (state) {
				case CompanionAppService.State.NEEDS_LOGIN:
					inputEmail.IsEnabled = true;
					inputPassword.IsEnabled = true;
					inputCode.IsEnabled = false;
					buttonNext.IsEnabled = true;
					buttonSend.IsEnabled = false;
					break;
				case CompanionAppService.State.NEEDS_CONFIRMATION:
					inputEmail.IsEnabled = false;
					inputPassword.IsEnabled = false;
					inputCode.IsEnabled = true;
					buttonNext.IsEnabled = true;
					buttonSend.IsEnabled = false;
					break;
				case CompanionAppService.State.READY:
					inputEmail.IsEnabled = false;
					inputPassword.IsEnabled = false;
					inputCode.IsEnabled = false;
					buttonNext.IsEnabled = false;
					buttonSend.IsEnabled = true;
					break;
			}
		}
        private void Window_Closing(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
