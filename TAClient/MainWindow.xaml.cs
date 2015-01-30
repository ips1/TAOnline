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
using TAGame;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace TAClient
{
    enum WindowStatus { DISCONNECTED, CONNECTED }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            receiveLock = new object();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(IPBox);
            currentStatus = WindowStatus.DISCONNECTED;
        }

        private Connection assignedConnection;
        private string insertedIP;
        private WindowStatus currentStatus;
        private object receiveLock;

        /// <summary>
        /// Enables user input on the connection form (except the Quit button)
        /// </summary>
        private void EnableInput()
        {
            currentStatus = WindowStatus.DISCONNECTED;
            ConnectButton.IsEnabled = true;
            IPBox.IsEnabled = true;
            NicknameBox.IsEnabled = true;
        }

        /// <summary>
        /// Disables user input on the connection form (except the Quit button)
        /// </summary>
        private void DisableInput()
        {
            currentStatus = WindowStatus.CONNECTED;
            ConnectButton.IsEnabled = false;
            IPBox.IsEnabled = false;
            NicknameBox.IsEnabled = false;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (NicknameBox.Text.Length > 20)
            {
                ShowStatusError("Your nick is too long!");
                return;
            }

            // Disables input and then attempts connection, if it fails, enables input again
            DisableInput();
            if (!AttemptConnection()) EnableInput();
        }

        /// <summary>
        /// Attempts to connect to the server
        /// </summary>
        /// <returns>True if the connection was succesfull, false otherwise</returns>
        private bool AttemptConnection()
        {
            if (IPBox.Text == "")
            {
                ShowStatusError("IP Address must be entered!");
                return false;
            }
            if (NicknameBox.Text == "")
            {
                ShowStatusError("Nickname must be entered!");
                return false;
            }
            if (!IsCorrectName(NicknameBox.Text))
            {
                ShowStatusError("Ivalid nickname (can't contain space)!");
                return false;

            }

            IPAddress ip;
            try
            {
                ip = IPAddress.Parse(IPBox.Text);
            }
            catch (FormatException)
            {
                ShowStatusError("Invalid IP format!");
                return false;
            }

            ShowStatusProgress("Connecting to " + IPBox.Text + "...");
            assignedConnection = new Connection(ip, NicknameBox.Text, receiveLock);
            insertedIP = IPBox.Text;
            // The connection itself is performed in different thread in order for the GUI to be available
            Thread connectionThread = new Thread(StartConnection);
            connectionThread.IsBackground = true;
            connectionThread.Start();
            return true;
        }

        /// <summary>
        /// Starting method for the connection thread
        /// </summary>
        private void StartConnection()
        {
            try
            {
                assignedConnection.Connect();
            }
            catch (ConnectionError err)
            {
                ShowStatusError("Connection error: " + err.Message);
                ConnectButton.Dispatcher.BeginInvoke(new Action(() => EnableInput()));
                return;
            }

            ShowStatusSuccess("Succesfully connected to " + insertedIP + "...");
            ShowGameWindow();

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Shows error message in the status area of the window (in red field)
        /// </summary>
        /// <param name="msg">Message to be shown</param>
        private void ShowStatusError(string msg)
        {
            if (StatusPanel.Dispatcher.CheckAccess())
            {
                StatusPanel.Background = Brushes.Red;
                StatusText.Text = msg;
            }
            else
            {
                StatusPanel.Dispatcher.BeginInvoke(new Action(() => { this.StatusPanel.Background = Brushes.Red; this.StatusText.Text = msg; }));
            }

        }

        /// <summary>
        /// Shows message reporting success in the status area of the window (in green field)
        /// </summary>
        /// <param name="msg">Message to be shown</param>
        private void ShowStatusSuccess(string msg)
        {
            if (StatusPanel.Dispatcher.CheckAccess())
            {
                StatusPanel.Background = Brushes.LightGreen;
                StatusText.Text = msg;
            }
            else
            {
                StatusPanel.Dispatcher.BeginInvoke(new Action(() => { this.StatusPanel.Background = Brushes.LightGreen; this.StatusText.Text = msg; }));
            }
        }

        /// <summary>
        /// Shows message reporting progress in the status area of the window (in yellow field)
        /// </summary>
        /// <param name="msg">Message to be shown</param>
        private void ShowStatusProgress(string msg)
        {
            if (StatusPanel.Dispatcher.CheckAccess())
            {
                StatusPanel.Background = Brushes.LightYellow;
                StatusText.Text = msg;
            }
            else
            {
                StatusPanel.Dispatcher.BeginInvoke(new Action(() => { this.StatusPanel.Background = Brushes.LightYellow; this.StatusText.Text = msg; }));
            }
        }

        /// <summary>
        /// Opens new GameWindow
        /// </summary>
        /// <remarks>Must be called after connection is succesfully established!</remarks>
        private void ShowGameWindow()
        {
            if (this.Dispatcher.CheckAccess())
            {
                GameWindow window = new GameWindow();
                window.Init(assignedConnection, this, receiveLock);
                window.Show();
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GameWindow window = new GameWindow();
                    window.Init(assignedConnection, this, receiveLock);
                    window.Show(); 
                }));
            }
        }

        /// <summary>
        /// Resumes the window to its default state (while closing assigned connection)
        /// </summary>
        public void ResumeWindow()
        {
            if (currentStatus == WindowStatus.CONNECTED)
            {
                EnableInput();
                assignedConnection.ForceDisconnect(false);
                assignedConnection = null;
            }
            ShowStatusSuccess("Ready to connect!");
        }

        /// <summary>
        /// Checks whether a string represents correct name
        /// </summary>
        /// <param name="name">Name to be checked</param>
        /// <returns>True if the name is correct</returns>
        private bool IsCorrectName(string name)
        {
            if (name.Contains(' ')) return false;
            else return true;
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            Application.Current.Shutdown();

        }

        private void TextBlock_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            lock (receiveLock)
            {
                if (assignedConnection != null)
                {
                    assignedConnection.ForceDisconnect(false);
                }
            }
        }
        



    }
}
