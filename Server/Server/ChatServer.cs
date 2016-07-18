using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;
using System.Linq;

namespace Server
{
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);
    public delegate void StatusChangesOnlineList(object sender, StatusChangedOnlineListArgs e);

    public class StatusChangedEventArgs : EventArgs
    {        
        private string EventMsg;
        
        public string EventMessage
        {
            get { return EventMsg; }
            set { EventMsg = value; }
        }

        public StatusChangedEventArgs(string strEventMsg)
        {
            EventMsg = strEventMsg;
        }
    }

    public class StatusChangedOnlineListArgs : EventArgs
    {
        private string OnlineList;

        public string OnlineUsersList
        {
            get { return OnlineList; }
            set { OnlineList = value; }
        }

        public StatusChangedOnlineListArgs(string strList)
        {
            OnlineList = strList;
        }
    }

    class Server
    {
        private IPAddress ipAddress;
        private TcpClient client;
        private Thread thrListener;
        private Thread thrСheck;
        private TcpListener tlsClient;
        public static Hashtable htUsers = new Hashtable(30);
        public static Hashtable htConnections = new Hashtable(30);
        private static Dictionary<string, TcpClient> dicUsers= new Dictionary<string, TcpClient>();        
        public static event StatusChangedEventHandler StatusChanged;
        public static event StatusChangesOnlineList ListChanged;
        private static StatusChangedEventArgs e;
        private static StatusChangedOnlineListArgs l;
        public static List<string> usersOnline = new List<string>();
        private Connection connect;
        bool ServRunning = false;   

        public Server(IPAddress address)
        {
            ipAddress = address;
        }

        public static void AddUser(TcpClient user, string userName)
        {
            dicUsers.Add(userName, user);
            htUsers.Add(userName, user);
            htConnections.Add(user, userName);
            usersOnline.Add(userName);
            SendNotification(htConnections[user] + " connected");
            SendListOnline();
        }

        public static void RemoveUser(TcpClient user)
        {
            if (htConnections[user] != null)
            {
                string name;
                name = htConnections[user].ToString();

                dicUsers.Remove(name);
                htUsers.Remove(htConnections[user]);
                htConnections.Remove(user);

                usersOnline.Remove(name);
                SendNotification(name + " disconnected");
                SendListOnline();
            }
        }

        public static void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChangedEventHandler statusHandler = StatusChanged;
            if (statusHandler != null)
            {                
                statusHandler(null, e);
            }
        }

        public static void OnListChanged(StatusChangedOnlineListArgs e)
        {
            StatusChangesOnlineList statusList = ListChanged;
            if (statusList != null)
            {
                statusList(null, e);
            }

        }

        public static void SendNotification(string message)
        {
            StreamWriter sender;
            TcpClient[] clients = new TcpClient[htUsers.Count];
            //TcpClient[] clients = new TcpClient[htConnections.Count];
            htUsers.Values.CopyTo(clients, 0);            

            e = new StatusChangedEventArgs("Notification: " + message);
            OnStatusChanged(e);

            for (int i = 0; i < clients.Length; i++)
            {
                try
                {
                    if (message.Trim() == "" || clients[i] == null)
                    {
                        continue;
                    }

                    sender = new StreamWriter(clients[i].GetStream());
                    sender.WriteLine("#sendAll");
                    sender.WriteLine(message);
                    sender.Flush();
                    sender = null;
                }

                catch
                {
                    RemoveUser(clients[i]);
                }                
            }
        }

        public static void SendMessage(string user, string message)
        {
            StreamWriter sender;
            TcpClient[] clients = new TcpClient[htUsers.Count];
            htUsers.Values.CopyTo(clients, 0);

            e = new StatusChangedEventArgs(user + " says: " + message);
            OnStatusChanged(e);

            for (int i = 0; i < clients.Length; i++)
            {
                if (message.Trim() == "" || clients[i] == null)
                {
                    continue;
                }

                try
                {
                    sender = new StreamWriter(clients[i].GetStream());
                    sender.WriteLine("#sendAll");
                    sender.WriteLine(user + ": " + message);
                    sender.Flush();
                    sender = null;
                }
                catch
                {
                    RemoveUser(clients[i]);
                }
               
            }
        }

        public static void SendListOnline()
        {
            StreamWriter sender;
            //TcpClient client = new TcpClient();
            //dicUsers.TryGetValue(user, out client);
            TcpClient[] clients = new TcpClient[htUsers.Count];
            htUsers.Values.CopyTo(clients, 0);
            string strOnlineList;

            strOnlineList = string.Join("\r\n", usersOnline.ToArray());
            l = new StatusChangedOnlineListArgs(strOnlineList);
            OnListChanged(l);

            for (int i = 0; i < clients.Length; i++)
            {
                sender = new StreamWriter(clients[i].GetStream());
                sender.WriteLine("#update_onlineList");
                sender.WriteLine(strOnlineList);
                sender.WriteLine("#end_update_onlineList");
                sender.Flush();
                sender = null;
            }           
        }

        public static void SendPrivateMessage(string from, string to, string message)
        {
            StreamWriter sender;
            TcpClient client = new TcpClient();
            
            dicUsers.TryGetValue(to, out client);
            message = to + ": " + message;
            sender = new StreamWriter(client.GetStream());
            sender.WriteLine("#input_private_message");
            sender.WriteLine(message);
            sender.Flush();
            sender = null;            
        }

        public void StartListening()
        {
            IPAddress ipLocal = ipAddress;
            tlsClient = new TcpListener(8888);

            ServRunning = true;
            tlsClient.Start();
            thrListener = new Thread(KeepListening);
            thrListener.Start();
            thrСheck = new Thread(UserCheck);
            thrСheck.Start();
        }

        public void KeepListening()
        {
            try
            {
                while (ServRunning == true)
                {
                    client = tlsClient.AcceptTcpClient();
                    connect = new Connection(client);
                }
            }
            
            catch
            {
                SendNotification("Conection lost");
            }
        }

        public void UserCheck()
        {
            StreamReader srCheck;
            while (ServRunning == true)
            {
                Thread.Sleep(60000);
                TcpClient[] clients = new TcpClient[htUsers.Count];
                
                htUsers.Values.CopyTo(clients, 0);

                for (int i = 0; i < clients.Length; i++)
                {
                    try
                    {
                        srCheck = new StreamReader(clients[i].GetStream());
                        srCheck = null;
                    }

                    catch
                    {
                        RemoveUser(clients[i]);
                    }
                }

                SendListOnline();
            }
        }
        
        public void StopListening()
        {
            SendNotification("Server stop");
            ServRunning = false;
            tlsClient.Stop();
            connect.CloseConnection();                
        }
    }

    class Connection
    {
        TcpClient tcpClient;
       
        private Thread thrSender;
        private StreamReader srReceiver;
        private StreamWriter swSender;
        private string currUser, toUser;
        private string strResponse;
              
        public Connection(TcpClient tcpCon)
        {
            tcpClient = tcpCon;
           
            thrSender = new Thread(AcceptClient);
         
            thrSender.Start();
        }

        public void CloseConnection()
        {            
            tcpClient.Close();
            srReceiver.Close();
            swSender.Close();
        }

        
        private void AcceptClient()
        {
            srReceiver = new StreamReader(tcpClient.GetStream());
            swSender = new StreamWriter(tcpClient.GetStream());

            
            currUser = srReceiver.ReadLine();

            
            if (currUser != "")
            {
                
                if (Server.htUsers.Contains(currUser) == true)
                {
                    
                    swSender.WriteLine("This user already exists.");
                    swSender.Flush();
                    CloseConnection();
                    return;
                }
                else if (currUser == "Administrator")
                {
                    
                    swSender.WriteLine("This user is reserved.");
                    swSender.Flush();
                    CloseConnection();
                    return;
                }
                else
                {             
                    Server.AddUser(tcpClient, currUser);
                }
            }
            else
            {
                CloseConnection();
                return;
            }

            try
            {
                while ((strResponse = srReceiver.ReadLine()) != "")
                {
                    if (strResponse == null && Server.usersOnline.Count >= 1)
                    {
                        Server.RemoveUser(tcpClient);
                    }
                    else
                    {
                        if (strResponse == "#output_private_message")
                        {
                            toUser = srReceiver.ReadLine();
                            strResponse = srReceiver.ReadLine();
                            Server.SendPrivateMessage(currUser, toUser, strResponse);
                        }
                        else
                        {
                            Server.SendMessage(currUser, strResponse);
                        }
                        
                    }

                }
            }
            catch
            {                
                Server.RemoveUser(tcpClient);
            }          
        }
    }
}