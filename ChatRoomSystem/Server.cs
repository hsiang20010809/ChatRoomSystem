using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Server
{
    private TcpListener listener;
    private List<TcpClient> clients = new List<TcpClient>();
    private Dictionary<TcpClient, string> clientUsernames = new Dictionary<TcpClient, string>();
    private bool isRunning;
    private Thread listenThread;

    public event Action<string> MessageReceived;
    public event Action<string> ClientConnected;
    public event Action<string> ClientDisconnected;

    public void Start(int port)
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;
            listenThread = new Thread(ListenForClients);
            listenThread.Start();
            Console.WriteLine($"Server started on port {port}");
        }
        catch (SocketException se)
        {
            Console.WriteLine($"SocketException: {se.Message}");
            // 可能是端口已被占用
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error starting server: {e.Message}");
        }
    }

    public void Stop()
    {
        try
        {
            isRunning = false;
            listener?.Stop();
            foreach (var client in clients)
            {
                client.Close();
            }
            clients.Clear();
            clientUsernames.Clear();
            Console.WriteLine("Server stopped");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error stopping server: {e.Message}");
        }
    }

    private void ListenForClients()
    {
        while (isRunning)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);
                Thread clientThread = new Thread(HandleClientComm);
                clientThread.Start(client);
            }
            catch (SocketException se)
            {
                if (!isRunning) // 正常停止服務器時可能會拋出異常
                    break;
                Console.WriteLine($"SocketException: {se.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error accepting client: {e.Message}");
            }
        }
    }

    private void HandleClientComm(object clientObj)
    {
        TcpClient tcpClient = (TcpClient)clientObj;
        NetworkStream clientStream = tcpClient.GetStream();

        byte[] message = new byte[4096];
        int bytesRead;

        try
        {
            while (isRunning)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch (IOException)
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
                Message msg = Message.FromJson(receivedMessage);

                if (!clientUsernames.ContainsKey(tcpClient))
                {
                    clientUsernames[tcpClient] = msg.Username;
                    if (msg.Content == "__CONNECTED__")
                    {
                        ClientConnected?.Invoke($"{msg.Username} has connected to the server.");
                        continue; // 跳過廣播這個特殊消息
                    }
                    else
                    {
                        ClientConnected?.Invoke($"{msg.Username} ({((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address}) has joined the chat.");
                    }
                }

                MessageReceived?.Invoke($"{msg.Username}: {msg.Content}");
                BroadcastMessage(receivedMessage, tcpClient);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error handling client communication: {e.Message}");
        }
        finally
        {
            string username = clientUsernames.ContainsKey(tcpClient) ? clientUsernames[tcpClient] : "Unknown";
            clients.Remove(tcpClient);
            clientUsernames.Remove(tcpClient);
            tcpClient.Close();
            ClientDisconnected?.Invoke($"{username} has left the chat.");
        }
    }

    public void BroadcastMessage(string message, TcpClient excludeClient = null)
    {
        foreach (TcpClient client in clients)
        {
            if (client != excludeClient && client.Connected)
            {
                try
                {
                    NetworkStream clientStream = client.GetStream();
                    byte[] broadcastBytes = Encoding.UTF8.GetBytes(message);
                    clientStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                    clientStream.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error broadcasting message to a client: {e.Message}");
                    // 考慮在這裡移除斷開連接的客戶端
                }
            }
        }
    }
}
