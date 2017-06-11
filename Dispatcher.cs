using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using EdGo.EdgoClient;
using EddiCompanionAppService;
namespace EdGo
{
	class AppDispatcher
	{

		/*
		 * Tasks 
		 */
		private Thread resetProcessor;
		private Thread lastFileProcessor;
		private Thread scanDirectoryProcessor;
		private Thread readFileProcessor;

		public static AppDispatcher instance { get; } = new AppDispatcher();

		private ClientWindow settingsClient = null;
		public MainWindow mWin { get; set; } = null;
		//public ClientWindow cWin { get; set; } = null;
		public Companian cWin { get; set; } = null;

		private Client client = Client.instance;
		private bool started { get; set; } = false;

		private FileProcessor fileProc = new FileProcessor();

		private TextLogger logger = TextLogger.instance;


		private IDictionary<String, bool> pilotNames = new Dictionary<String, bool>();

		public CompanionAppService companionService { get; } = CompanionAppService.Instance;

		public AppDispatcher()
		{
			fileProc.load();
		}

		public void saveProperties()
		{
			Properties.Settings.Default.Save();
		}


		protected void setPilotNames(String names)
		{
			if (names.Trim().Length > 0)
			{
				Regex reg = new Regex("\\|\\|\\|\\|\\|", RegexOptions.IgnoreCase | RegexOptions.Compiled);
				//Console.WriteLine(names);
				String[] namesArr = reg.Split(names);
				//Console.WriteLine(namesArr.Length);
				//pilotNames = new Dictionary<String, bool>();
				foreach (String name in namesArr)
				{
					pilotNames[name.ToLower()] = false;
				}
			}
		}

		public void newPilotDialog(String name)
		{
			//killAllTasks();
			if (mWin.showNewPilotDialog(name))
			{
				pilotNames[name.ToLower()] = true;
			}
			else
			{
				pilotNames[name.ToLower()] = false;
			}
			//started = true;
			//process();
		}
		public int isNewPilot(String name)
		{
			int result = -1;
			if (!pilotNames.ContainsKey(name.ToLower()))
			{
				if (pilotNames.Count == 0)
				{
					pilotNames[name.ToLower()] = false;
				}
				else
				{ 
					newPilotDialog(name);
				}
			}
			result = pilotNames[name.ToLower()] ? 1 : 0;
			return result;
		}
		public void process()
		{
			if (client.isTested)
			{
				// Normal process
				if (started)
				{
					logger.log("Process......");
					mWin.setStartStopState(false);
					//Thread.Sleep(500);
					logger.log("Load last Info");
					IDictionary<string, string> startInfo = client.getLastInfo();
					if (startInfo == null)
					{
						logger.log("Error cnnectiong to Server!!!!");
						mWin.showStartButton();
						started = false;
					}
					else
					{
						if (startInfo.ContainsKey("timestamp"))
						{
							//setPilotNames(startInfo["used_names"]);
							fileProc.setReset(startInfo["timestamp"], startInfo["event_name"], startInfo["event_hash"]);
						} else
						{
							fileProc.setReset();
						}
						resetProcessor = new Thread(new ThreadStart(fileProc.resetTo));
						resetProcessor.Start();
					}
				}
				mWin.setStartStopState(true);
			} else
			{
				mWin.setStartStopState(false);
			}
		}

		public void pressStart()
		{
			started = !started;
			if (started)
			{
                mWin.trayIcon.Icon = Properties.Resources.started;
                mWin.showStopButton();
				process();
			}
			else
			{
                mWin.trayIcon.Icon = Properties.Resources.stoped;
                killAllTasks();
				mWin.showStartButton();
			}
		}
		public void showClientSettings()
		{
			if (settingsClient == null)
			{
				settingsClient = new ClientWindow();
			}
			settingsClient.ShowDialog();
		}

		public void hideClientSettings()
		{
			if (client.isTested)
			{
				started = false;
				mWin.setStartStopState(false);
			}
			process();
		}

		// processes
		public void endResetProcess()
		{
			saveProperties();
			logger.log("Finish reset");
			logger.log("Start read to last");
			lastFileProcessor = new Thread(new ThreadStart(fileProc.processToLast));
			lastFileProcessor.Start();
		}

		public void endReadToLastProcess()
		{
			saveProperties();
			logger.log("Finish read to last process");
			logger.log("Start scan new events");
			scanDirectoryProcessor = new Thread(new ThreadStart(fileProc.directoryRead));
			readFileProcessor = new Thread(new ThreadStart(fileProc.processCurrentFile));
			scanDirectoryProcessor.Start();
			readFileProcessor.Start();
		}

		public void responceError()
		{
			logger.log("ERROR IN REQUEST");
			logger.log("COMMAND: ");
			logger.log(client.lastCommand);
			logger.log("RESPONCE: ");
			logger.log(client.lastResponce);
			killAllTasks();
		}
		public void killAllTasks()
		{
			if (resetProcessor != null && resetProcessor.IsAlive)
			{
				resetProcessor.Interrupt();
			}
			if (lastFileProcessor != null && lastFileProcessor.IsAlive)
			{
				lastFileProcessor.Interrupt();
			}
			if (scanDirectoryProcessor != null && scanDirectoryProcessor.IsAlive)
			{
				scanDirectoryProcessor.Interrupt();
			}
			if (readFileProcessor != null && readFileProcessor.IsAlive)
			{
				readFileProcessor.Interrupt();
			}
		}

		/*
		 * Companian 
		 */
		public void showCompanionWindow()
		{
			if (cWin == null)
			{
				cWin = new Companian();
			}
			cWin.setState(companionService.CurrentState);
			cWin.ShowDialog();
		}

		public void companionNext()
		{
			switch (companionService.CurrentState)
			{
				case CompanionAppService.State.NEEDS_LOGIN:
					cWin.disableAll();
                    try
                    {
                        companionService.Login();
                        companionService.Credentials.Save();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "Companion API", MessageBoxButton.OK);
                    }
					cWin.setState(companionService.CurrentState);
					break;
				case CompanionAppService.State.NEEDS_CONFIRMATION:
					cWin.disableAll();
                    try
                    {
                        companionService.Confirm();
                        companionService.Credentials.Save();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "Companion API", MessageBoxButton.OK);
                    }
                    cWin.setState(companionService.CurrentState);
					break;
				case CompanionAppService.State.READY:
					cWin.disableAll();
					companionService.Profile();
					sendCompanianInfo();
					cWin.setState(companionService.CurrentState);
                    cWin.Hide();
                    break;
				default:
					cWin.setState(companionService.CurrentState);
					break;
			}
		}

		public void sendCompanianInfo()
		{
			if (companionService.CurrentState == CompanionAppService.State.READY && !string.IsNullOrEmpty(companionService.Content))
			{
				string data = "{ \"timestamp\":\"2001-01-01T00:00:00Z\", \"event\":\"CompanionApi\", \"CompanionData\": " + companionService.Content + " }";
				client.sendEvent(data);
			}
		}
	}
}
