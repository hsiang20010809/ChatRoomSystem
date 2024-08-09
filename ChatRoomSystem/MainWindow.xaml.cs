using System;
using System.Windows;
using System.Net.Sockets;

namespace ChatRoomSystem
{
    public partial class MainWindow : Window
    {
        private Server server;
        private Client client1;
        private Client client2;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            server = new Server();
            client1 = new Client();
            client2 = new Client();

            server.MessageReceived += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(ServerLogTextBox, msg));
            server.ClientConnected += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(ServerLogTextBox, msg));
            server.ClientDisconnected += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(ServerLogTextBox, msg));

            client1.MessageReceived += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client1ChatTextBox, msg));
            client1.MessageSent += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client1ChatTextBox, $"You sent: {msg}")); // 新增
            client1.Connected += () => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client1ChatTextBox, "Connected to server."));
            client1.Disconnected += () => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client1ChatTextBox, "Disconnected from server."));

            client2.MessageReceived += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client2ChatTextBox, msg));
            client2.MessageSent += (msg) => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client2ChatTextBox, $"You sent: {msg}")); // 新增
            client2.Connected += () => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client2ChatTextBox, "Connected to server."));
            client2.Disconnected += () => Dispatcher.Invoke(() => AppendTextWithTimestamp(Client2ChatTextBox, "Disconnected from server."));
        }

        private void AppendTextWithTimestamp(System.Windows.Controls.TextBox textBox, string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            textBox.AppendText(timestampedMessage + Environment.NewLine);
            textBox.ScrollToEnd();
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int port = int.Parse(ServerPortTextBox.Text);
                server.Start(port);
                AppendTextWithTimestamp(ServerLogTextBox, $"Server started on port {port}");
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid port number. Please enter a valid integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            server.Stop();
            AppendTextWithTimestamp(ServerLogTextBox, "Server stopped");
            StartServerButton.IsEnabled = true;
            StopServerButton.IsEnabled = false;
        }

        private void ConnectClient(Client client, string ipTextBox, string portTextBox, string usernameTextBox, string chatTextBox)
        {
            try
            {
                string ip = this.FindName(ipTextBox) as System.Windows.Controls.TextBox != null ? (this.FindName(ipTextBox) as System.Windows.Controls.TextBox).Text : "";
                int port = int.Parse((this.FindName(portTextBox) as System.Windows.Controls.TextBox).Text);
                string username = (this.FindName(usernameTextBox) as System.Windows.Controls.TextBox).Text;

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Please enter a username.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                client.Connect(ip, port, username);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid port number. Please enter a valid integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SocketException se)
            {
                MessageBox.Show($"Connection error: {se.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectClient1Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectClient(client1, "Client1IpTextBox", "Client1PortTextBox", "Client1UsernameTextBox", "Client1ChatTextBox");
        }

        private void DisconnectClient1Button_Click(object sender, RoutedEventArgs e)
        {
            client1.Disconnect();
        }

        private void SendClient1Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(client1, Client1MessageTextBox);
        }

        private void ConnectClient2Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectClient(client2, "Client2IpTextBox", "Client2PortTextBox", "Client2UsernameTextBox", "Client2ChatTextBox");
        }

        private void DisconnectClient2Button_Click(object sender, RoutedEventArgs e)
        {
            client2.Disconnect();
        }

        private void SendClient2Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(client2, Client2MessageTextBox);
        }

        private void SendMessage(Client client, System.Windows.Controls.TextBox messageTextBox)
        {
            string message = messageTextBox.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                client.SendMessage(message);
                messageTextBox.Clear(); // 清除輸入框
            }
            else
            {
                MessageBox.Show("Please enter a message to send.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
