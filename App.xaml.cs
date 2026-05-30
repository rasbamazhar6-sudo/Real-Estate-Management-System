using System.Configuration;
using System.Data;
using System.Windows;
using Project.Data;

namespace Project
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Test database connection on startup
            if (!DatabaseHelper.TestConnection())
            {
                string errorDetails = DatabaseHelper.GetConnectionError();
                string message = "Cannot connect to the database. Please check:\n" +
                    "1. SQL Server is running\n" +
                    "2. Database 'RealEstateDB' exists\n" +
                    "3. Connection string is correct\n" +
                    "4. Windows Authentication is enabled";
                
                if (!string.IsNullOrEmpty(errorDetails))
                {
                    message += $"\n\nError details: {errorDetails}";
                }
                
                MessageBox.Show(
                    message,
                    "Database Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

}
