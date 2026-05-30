using System;
using System.Windows;
using System.Windows.Controls;
using Project.Data;

namespace Project.Pages
{
    public partial class CreateAccountPage : Page
    {

        public CreateAccountPage()
        {
            InitializeComponent();
            Loaded += CreateAccountPage_Loaded;
        }

        private void CreateAccountPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtFullName.Focus();
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).NavigateWithFade(new LoginPage());
        }

        private void BtnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            // Validation
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("Please enter your full name.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                ShowError("Please enter a valid email address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            try
            {
                // Check if email already exists
                if (UserDataAccess.EmailExists(email))
                {
                    ShowError("This email is already registered. Please use a different email.");
                    return;
                }

                // Split full name into first and last name
                string name = fullName;

                // Hash password and create account
                string hashedPassword = UserDataAccess.HashPassword(password);
                int userId = UserDataAccess.CreateUser(name, email, hashedPassword);

                if (userId > 0)
                {
                    MessageBox.Show(
                        "Account created successfully! You can now sign in.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    ((MainWindow)Application.Current.MainWindow).NavigateWithFade(new LoginPage());
                }
                else
                {
                    ShowError("Failed to create account. Please try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowError("Failed to create account. Please try again.");
            }
        }

        private void ShowError(string message)
        {
            txtErrorMessage.Text = message;
            txtErrorMessage.Visibility = Visibility.Visible;
        }
    }
}

