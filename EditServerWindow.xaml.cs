using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace 云端管理
{
    public partial class EditServerWindow : Window
    {
        public SshProfile ResultProfile { get; private set; }
        private string _keyContent = "";

        public EditServerWindow(SshProfile profile = null)
        {
            InitializeComponent();
            if (profile != null)
            {
                NameInput.Text = profile.Name;
                HostInput.Text = profile.Host;
                PortInput.Text = profile.Port;
                UserInput.Text = profile.Username;

                if (profile.AuthType == "Key")
                {
                    RadioKey.IsChecked = true;
                    _keyContent = profile.SecretData;
                    KeyPathDisplay.Text = "已加载已有密钥";
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void AuthType_Changed(object sender, RoutedEventArgs e)
        {
            if (PasswordArea == null || KeyArea == null) return;
            bool isKey = RadioKey.IsChecked == true;
            PasswordArea.Visibility = isKey ? Visibility.Collapsed : Visibility.Visible;
            KeyArea.Visibility = isKey ? Visibility.Visible : Visibility.Collapsed;
            SecretLabel.Text = isKey ? "私钥文件" : "SSH 密码";
        }

        private void SelectKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                KeyPathDisplay.Text = Path.GetFileName(openFileDialog.FileName);
                _keyContent = File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ResultProfile = new SshProfile
            {
                Name = string.IsNullOrWhiteSpace(NameInput.Text) ? HostInput.Text : NameInput.Text,
                Host = HostInput.Text,
                Port = string.IsNullOrWhiteSpace(PortInput.Text) ? "22" : PortInput.Text,
                Username = UserInput.Text,
                AuthType = RadioKey.IsChecked == true ? "Key" : "Password",
                SecretData = RadioKey.IsChecked == true ? _keyContent : PasswordInput.Password
            };
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}