using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Server
{
    public partial class Form1 : Form
    {
        private delegate void UpdateStatusCallback(string message);
        private delegate void UpdateOnlineList(string list);
        private Server server;

        public Form1()
        {
            InitializeComponent();
            this.Text = "ChatServer";
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            server = new Server(ipAddr);
            Server.StatusChanged += new StatusChangedEventHandler(server_StatusChanged);
            Server.ListChanged += new StatusChangesOnlineList(onlineList_StatusChanged);
            server.StartListening();
            tbChat.AppendText("Monitoring for connections...\r\n");
        }

        public void server_StatusChanged(object sender, StatusChangedEventArgs e)
        {        
            this.Invoke(new UpdateStatusCallback(this.UpdateStatus), new object[] { e.EventMessage });
        }

        public void onlineList_StatusChanged(object sender, StatusChangedOnlineListArgs e)
        {
            this.Invoke(new UpdateOnlineList(this.UpdateList), new object[] { e.OnlineUsersList });
        }

        private void UpdateStatus(string message)
        {            
            tbChat.AppendText(message + "\r\n");
        }

        private void UpdateList(string list)
        {
            tbOnlineList.Text = list;
        }

        private void stopServerButton_Click(object sender, EventArgs e)
        {
            server.StopListening();
        }
    }
}
