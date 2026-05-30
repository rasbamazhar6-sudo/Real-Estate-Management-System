using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using Project.Data;

namespace Project.Pages
{
    /// <summary>
    /// Interaction logic for OwnershipMappingPage.xaml
    /// </summary>
    public partial class OwnershipMappingPage : Page
    {
        public class PlotInfo
        {
            public int PlotId { get; set; }
            public string PlotNo { get; set; } = string.Empty;
        }

        public class PartyInfo
        {
            public int PartyId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }

        public class OwnershipMapping
        {
            public int SaleId { get; set; }
            public string PlotNo { get; set; } = string.Empty;
            public string BuyerSellerName { get; set; } = string.Empty;
            public string OwnershipType { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
            public string Notes { get; set; } = string.Empty;
        }

        private List<OwnershipMapping> _mappings = new();
        private List<PlotInfo> _plots = new();
        private List<PartyInfo> _parties = new();
        private bool _isLoaded = false;

        public OwnershipMappingPage()
        {
            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadPlots();
                await LoadParties();
                await LoadDataFromDatabase();
                if (cmbOwnershipType != null) cmbOwnershipType.ItemsSource = new List<string> { "Buyer", "Seller", "Agent" };
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPlots()
        {
            try
            {
                var plotList = await Task.Run(() => PlotManagementDataAccess.GetAllPlots());
                var mappedPlots = plotList.Select(p => new PlotInfo
                {
                    PlotId = p.PlotId,
                    PlotNo = p.PlotNo
                }).ToList();

                _plots = mappedPlots;
                if (cmbPlot != null)
                {
                    cmbPlot.ItemsSource = _plots;
                    cmbPlot.SelectedValuePath = "PlotId";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading plots: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadParties()
        {
            try
            {
                // Load all parties (Buyers, Sellers, Agents) using the DAL
                var partyTypes = new[] { Constants.PartyTypeBuyer, Constants.PartyTypeSeller, Constants.PartyTypeAgent };
                var allParties = await Task.Run(() => PartyDataAccess.GetPartiesByTypes(partyTypes));

                var mappedParties = allParties.Select(p => new PartyInfo
                {
                    PartyId = p.PartyId,
                    Name = p.Name,
                    Type = p.Type
                }).ToList();

                _parties = mappedParties;
                if (cmbBuyerSeller != null)
                {
                    cmbBuyerSeller.ItemsSource = _parties;
                    cmbBuyerSeller.SelectedValuePath = "PartyId";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading parties: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadDataFromDatabase()
        {
            try
            {
                if (dgOwnership != null) dgOwnership.IsEnabled = false;

                var sales = await Task.Run(() => SalesDataAccess.GetAllSales());
                var mappedMappings = sales.Select(s => new OwnershipMapping
                {
                    SaleId = s.SaleId,
                    PlotNo = s.PlotNo,
                    BuyerSellerName = !string.IsNullOrEmpty(s.BuyerName) ? s.BuyerName : (!string.IsNullOrEmpty(s.SellerName) ? s.SellerName : ""),
                    OwnershipType = s.BuyerId.HasValue ? Constants.PartyTypeBuyer : (s.SellerId.HasValue ? (s.SellerType == Constants.PartyTypeAgent ? Constants.PartyTypeAgent : Constants.PartyTypeSeller) : ""),
                    Date = s.SaleDate,
                    Amount = s.SalePrice,
                    Notes = s.Notes ?? ""
                }).ToList();

                _mappings = mappedMappings;
                if (dgOwnership != null)
                {
                    dgOwnership.ItemsSource = null;
                    dgOwnership.ItemsSource = _mappings;
                    dgOwnership.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (dgOwnership != null) dgOwnership.IsEnabled = true;
                MessageBox.Show($"Error loading ownership mappings: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private OwnershipMapping? _selectedMapping;

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            if (cmbPlot.SelectedItem == null)
            {
                MessageBox.Show("Please select a Plot.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbBuyerSeller.SelectedItem == null)
            {
                MessageBox.Show("Please select a Buyer/Seller.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rawAmount = txtAmount.Text
                .Replace("PKR", "")
                .Replace(",", "")
                .Trim();

            if (!decimal.TryParse(rawAmount, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpDate.SelectedDate == null)
            {
                MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSave.IsEnabled = false;

            try
            {
                var selectedPlot = (PlotInfo)cmbPlot.SelectedItem;
                var selectedParty = (PartyInfo)cmbBuyerSeller.SelectedItem;
                string ownershipType = cmbOwnershipType.SelectedItem?.ToString() ?? Constants.PartyTypeBuyer;

                // Get ProjectId from DAL
                int projectId = await Task.Run(() => PlotManagementDataAccess.GetProjectIdByPlotId(selectedPlot.PlotId));
                if (projectId == 0)
                {
                    MessageBox.Show("Could not find project for this plot.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int? buyerId = ownershipType == Constants.PartyTypeBuyer ? selectedParty.PartyId : null;
                int? sellerId = (ownershipType == Constants.PartyTypeSeller || ownershipType == Constants.PartyTypeAgent) ? selectedParty.PartyId : null;

                // Get notes from text field
                string notes = txtNotes?.Text?.Trim() ?? "";

                if (_selectedMapping != null)
                {
                    // Update existing sale
                    await Task.Run(() => SalesDataAccess.UpdateSale(
                        _selectedMapping.SaleId,
                        buyerId,
                        sellerId,
                        amount,
                        null, 
                        dpDate.SelectedDate.Value,
                        Constants.StatusActive,
                        notes
                    ));
                }
                else
                {
                    // Check if plot already has a sale
                    var existingSale = await Task.Run(() => SalesDataAccess.GetSaleByPlotId(selectedPlot.PlotId));
                    if (existingSale != null)
                    {
                        // Update existing sale
                        await Task.Run(() => SalesDataAccess.UpdateSale(
                            existingSale.SaleId,
                            buyerId,
                            sellerId,
                            amount,
                            null,
                            dpDate.SelectedDate.Value,
                            Constants.StatusActive,
                            notes
                        ));
                    }
                    else
                    {
                        // Create new sale
                        await Task.Run(() => SalesDataAccess.InsertSale(
                            projectId,
                            selectedPlot.PlotId,
                            buyerId,
                            sellerId,
                            amount,
                            null, 
                            dpDate.SelectedDate.Value,
                            Constants.StatusActive,
                            notes
                        ));
                    }
                }

                await LoadDataFromDatabase();
                MessageBox.Show("Ownership mapping saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnClear_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving ownership mapping: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
            }
        }

        private int GetProjectIdByPlotId(int plotId)
        {
            // Moved to PlotManagementDataAccess.GetProjectIdByPlotId
            return PlotManagementDataAccess.GetProjectIdByPlotId(plotId);
        }


        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _selectedMapping = null;
            if (cmbPlot != null) cmbPlot.SelectedIndex = -1;
            if (cmbBuyerSeller != null) cmbBuyerSeller.SelectedIndex = -1;
            if (cmbOwnershipType != null) cmbOwnershipType.SelectedIndex = -1;
            if (dpDate != null) dpDate.SelectedDate = DateTime.Now;
            if (txtAmount != null) txtAmount.Clear();
            if (txtNotes != null) txtNotes.Clear();
            if (dgOwnership != null) dgOwnership.SelectedItem = null;
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMapping == null)
            {
                MessageBox.Show("Please select an ownership mapping to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete the ownership mapping for plot '{_selectedMapping.PlotNo}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (!_isLoaded) return;
            btnDelete.IsEnabled = false;

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await Task.Run(() => SalesDataAccess.DeleteSale(_selectedMapping.SaleId));
                    await LoadDataFromDatabase();
                    BtnClear_Click(sender, e);
                    MessageBox.Show("Ownership mapping deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting ownership mapping: {ex.Message}", 
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

        private void DgOwnership_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (dgOwnership.SelectedItem is OwnershipMapping selectedMapping)
            {
                _selectedMapping = selectedMapping;
                
                var plot = _plots.FirstOrDefault(p => p.PlotNo == selectedMapping.PlotNo);
                if (plot != null)
                {
                    cmbPlot.SelectedItem = plot;
                }

                var party = _parties.FirstOrDefault(p => p.Name == selectedMapping.BuyerSellerName);
                if (party != null)
                {
                    cmbBuyerSeller.SelectedItem = party;
                }

                cmbOwnershipType.SelectedItem = selectedMapping.OwnershipType;
                dpDate.SelectedDate = selectedMapping.Date;
                txtAmount.Text = $"{selectedMapping.Amount:N2}";
                txtNotes.Text = selectedMapping.Notes;
            }
        }
    }
}

