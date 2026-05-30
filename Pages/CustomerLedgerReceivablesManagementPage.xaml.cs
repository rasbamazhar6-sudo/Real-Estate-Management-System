using Microsoft.Data.SqlClient;
using Project.Data;
using Project.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static Project.Pages.CustomerLedgerReceivablesManagementPage;

namespace Project.Pages
{
    /// <summary>
    /// Interaction logic for CustomerLedgerReceivablesManagementPage.xaml
    /// </summary>
    public partial class CustomerLedgerReceivablesManagementPage : Page
    {
        public class CustomerLedger
        {
            public int PartyId { get; set; }
            public string? Name { get; set; }
            public string? CNICNTN { get; set; }
            public string? Contact { get; set; }
            public string? Address { get; set; }
            public string? Type { get; set; }
            public string? Status { get; set; }
        }

        private List<CustomerLedger> _customers = new List<CustomerLedger>();
        private List<CustomerLedger> _filteredCustomers = new List<CustomerLedger>();
        private CustomerLedger? _selectedCustomer;
        private bool _isLoaded = false;

        public CustomerLedgerReceivablesManagementPage()
        {
            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializeComboBoxes();
                await LoadDataFromDatabase();
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeComboBoxes()
        {
            cmbType.ItemsSource = new List<string> { Constants.PartyTypeBuyer, Constants.PartyTypeSeller, Constants.PartyTypeAgent };
            cmbStatus.ItemsSource = new List<string> { Constants.StatusActive, Constants.StatusInactive };
            
            cmbFilterType.Items.Add(new ComboBoxItem { Content = "All Types", IsSelected = true });
            cmbFilterType.Items.Add(new ComboBoxItem { Content = Constants.PartyTypeBuyer });
            cmbFilterType.Items.Add(new ComboBoxItem { Content = Constants.PartyTypeSeller });
            cmbFilterType.Items.Add(new ComboBoxItem { Content = Constants.PartyTypeAgent });
            
            cmbFilterStatus.Items.Add(new ComboBoxItem { Content = "All Status", IsSelected = true });
            cmbFilterStatus.Items.Add(new ComboBoxItem { Content = Constants.StatusActive });
            cmbFilterStatus.Items.Add(new ComboBoxItem { Content = Constants.StatusInactive });
        }

        private async Task LoadDataFromDatabase()
        {
            try
            {
                if (dgCustomers != null) dgCustomers.IsEnabled = false;

                var parties = await Task.Run(() => PartyDataAccess.GetAllParties());
                var mappedCustomers = parties.Select(p => new CustomerLedger
                {
                    PartyId = p.PartyId,
                    Name = p.Name ?? "",
                    CNICNTN = p.CNIC ?? "",
                    Contact = p.ContactPhone ?? "",
                    Address = p.Address ?? "",
                    Type = p.Type ?? "",
                    Status = p.Status ?? Constants.StatusActive
                }).ToList();

                _customers = mappedCustomers;
                _filteredCustomers = new List<CustomerLedger>(_customers);
                if (dgCustomers != null)
                {
                    dgCustomers.ItemsSource = null;
                    dgCustomers.ItemsSource = _filteredCustomers;
                    dgCustomers.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (dgCustomers != null) dgCustomers.IsEnabled = true;
                MessageBox.Show($"Error loading customers: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSave.IsEnabled = false;

            try
            {
                string name = txtName.Text.Trim();
                string cnic = txtCNICNTN.Text?.Trim() ?? "";
                string contact = txtContact.Text?.Trim() ?? "";
                string address = txtAddress.Text?.Trim() ?? "";
                string type = cmbType.SelectedItem?.ToString() ?? Constants.PartyTypeBuyer;
                string status = cmbStatus.SelectedItem?.ToString() ?? Constants.StatusActive;

                if (_selectedCustomer != null)
                {
                    // Update existing
                    await Task.Run(() => PartyManagementDataAccess.UpdateParty(
                        _selectedCustomer.PartyId,
                        name,
                        type,
                        string.IsNullOrWhiteSpace(cnic) ? null : cnic,
                        string.IsNullOrWhiteSpace(contact) ? null : contact,
                        null, // ContactEmail - not in UI
                        string.IsNullOrWhiteSpace(address) ? null : address,
                        status
                    ));
                    MessageBox.Show("Customer updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Check if CNIC already exists
                    var existingParty = _customers.FirstOrDefault(c => (c.CNICNTN ?? "") == cnic && !string.IsNullOrWhiteSpace(cnic));
                    if (existingParty != null)
                    {
                        MessageBox.Show("A customer with this CNIC/NTN already exists. Please select it to update.", 
                            "Duplicate CNIC", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Add new
                    await Task.Run(() => PartyManagementDataAccess.InsertParty(
                        name,
                        type,
                        string.IsNullOrWhiteSpace(cnic) ? null : cnic,
                        string.IsNullOrWhiteSpace(contact) ? null : contact,
                        null, // ContactEmail - not in UI
                        string.IsNullOrWhiteSpace(address) ? null : address,
                        status
                    ));
                    MessageBox.Show("Customer added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadDataFromDatabase();
                BtnClear_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving customer: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
            }
        }


        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _selectedCustomer = null;
            txtName.Clear();
            txtCNICNTN.Clear();
            txtContact.Clear();
            txtAddress.Clear();
            cmbType.SelectedIndex = -1;
            cmbStatus.SelectedIndex = -1;
            if (dgCustomers != null) dgCustomers.SelectedItem = null;
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer == null)
            {
                MessageBox.Show("Please select a customer to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete customer '{_selectedCustomer.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (!_isLoaded) return;
            btnDelete.IsEnabled = false;

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await Task.Run(() => PartyManagementDataAccess.DeleteParty(_selectedCustomer.PartyId));
                    await LoadDataFromDatabase();
                    BtnClear_Click(sender, e);
                    MessageBox.Show("Customer deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    MessageBox.Show("Cannot delete customer because they have related records (sales, transactions, or plots).", "Dependency Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btnDelete.IsEnabled = true;
                }
            }
            else
            {
                btnDelete.IsEnabled = true;
            }
        }

        private void DgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (dgCustomers.SelectedItem is CustomerLedger selectedCustomer)
            {
                _selectedCustomer = selectedCustomer;
                txtName.Text = selectedCustomer.Name ?? "";
                txtCNICNTN.Text = selectedCustomer.CNICNTN ?? "";
                txtContact.Text = selectedCustomer.Contact ?? "";
                txtAddress.Text = selectedCustomer.Address ?? "";
                
                // Set combobox selections - find the item in the ItemsSource
                if (cmbType.ItemsSource is List<string> typeList)
                {
                    var selectedType = selectedCustomer.Type ?? "";
                    cmbType.SelectedItem = typeList.FirstOrDefault(t => t == selectedType);
                }
                
                if (cmbStatus.ItemsSource is List<string> statusList)
                {
                    var selectedStatus = selectedCustomer.Status ?? "";
                    cmbStatus.SelectedItem = statusList.FirstOrDefault(s => s == selectedStatus);
                }
            }
            else
            {
                _selectedCustomer = null;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            // Guard against null or empty customers list (prevents crash during initialization)
            if (_customers == null || _customers.Count == 0)
            {
                _filteredCustomers = new List<CustomerLedger>();
                if (dgCustomers != null)
                {
                    dgCustomers.ItemsSource = null;
                    dgCustomers.ItemsSource = _filteredCustomers;
                }
                return;
            }

            _filteredCustomers = _customers.Where(c =>
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(txtSearch.Text) ||
                                    (c.Name != null && c.Name.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase)) ||
                                    (c.CNICNTN != null && c.CNICNTN.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase)) ||
                                    (c.Contact != null && c.Contact.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase));
                
                bool matchesType = cmbFilterType.SelectedItem is ComboBoxItem typeItem &&
                                  (typeItem.Content.ToString() == "All Types" || (c.Type ?? "") == typeItem.Content.ToString());
                
                bool matchesStatus = cmbFilterStatus.SelectedItem is ComboBoxItem statusItem &&
                                    (statusItem.Content.ToString() == "All Status" || (c.Status ?? "") == statusItem.Content.ToString());
                
                return matchesSearch && matchesType && matchesStatus;
            }).ToList();

            // Update customer data grid with filtered results
            if (dgCustomers != null)
            {
                dgCustomers.ItemsSource = null;
                dgCustomers.ItemsSource = _filteredCustomers;
            }
        }

        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredCustomers.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExportService.ExportToPDF(
                                dgCustomers,
                                "Customer Ledger Report",
                                $"CustomerLedger_{DateTime.Now:yyyyMMdd}.pdf"
            );
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredCustomers.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExportService.ExportToExcel(dgCustomers, $"CustomerLedger_{DateTime.Now:yyyyMMdd}.xlsx");
        }

    }
}
