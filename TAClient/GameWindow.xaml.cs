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
using TAGame;

namespace TAClient
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private Connection connection;
        private Synchronizer synchronizer;
        private MainWindow mainWindow;
        private MessageParser parser;
        private object receiveLock;

        public GameWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes all required fields in GameWindow class. Cannot be done using constructor beacuse of cyclic dependencies.
        /// </summary>
        /// <remarks>Must be called before opening the window!</remarks>
        /// <param name="connection">Connection which is used for the server communication.</param>
        /// <param name="mainWindow">Main window from which this window is opened.</param>
        /// <param name="receiveLock">Lock for closing the app and receiving the message.</param>
        public void Init(Connection connection, MainWindow mainWindow, object receiveLock)
        {
            this.mainWindow = mainWindow;

            this.connection = connection;

            this.synchronizer = new Synchronizer(connection, this);

            this.parser = new MessageParser(synchronizer);

            this.receiveLock = receiveLock;

        }

        /// <summary>
        /// Prints given text to a game log
        /// </summary>
        /// <param name="text">Text to be logged</param>
        public void Log(string text)
        {
            if (GameLog.Dispatcher.CheckAccess())
            {
                LogInner(text);
            }
            else
            {
                GameLog.Dispatcher.BeginInvoke(new Action(() => LogInner(text)));
            }
        }

        private void LogInner(string text)
        {
            string formatedText = String.Format("[{0:HH:mm:ss}]: {1}", DateTime.Now, text);
            if (GameLog.Text == "")
            {
                GameLog.Text = formatedText;
                return;
            }
            GameLog.Text += System.Environment.NewLine + formatedText;
            GameLog.ScrollToEnd();
        }

        /// <summary>
        /// Sets given text as a status text in the GameWindow
        /// </summary>
        /// <param name="text">Text to be shown</param>
        ///  <param name="highlight">If true, the status box is highlighted.</param>
        public void SetStatusText(string text, bool highlight)
        {
            if (InfoArea.Dispatcher.CheckAccess())
            {
                if (highlight)
                {
                    InfoArea.Foreground = Brushes.MintCream;
                    InfoBox.Background = Brushes.Green;
                    InfoArea.Text = text;
                }
                else 
                {
                    InfoArea.Foreground = Brushes.Green;
                    InfoBox.Background = Brushes.MintCream;
                    InfoArea.Text = text;
                }
            }
            else
            {
                InfoArea.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (highlight)
                    {
                        InfoArea.Foreground = Brushes.MintCream;
                        InfoBox.Background = Brushes.Green;
                        InfoArea.Text = text;
                    }
                    else
                    {
                        InfoArea.Foreground = Brushes.Green;
                        InfoBox.Background = Brushes.MintCream;
                        InfoArea.Text = text;
                    }
                }));
            }
        }

        /// <summary>
        /// Prints given text as a received message in the chat
        /// </summary>
        /// <param name="text">Text to be printed</param>
        public void PrintMessage(string text)
        {
            if (ChatBox.Dispatcher.CheckAccess())
            {
                if (ChatBox.Text == "")
                {
                    ChatBox.Text = text;
                    ChatBox.ScrollToEnd();
                }
                else
                {
                    ChatBox.Text += System.Environment.NewLine + text;
                    ChatBox.ScrollToEnd();
                }
            }
            else
            {
                ChatBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ChatBox.Text == "")
                    {
                        ChatBox.Text = text;
                        ChatBox.ScrollToEnd();

                    }
                    else
                    {
                        ChatBox.Text += System.Environment.NewLine + text;
                        ChatBox.ScrollToEnd();

                    }
                }));
            }

        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            // We resume the parent MainWindow
            mainWindow.Show();
            mainWindow.ResumeWindow();
        }


        private void Window_Loaded_2(object sender, RoutedEventArgs e)
        {
            // We can start receiving after the window has been loaded -> the first command received might require window to close
            connection.Receive(parser);

        }

        private void Window_GotFocus_1(object sender, RoutedEventArgs e)
        {

        }

        private void ReadyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ReadyCheckBox.IsEnabled = false;
            synchronizer.LocalReady();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Attempts to send the message
            string message = MessageField.Text;
            if (message != "")
            {
                synchronizer.SendMessage(message);
            }
            MessageField.Text = "";
            MessageField.Focus();
        }

        private void MessageField_KeyDown(object sender, KeyEventArgs e)
        {
            // If enter is pressed, attempts to send the message
            if (e.Key != Key.Enter) return;
            string message = MessageField.Text;
            if (message != "")
            {
                synchronizer.SendMessage(message);
            }
            MessageField.Text = "";
            MessageField.Focus();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Window cannot close while receiveLock is locked
            lock (receiveLock)
            {
                if (connection != null)
                {
                    connection.ForceDisconnect(false);
                }
            }
        }
    }
}
