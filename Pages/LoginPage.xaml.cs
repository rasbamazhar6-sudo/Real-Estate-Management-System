using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Project;
using Project.Data;

namespace Project.Pages
{
    public partial class LoginPage : Page
    {

        public LoginPage()
        {
            InitializeComponent();

            Loaded += LoginPage_Loaded;
            txtPassword.KeyDown += TxtPassword_KeyDown;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }
        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Please contact your system administrator to reset your password.",
                "Forgot Password",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(this, new RoutedEventArgs());
            }
        }

        private void BtnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).NavigateWithFade(new CreateAccountPage());
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (email == "" || password == "")
            {
                ShowError("Please enter email and password.");
                return;
            }

            try
            {
                string hashedPassword = UserDataAccess.HashPassword(password);
                
                if (UserDataAccess.AuthenticateUser(email, hashedPassword))
                {
                    txtErrorMessage.Visibility = Visibility.Collapsed;
                    ((MainWindow)Application.Current.MainWindow).NavigateWithFade(new MainPage());
                }
                else
                {
                    ShowError("Incorrect email or password.");
                    txtPassword.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            txtErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
