/// <summary>
/// Author:    Draeden Jensen and Derek Kober
/// Date:      03-28-2023
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500, Draeden Jensen - This work may not 
///            be copied for use in Academic Coursework.
///
/// Draeden and Derek certify that this code was written from scratch and
/// we did not copy it in part or whole from another source.  All 
/// references used in the completion of the assignments are cited 
/// in the README file.
///
/// File Contents:
/// Contains a definition for a Networking class. This class interacts with a "client code" (any
/// program that wants networking, including the Client and Server GUIs used in this project) via
/// a callback mechanism. This means that the "client code" will provide (via a delegate) a 
/// function, e.g., HandleMessage, that the Networking object will call anytime a full message 
/// comes across the network. 
/// </summary>

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Communications
{
    public class Networking
    {
        public delegate void ReportMessageArrived(Networking channel, string message);
        public delegate void ReportDisconnect(Networking channel);
        public delegate void ReportConnectionEstablished(Networking channel);

        private ReportMessageArrived onMessage;
        private ReportConnectionEstablished onConnect;
        private ReportDisconnect onDisconnect;

        private readonly ILogger logger;
        private char terminationCharacter;

        private TcpClient client;
        private CancellationTokenSource waitForCancellation;

        /// <summary>
        /// Gets or sets the name of the client. The default ID is the TCP Client's RemoteEndPoint, 
        /// but could be changed to something like "Jim".
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Creates a new Networking object and initializes its member fields.
        /// </summary>
        /// <param name="logger">Logger provided by dependency injection</param>
        /// <param name="onConnect">Delegate callback which defines handling new connections</param>
        /// <param name="onDisconnect">Delegate callback which defines handling disconnections</param>
        /// <param name="onMessage">Delegate callback which defines handling message sending/receiving</param>
        /// <param name="terminationCharacter">Character which defines the end of a message</param>
        public Networking(ILogger? logger, ReportConnectionEstablished onConnect,
        ReportDisconnect onDisconnect, ReportMessageArrived onMessage, char terminationCharacter)
        {
            this.logger = logger;
            this.onMessage = onMessage;
            this.onConnect = onConnect;
            this.onDisconnect = onDisconnect;
            this.terminationCharacter = terminationCharacter;
        }

        /// <summary>
        /// Creates (and stores) a TCP Client object connected to the given host/port.
        /// Throws a SocketException if the host/port is not available.
        /// </summary>
        /// <param name="host">Host name to connect to.</param>
        /// <param name="port">Port number to connect to.</param>
        /// <exception cref="SocketException">
        public void Connect(string host, int port)
        {
            try
            {
                client = new TcpClient(host, port);
            }
            catch (SocketException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Awaits for data to come in over the TCP Client. When data is received, it checks for
        /// messages. Calls the onDisconnect callback method if the TCP Client disconnects while
        /// reading data.
        /// </summary>
        /// <param name="infinite">Defines whether this loop is run infinitely or just once</param>
        public async void AwaitMessagesAsync(bool infinite = true)
        {
            try
            {
                StringBuilder dataBacklog = new StringBuilder();
                byte[] buffer = new byte[4096];
                NetworkStream stream = client.GetStream();

                if (stream == null)
                {
                    return;
                }

                do
                {
                    int total = await stream.ReadAsync(buffer);

                    if (total == 0)
                    {
                        throw new Exception("End of stream reached. Connection must be closed.");
                    }

                    string current_data = Encoding.UTF8.GetString(buffer, 0, total);

                    dataBacklog.Append(current_data);

                    this.CheckForMessage(dataBacklog);
                }
                while (infinite);
            }
            catch (Exception ex)
            {
                onDisconnect(this);
            }
        }

        /// <summary>
        /// Checks if data received from the TCP Client contains messages. If at least one message
        /// was received, calls the OnMessage callback method.
        /// </summary>
        /// <param name="data">Data received from the TCP Client</param>
        private void CheckForMessage(StringBuilder data)
        {
            string allData = data.ToString();
            int terminator_position = allData.IndexOf(terminationCharacter);

            while (terminator_position >= 0)
            {
                string message = allData.Substring(0, terminator_position + 1);
                data.Remove(0, terminator_position + 1);
                onMessage(this, message);

                allData = data.ToString();
                terminator_position = allData.IndexOf(terminationCharacter);
            }
        }

        /// <summary>
        /// Continuously waits for clients to connect, then, upon receiving a connection, builds
        /// a new Networking object to handle this client's connection.
        /// </summary>
        /// <param name="port">Port number</param>
        /// <param name="infinite">Defines whether this loop is run infinitely or just once</param>
        public async void WaitForClients(int port, bool infinite)
        {
            TcpListener network_listener = new(IPAddress.Any, port);
            network_listener.Start();
            waitForCancellation = new();

            try
            {
                do
                {
                    TcpClient connection = await network_listener.AcceptTcpClientAsync(waitForCancellation.Token);

                    Networking newConnection = new(logger, onConnect, onDisconnect, onMessage, terminationCharacter);
                    newConnection.client = connection;

                    onConnect(newConnection);
                }
                while (infinite);
            }
            catch (Exception ex)
            {
                network_listener.Stop();
            }
        }

        /// <summary>
        /// Cancels the WaitForClients method.
        /// </summary>
        public void StopWaitingForClients()
        {
            waitForCancellation.Cancel();
        }

        /// <summary>
        /// Closes the connection to the remote host.
        /// </summary>
        public void Disconnect()
        {
            client.Close();
        }

        /// <summary>
        /// Sends the text across the network using the TCP Client.
        /// </summary>
        /// <param name="text"></param>
        public async void Send(string text)
        {
            if (text[text.Length - 1] != terminationCharacter)
            {
                text += terminationCharacter;
            }

            byte[] messageBytes = Encoding.UTF8.GetBytes(text);

            try
            {
                await client.GetStream().WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                onDisconnect(this);
            }
        }
    }
}