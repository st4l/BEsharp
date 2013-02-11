// ----------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

namespace BESharp.WinClient
{
    using System;
    using System.Security.Authentication;
    using System.Windows;
    using Datagrams;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool connected;
        private string host = "68.233.230.165";
        private int port = 2302;
        private RConClient rcc;


        public MainWindow()
        {
            this.InitializeComponent();
            this.UpdateUiStatus();
        }


        private async void ConnectClick(object sender, RoutedEventArgs e)
        {
            this.btnConnect.IsEnabled = false;
            if (this.connected)
            {
                this.rcc.MessageReceived -= this.OnRccOnMessageReceived;
                this.rcc.Disconnected -= this.RccOnDisconnected;
                this.rcc.Close();
                this.rcc = null;
                this.connected = false;
                this.UpdateUiStatus();
                this.btnConnect.IsEnabled = true;
                return;
            }

            this.rcc = new RConClient(this.host, this.port, "70e02f66");
            this.rcc.MessageReceived += this.OnRccOnMessageReceived;
            this.rcc.Disconnected += this.RccOnDisconnected;

            try
            {
                string msg = string.Format("Connecting to {0} on port {1}...", this.host, this.port);
                this.WriteLine(msg);

                this.connected = await this.rcc.ConnectAsync();
                this.WriteLine(
                    !this.connected ? "Could not connect to the specified remote host." : "Connected!");
            }
            catch (TimeoutException te)
            {
                this.WriteLine(te.Message);
            }
            catch (InvalidCredentialException te)
            {
                this.WriteLine(te.Message);
            }
            finally
            {
                if (!this.connected)
                {
                    this.rcc.MessageReceived -= this.OnRccOnMessageReceived;
                    this.rcc.Disconnected -= this.RccOnDisconnected;
                }
            }
            this.btnConnect.IsEnabled = true;
            this.UpdateUiStatus();
        }


        private void UpdateUiStatus()
        {
            if (this.connected)
            {
                this.btnConnect.Content = "Disconnect";
                this.txtCommand.IsEnabled = true;
                this.btnSendCommand.IsEnabled = true;
                this.txtCommand.Visibility = Visibility.Visible;
                this.btnSendCommand.Visibility = Visibility.Visible;
            }
            else
            {
                this.btnConnect.Content = "Connect";
                this.txtCommand.IsEnabled = false;
                this.btnSendCommand.IsEnabled = false;
                this.txtCommand.Visibility = Visibility.Hidden;
                this.btnSendCommand.Visibility = Visibility.Hidden;
            }
        }


        private void RccOnDisconnected(object sender, DisconnectedEventArgs disconnectedEventArgs)
        {
            this.WriteLine("Disconnected!");
            this.connected = false;
            this.UpdateUiStatus();
        }


        private void OnRccOnMessageReceived(object s, MessageReceivedEventArgs args)
        {
            this.WriteLine(args.MessageBody);
        }


        private void WriteLine(string text)
        {
            this.TxtLog.AppendText(text + "\r\n");
            this.TxtLog.ScrollToEnd();
        }


        private async void btnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtCommand.Text))
            {
                return;
            }
            this.WriteLine("> " + this.txtCommand.Text);

            ResponseHandler handler = this.rcc.SendCommand(this.txtCommand.Text);
            await handler.WaitForResponse();
            if (handler.ResponseDatagram != null)
            {
                var response = handler.ResponseDatagram as CommandResponseDatagram;
                if (response != null)
                {
                    this.WriteLine(response.Body);
                }
            }
        }
    }
}
