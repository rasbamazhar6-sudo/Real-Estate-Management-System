using Project.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Project.Services;

namespace Project.Pages
{
    public partial class Page48Page : Page
    {
        private List<PaymentPlan> _paymentPlans = new();
        private List<PaymentPlan> _filteredPlans = new();
        private List<Buyer> _buyers = new();
        private List<Plot> _plots = new();
        private PaymentPlan? _selectedPlan;
        private bool _isLoaded = false;
        
        public Page48Page()
        {
            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadBuyersAndPlots();
                await LoadDataFromDatabase();
                if (dpStartDate != null) dpStartDate.SelectedDate = DateTime.Now;
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task LoadBuyersAndPlots()
        {
            try
            {
                var buyerList = await Task.Run(() => PartyDataAccess.GetAllBuyers());
                _buyers = buyerList.Select(b => new Buyer 
                { 
                    BuyerId = b.BuyerId, 
                    Name = b.Name 
                }).ToList();
                
                var plotList = await Task.Run(() => PlotDataAccess.GetAllPlots());
                _plots = plotList.Select(p => new Plot 
                { 
                    PlotId = p.PlotId, 
                    PlotNo = p.PlotNo 
                }).ToList();
                
                if (cmbBuyer != null) cmbBuyer.ItemsSource = _buyers;
                if (cmbPlot != null) cmbPlot.ItemsSource = _plots;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading buyers and plots: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadDataFromDatabase()
        {
            try
            {
                if (dgPaymentPlans != null) dgPaymentPlans.IsEnabled = false;

                var data = await Task.Run(() => PaymentPlanDataAccess.GetAllPaymentPlans());

                _paymentPlans = data;
                _filteredPlans = new List<PaymentPlan>(_paymentPlans);

                if (dgPaymentPlans != null)
                {
                    dgPaymentPlans.ItemsSource = null;
                    dgPaymentPlans.ItemsSource = _filteredPlans;
                    dgPaymentPlans.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (dgPaymentPlans != null) dgPaymentPlans.IsEnabled = true;

                MessageBox.Show($"Error loading payment plans: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CmbPlanType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Don't calculate if the page is still initializing
            if (txtTotalAmount == null || txtDownPayment == null || 
                txtInstallments == null || txtInstallmentAmount == null)
            {
                return;
            }
            CalculateInstallmentAmount();
        }
        
        private void TxtTotalAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't calculate if controls aren't initialized
            if (txtTotalAmount == null || txtDownPayment == null || 
                txtInstallments == null || txtInstallmentAmount == null)
            {
                return;
            }
            CalculateInstallmentAmount();
        }
        
        private void TxtDownPayment_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't calculate if controls aren't initialized
            if (txtTotalAmount == null || txtDownPayment == null || 
                txtInstallments == null || txtInstallmentAmount == null)
            {
                return;
            }
            CalculateInstallmentAmount();
        }
        
        private void TxtInstallments_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't calculate if controls aren't initialized
            if (txtTotalAmount == null || txtDownPayment == null || 
                txtInstallments == null || txtInstallmentAmount == null)
            {
                return;
            }
            CalculateInstallmentAmount();
        }
        
        private void CalculateInstallmentAmount()
        {
            // Check if controls are initialized
            if (txtTotalAmount == null || txtDownPayment == null || 
                txtInstallments == null || txtInstallmentAmount == null)
            {
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtTotalAmount.Text) ||
                string.IsNullOrWhiteSpace(txtDownPayment.Text) ||
                string.IsNullOrWhiteSpace(txtInstallments.Text))
            {
                txtInstallmentAmount.Text = "";
                return;
            }
            
            if (decimal.TryParse(txtTotalAmount.Text, out decimal totalAmount) &&
                decimal.TryParse(txtDownPayment.Text, out decimal downPayment) &&
                int.TryParse(txtInstallments.Text, out int installments) &&
                installments > 0)
            {
                decimal remainingAmount = totalAmount - downPayment;
                decimal installmentAmount = remainingAmount / installments;
                txtInstallmentAmount.Text = $"{installmentAmount:N2}";
            }
            else
            {
                txtInstallmentAmount.Text = "";
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            btnSave.IsEnabled = false;

            try
            {
                if (cmbBuyer.SelectedItem == null)
                {
                    MessageBox.Show("Please select a buyer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbPlot.SelectedItem == null)
                {
                    MessageBox.Show("Please select a plot.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtTotalAmount.Text) || !decimal.TryParse(txtTotalAmount.Text, out decimal totalAmount) || totalAmount <= 0)
                {
                    MessageBox.Show("Please enter a valid total amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDownPayment.Text) || !decimal.TryParse(txtDownPayment.Text, out decimal downPayment) || downPayment < 0)
                {
                    MessageBox.Show("Please enter a valid down payment.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtInstallments.Text) || !int.TryParse(txtInstallments.Text, out int installments) || installments <= 0)
                {
                    MessageBox.Show("Please enter a valid number of installments.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpStartDate.SelectedDate == null)
                {
                    MessageBox.Show("Please select a start date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (downPayment >= totalAmount)
                {
                    MessageBox.Show("Down payment cannot be greater than or equal to total amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var buyer = (Buyer)cmbBuyer.SelectedItem;
                var plot = (Plot)cmbPlot.SelectedItem;

                if (!int.TryParse(buyer.BuyerId, out int buyerIdInt))
                {
                    MessageBox.Show("Invalid Buyer ID.");
                    return;
                }

                if (!int.TryParse(plot.PlotId, out int plotIdInt))
                {
                    MessageBox.Show("Invalid Plot ID.");
                    return;
                }

                string raw = txtInstallmentAmount.Text
                                .Replace("PKR", "")
                                .Replace(",", "")
                                .Trim();

                if (!decimal.TryParse(raw, out decimal installmentAmount))
                {
                    MessageBox.Show("Invalid installment amount.");
                    return;
                }

                var planType = ((ComboBoxItem)cmbPlanType.SelectedItem)?.Content?.ToString() ?? "Monthly";
                var status = ((ComboBoxItem)cmbStatus.SelectedItem)?.Content?.ToString() ?? "Active";

                bool overdueReminder = chkOverdueReminder.IsChecked ?? true;
                bool upcomingReminder = chkUpcomingReminder.IsChecked ?? true;

                if (_selectedPlan != null)
                {
                    await Task.Run(() => PaymentPlanDataAccess.UpdatePaymentPlanComplete(
                        _selectedPlan.PaymentPlanId,
                        buyerIdInt,
                        plotIdInt,
                        totalAmount,
                        downPayment,
                        installments,
                        installmentAmount,
                        dpStartDate.SelectedDate.Value,
                        planType,
                        status,
                        overdueReminder,
                        upcomingReminder
                    ));
                }
                else
                {
                    await Task.Run(() => PaymentPlanDataAccess.SavePaymentPlanComplete(
                        buyerIdInt,
                        plotIdInt,
                        totalAmount,
                        downPayment,
                        installments,
                        installmentAmount,
                        dpStartDate.SelectedDate.Value,
                        planType,
                        status,
                        overdueReminder,
                        upcomingReminder
                    ));
                }

                await LoadDataFromDatabase();

                MessageBox.Show("Payment plan saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                BtnClear_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving payment plan: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            cmbBuyer.SelectedItem = null;
            cmbPlot.SelectedItem = null;
            cmbPlanType.SelectedIndex = 0;
            txtTotalAmount.Clear();
            txtDownPayment.Clear();
            txtInstallments.Clear();
            txtInstallmentAmount.Clear();
            dpStartDate.SelectedDate = DateTime.Now;
            cmbStatus.SelectedIndex = 0;
            chkOverdueReminder.IsChecked = true;
            chkUpcomingReminder.IsChecked = true;
            _selectedPlan = null;
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPlan == null)
            {
                MessageBox.Show("Please select a payment plan to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show($"Are you sure you want to delete payment plan {_selectedPlan.PlanId}?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (!_isLoaded) return;
                btnDelete.IsEnabled = false;

                try
                {
                    await Task.Run(() => PaymentPlanDataAccess.DeletePaymentPlan(_selectedPlan.PaymentPlanId));
                    await LoadDataFromDatabase();
                    BtnClear_Click(sender, e);
                    MessageBox.Show("Payment plan deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting payment plan: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btnDelete.IsEnabled = true;
                }
            }
        }
        
        private void DgPaymentPlans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (dgPaymentPlans.SelectedItem is PaymentPlan plan)
            {
                _selectedPlan = plan;
                
                var buyer = _buyers.FirstOrDefault(b => b.BuyerId == plan.BuyerId);
                var plot = _plots.FirstOrDefault(p => p.PlotId == plan.PlotId);
                
                if (buyer != null) cmbBuyer.SelectedItem = buyer;
                if (plot != null) cmbPlot.SelectedItem = plot;
                
                var planTypeItem = cmbPlanType.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == plan.PlanType);
                if (planTypeItem != null) cmbPlanType.SelectedItem = planTypeItem;
                
                txtTotalAmount.Text = $"{plan.TotalAmount:N2}";
                txtDownPayment.Text = $"{plan.DownPayment:N2}";
                txtInstallments.Text = plan.InstallmentCount.ToString();
                txtInstallmentAmount.Text = $"{plan.InstallmentAmount:N2}";
                dpStartDate.SelectedDate = plan.StartDate;
                
                var statusItem = cmbStatus.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == plan.Status);
                if (statusItem != null) cmbStatus.SelectedItem = statusItem;
                
                chkOverdueReminder.IsChecked = plan.OverdueReminder;
                chkUpcomingReminder.IsChecked = plan.UpcomingReminder;
            }
        }
        
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch == null || cmbFilterStatus == null || dgPaymentPlans == null)
            {
                return;
            }
            ApplyFilters();
        }
        
        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtSearch == null || cmbFilterStatus == null || dgPaymentPlans == null)
            {
                return;
            }
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            if (txtSearch == null || cmbFilterStatus == null || dgPaymentPlans == null)
            {
                return;
            }
            
            _filteredPlans.Clear();
            var query = _paymentPlans.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var searchText = txtSearch.Text.ToLower();
                query = query.Where(p => 
                    (p.BuyerName ?? "").ToLower().Contains(searchText) ||
                    (p.PlotNo ?? "").ToLower().Contains(searchText) ||
                    (p.PlanId ?? "").ToLower().Contains(searchText));
            }
            
            if (cmbFilterStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "All Status")
            {
                query = query.Where(p => p.Status == statusItem.Content.ToString());
            }
            
            _filteredPlans.AddRange(query);
            dgPaymentPlans.ItemsSource = null;
            dgPaymentPlans.ItemsSource = _filteredPlans;
        }
        
        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredPlans.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            ExportService.ExportToPDF(
                    dgPaymentPlans,
                    "Payment Plans Report",
                    $"PaymentPlans_{DateTime.Now:yyyyMMdd}.pdf"
                );
        }
        
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredPlans.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExportService.ExportToExcel(
                    dgPaymentPlans,
                    $"PaymentPlans_{DateTime.Now:yyyyMMdd}.xlsx"
                );
        }
        
        private class Buyer
        {
            public string BuyerId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
        
        private class Plot
        {
            public string PlotId { get; set; } = string.Empty;
            public string PlotNo { get; set; } = string.Empty;
        }
        
        public class PaymentPlan
        {
            public int PaymentPlanId { get; set; }
            public string PlanId { get; set; } = string.Empty;
            public string BuyerId { get; set; } = string.Empty;
            public string BuyerName { get; set; } = string.Empty;
            public string PlotId { get; set; } = string.Empty;
            public string PlotNo { get; set; } = string.Empty;
            public string PlanType { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public decimal DownPayment { get; set; }
            public int InstallmentCount { get; set; }
            public decimal InstallmentAmount { get; set; }
            public DateTime StartDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool OverdueReminder { get; set; }
            public bool UpcomingReminder { get; set; }
        }
    }
}