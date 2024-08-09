using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Client
{
    private TcpClient tcpClient;
    private NetworkStream clientStream;
    private string username;
    private bool isConnected;
    private Thread receiveThread;
    public event Action<string> MessageSent;
    public event Action<string> MessageReceived;
    public event Action Connected;
    public event Action Disconnected;

    public void Connect(string ip, int port, string username)
    {
        try
        {
            this.username = username;
            tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);
            clientStream = tcpClient.GetStream();
            isConnected = true;
            Connected?.Invoke();
            SendConnectionMessage();
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
        }
        catch (SocketException se)
        {
            Console.WriteLine($"SocketException: {se.Message}");
            // 可能是服務器未啟動或網絡問題
            throw; // 重新拋出異常，讓調用者知道連接失敗
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error connecting to server: {e.Message}");
            throw; // 重新拋出異常，讓調用者知道連接失敗
        }
    }

    private void SendConnectionMessage()
    {
        Message connectionMsg = new Message(username, "__CONNECTED__");
        byte[] buffer = Encoding.UTF8.GetBytes(connectionMsg.ToJson());
        clientStream.Write(buffer, 0, buffer.Length);
        clientStream.Flush();
    }

    public void Disconnect()
    {
        try
        {
            isConnected = false;
            tcpClient?.Close();
            receiveThread?.Join(1000); // 等待接收線程結束
            Disconnected?.Invoke();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error disconnecting: {e.Message}");
        }
        finally
        {
            tcpClient = null;
            clientStream = null;
        }
    }

    public void SendMessage(string content)
    {
        if (!isConnected)
        {
            Console.WriteLine("Not connected to server.");
            return;
        }

        try
        {
            Message msg = new Message(username, content);
            byte[] buffer = Encoding.UTF8.GetBytes(msg.ToJson());
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            // 修改：觸發 MessageSent 事件，而不是 MessageReceived
            MessageSent?.Invoke(content);
        }
        catch (IOException)
        {
            Console.WriteLine("Lost connection to server.");
            HandleDisconnect();
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("Connection is closed.");
            HandleDisconnect();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending message: {e.Message}");
        }
    }

    private void ReceiveMessages()
    {
        byte[] message = new byte[4096];
        int bytesRead;

        while (isConnected)
        {
            bytesRead = 0;

            try
            {
                bytesRead = clientStream.Read(message, 0, 4096);

                if (bytesRead == 0)
                {
                    // 服務器關閉了連接
                    break;
                }

                string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
                Message msg = Message.FromJson(receivedMessage);
                MessageReceived?.Invoke($"{msg.Username}: {msg.Content}");
            }
            catch (IOException)
            {
                Console.WriteLine("Lost connection to server.");
                break;
            }
            catch (ObjectDisposedException)
            {
                // 連接已經被關閉，這是正常的斷開連接行為
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error receiving message: {e.Message}");
                break;
            }
        }

        HandleDisconnect();
    }

    private void HandleDisconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            Disconnect();
        }
    }
}
