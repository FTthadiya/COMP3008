using ChatServiceInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DataLib;
using Message = DataLib.Message;
using System.Threading;
using Microsoft.Win32;

namespace LobbyClient
{

    public partial class LobbyWindow : Window
    {
        private LobbyServerInterface _lobbyServer;
        private Dictionary<string,LobbyRoom> _lobbies = new Dictionary<string,LobbyRoom>();
        private List<Message> messages = new List<Message>();
        private List<string> users = new List<string>();
        private string selectedLobby = null;
        private string selectedUser = null;
        private int msgCount = 0;
        private string curUser;
        private CancellationTokenSource _messagesCts;
        private CancellationTokenSource _lobbiesCts;
        private CancellationTokenSource _usersCts;

        public LobbyWindow(string username)
        {
            InitializeComponent();

            var tcp = new NetTcpBinding();
            var instanceContext = new InstanceContext(this);

            var URL = "net.tcp://localhost:8000/LobbyService";
            var channelFactory = new ChannelFactory<LobbyServerInterface>(tcp, URL);
            _lobbyServer = channelFactory.CreateChannel();

            ConnectToServer(username);

            
        }

        private async void ConnectToServer(string username)
        {
            try
            {
                bool result = await Task.Run(() => _lobbyServer.Connect(username));

                if (result)
                {
                    MessageBox.Show("Connected");
                    curUser = username;
                    Dispatcher.Invoke(() => UsernameLabel.Content = "User: "+username);
                    this.Show();

                    _lobbiesCts = new CancellationTokenSource();
                    await Task.Run(() => pullLobbies(_lobbiesCts.Token));
                }
                else
                {
                    MessageBox.Show("Username already exists");
                    ShowLoginWindow();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                
            }
        
        }

        private void ShowLoginWindow()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow.Show();
            this.Close();
        }

        

        private async void CreateLobbyBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lobbyName = LobbyNameTextBox.Text;
                bool result = await Task.Run(() => _lobbyServer.CreateRoom(lobbyName));
                if (result)
                {
                    LobbyNameTextBox.Text = "";
                    MessageBox.Show("Lobby created");
                    
                }
                else
                {
                    MessageBox.Show("Error occured");
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }


        private async void LobbyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedLobby != null)
            {
                await Task.Run(() => _lobbyServer.LeaveLobbyRoom(curUser, selectedLobby));
                messages.Clear();
                users.Clear();
                _lobbies[selectedLobby].lastMsgId = 0;
                _lobbies[selectedLobby].msgCount = 0;
            }

            if (LobbyList.SelectedItem == null)
            {
                return;
            }

            selectedLobby = LobbyList.SelectedItem.ToString();

            Dispatcher.Invoke(() => ChatRichTextBox.Document.Blocks.Clear());
            msgCount = 0;


            if (_messagesCts != null)
            {
                _messagesCts.Cancel();
            }

            if (_usersCts != null)
            {
                _usersCts.Cancel();
            }

            _messagesCts = new CancellationTokenSource();
            _usersCts = new CancellationTokenSource();
            await Task.Run(() => _lobbyServer.JoinLobbyRoom(curUser, selectedLobby));
            await Task.Run(() => pullMessages(_messagesCts.Token));
            await Task.Run(() => pullUsers(_usersCts.Token));
            
            ChatTitle.Content = selectedLobby;
            ChatWindowGrid.Visibility = Visibility.Visible;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLobby != null)
            {
            string message = MessageTextBox.Text;
                if (message.Equals(""))
                {
                    MessageBox.Show("Plaease type a message");
                    return;
                }


                Message messageObj = new Message(curUser, message, selectedLobby, selectedUser);

                SendButton.Content = "Sending...";

                await Task.Run(() => _lobbyServer.SendMessage(messageObj));
                
                MessageTextBox.Text = "";

                SendButton.Content = "Send";
            
            }


        }

        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLobby != null)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(filePath);

                    byte[] fileData = await Task.Run(() => System.IO.File.ReadAllBytes(filePath));

                    Message fileMessage = new Message(curUser, $"File shared: {fileName}", selectedLobby, fileName);

                    SendButton.Content = "Sending...";

                    // Send file data to the server
                    await Task.Run(() => _lobbyServer.ShareFile(selectedLobby, curUser, fileData, fileName, selectedUser));

                    SendButton.Content = "Send";
                }
            }
        }

        private async void pullMessages(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<Message> newMessages = await Task.Run(() => _lobbyServer.GetMessages(selectedLobby, msgCount, _lobbies[selectedLobby].lastMsgId, curUser));

                if (newMessages != null && newMessages.Count != 0)
                {
                    foreach (var message in newMessages)
                    {
                        if (message.Room == selectedLobby)
                        {
                            messages.Add(message);
                            AddMessageToChat(message);
                            _lobbies[selectedLobby].lastMsgId = message.Id;
                            
                        }
                    }
                }

               Thread.Sleep(1000);
            }

        }

        private async void pullLobbies(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<LobbyRoom> lobbies = await Task.Run(() => _lobbyServer.GetRooms(curUser));

                if (lobbies != null)
                {
                    foreach (var lobby in lobbies)
                    {
                        if(!_lobbies.ContainsKey(lobby.name))
                        {
                            UpdateLobbies(lobby);
                        }
                    }
                }

                Thread.Sleep(1000);
            }

        }

        private async void pullUsers(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<string> users = await Task.Run(() => _lobbyServer.GetUsers(selectedLobby));

                if (users != null)
                {
                    UpdateUsers(users);
                }

                Thread.Sleep(1000);
            }

        }

        public void AddMessageToChat(Message message)
        {
            if (selectedLobby == message.Room)
            {
                msgCount++;


                Dispatcher.Invoke(() => {
                    Paragraph paragraph = new Paragraph();

                    Run timeStamp = new Run(message.Time.ToShortTimeString() + ": ");
                    timeStamp.Foreground = Brushes.Gray;

                    Run sender;

                    if (message.ToUser == null || message.ToUser.Equals("All"))
                    {
                        sender = new Run(message.Username + ": ");
                        sender.FontWeight = FontWeights.Bold;
                    }
                    else {
                        sender = new Run(message.Username + " -> " + message.ToUser + ": ");
                        sender.FontWeight = FontWeights.Bold;
                        sender.Foreground = Brushes.Blue;
                    }

                    Run text = new Run(message.Text);

                    paragraph.Inlines.Add(timeStamp);
                    paragraph.Inlines.Add(sender);
                    paragraph.Inlines.Add(text);

                    // File link if available
                    if (!string.IsNullOrEmpty(message.FilePath))
                    {
                        Hyperlink fileLink = new Hyperlink(new Run(message.FilePath))
                        {
                            NavigateUri = new Uri(message.FilePath, UriKind.Absolute)
                        };
                        fileLink.RequestNavigate += FileLink_RequestNavigate;

                        // Style the hyperlink as a clickable link
                        fileLink.Foreground = Brushes.Blue;
                        fileLink.TextDecorations = TextDecorations.Underline;

                        paragraph.Inlines.Add(new LineBreak());
                        paragraph.Inlines.Add(fileLink);
                    }

                    paragraph.FontFamily = new FontFamily("Arial");
                    ChatRichTextBox.Document.Blocks.Add(paragraph);
                    ChatRichTextBox.ScrollToEnd();

                });
                
            }
        }

        private void FileLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.LocalPath, // Use LocalPath for local files
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}");
            }
        }


        public void UpdateLobbies(LobbyRoom lobbyRoom)
        {
            _lobbies[lobbyRoom.name] = lobbyRoom;
            Dispatcher.Invoke(() => LobbyList.Items.Add(lobbyRoom.name));
        }

        public void UpdateUsers(List<string> users)
        {

            Dispatcher.Invoke(() => {
                if (!ToUserComboBox.IsDropDownOpen)
                { 

                    string selected = ToUserComboBox.SelectedItem?.ToString();

                    ToUserComboBox.Items.Clear();
                    users.Clear();

                    ToUserComboBox.Items.Add("All");

                    foreach (var user in users)
                    {
                        if (!user.Equals(curUser))
                        {
                            users.Add(user);
                            ToUserComboBox.Items.Add(user);
                        }
                    }


                    if (selected != null)
                    {
                        ToUserComboBox.SelectedItem = selected;
                        selectedUser = selected;
                    }
                    else
                    {
                        ToUserComboBox.SelectedIndex = 0;
                        selectedUser = "All";
                    }
                    
                }
            });
            
            
        }

        private async void LobbyRefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLobby != null)
            {
                await Task.Run(() => _lobbyServer.LeaveLobbyRoom(curUser, selectedLobby));
                ChatWindowGrid.Visibility = Visibility.Collapsed;
                _lobbies[selectedLobby].lastMsgId = 0;
                _lobbies[selectedLobby].msgCount = 0;
                selectedLobby = null;
            }

            Dispatcher.Invoke(() => LobbyList.Items.Clear());
            foreach (var lobby in _lobbies)
            {
                Dispatcher.Invoke(() => LobbyList.Items.Add(lobby.Key));
            }

            if (_messagesCts != null)
            {
                _messagesCts.Cancel();
            }

            if (_usersCts != null)
            {
                _usersCts.Cancel();
            }

        }

        private void MsgRefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => ChatRichTextBox.Document.Blocks.Clear());
            foreach (var message in messages)
            {
                AddMessageToChat(message);
            }

        }

        private void UsersRefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => ToUserComboBox.Items.Clear());
            foreach (var user in users)
            {
                Dispatcher.Invoke(() => ToUserComboBox.Items.Add(user));
            }
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _messagesCts?.Cancel();
            _lobbiesCts?.Cancel();
            _usersCts?.Cancel();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _lobbyServer.Disconnect(curUser);
        }

        private void ToUserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
               selectedUser = ToUserComboBox.SelectedValue?.ToString();
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLobby != null)
            {
                _lobbyServer.LeaveLobbyRoom(curUser, selectedLobby);

            }
            _lobbyServer.Disconnect(curUser);

            ShowLoginWindow();

        }

        
    }
}
