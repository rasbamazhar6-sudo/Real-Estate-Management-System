using System;
using System.Windows;
using System.Windows.Controls;
using Project.Pages;

namespace Project
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {

        public MainPage()
        {
            InitializeComponent();

            // Load default page
            ContentFrame.Navigate(new ProjectManagementPage());

            // Highlight default button
            SetActiveButton(btnProjectManagement);
        }


        // ===============================
        // ACTIVE NAVIGATION BUTTON
        // ===============================

        private void SetActiveButton(Button activeButton)
        {
            btnProjectManagement.Tag = null;
            btnPlotManagement.Tag = null;
            btnPlotVisualDashboard.Tag = null;
            btnOwnershipMapping.Tag = null;
            btnCustomerLedger.Tag = null;
            btnLedgerView.Tag = null;
            btnQuickEntry.Tag = null;
            btnAgingReport.Tag = null;
            btnCustomerStatements.Tag = null;
            btnModule48.Tag = null;

            activeButton.Tag = "Active";
        }


        // ===============================
        // NAVIGATION BUTTONS
        // ===============================

        private void BtnProjectManagement_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnProjectManagement);
            ContentFrame.Navigate(new ProjectManagementPage());
        }

        private void BtnPlotManagement_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnPlotManagement);
            ContentFrame.Navigate(new PlotManagementPage());
        }

        private void BtnPlotVisualDashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnPlotVisualDashboard);
            ContentFrame.Navigate(new PlotVisualDashboardPage());
        }

        private void BtnOwnershipMapping_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(btnOwnershipMapping);
            ContentFrame.Navigate(new OwnershipMappingPage());
        }

        private void BtnModule48_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetActiveButton(btnModule48);
                ContentFrame.Navigate(new Page48Page());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Installments & Payment Plans page:\n\n{ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnModule410_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetActiveButton(btnCustomerLedger);
                ContentFrame.Navigate(new CustomerLedgerReceivablesManagementPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Customer Ledger page:\n\n{ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnLedgerView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetActiveButton(btnLedgerView);
                ContentFrame.Navigate(new LedgerViewPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Ledger View page:\n\n{ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnQuickEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetActiveButton(btnQuickEntry);
                ContentFrame.Navigate(new QuickEntryPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Quick Entry page:\n\n{ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnAgingReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetActiveButton(btnAgingReport);
                ContentFrame.Navigate(new ReceivablesAgingReportPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Aging Report page:\n\n{ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCustomerStatements_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetActiveButton(btnCustomerStatements);
                ContentFrame.Navigate(new CustomerStatementsPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error navigating to Customer Statements page:\n\n{ex.Message}",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}