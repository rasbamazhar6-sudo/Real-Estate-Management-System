using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Project.Data;
using Project.Services;

namespace Project.Pages
{
    public partial class ReceivablesAgingReportPage : Page
    {
        private List<CustomerInfo> _customers = new();
        private List<AgingReportItem> _agingReport = new();
        
        public ReceivablesAgingReportPage()
        {
            
            InitializeComponent();
            LoadCustomers();
            dpAsOfDate.SelectedDate = DateTime.Now;
            btnExportPDF.IsEnabled = false;
            btnExportExcel.IsEnabled = false;
        }

        private void LoadCustomers()
        {
            try
            {
                var parties = PartyDataAccess.GetAllParties();

                var customers = parties.Select(p => new CustomerInfo
                {
                    PartyId = p.PartyId,
                    Name = p.Name ?? ""
                }).ToList();

                // Add "All Customers"
                customers.Insert(0, new CustomerInfo
                {
                    PartyId = 0,
                    Name = "All Customers"
                });

                // 🔥 CRITICAL: Reset binding properly
                cmbCustomer.ItemsSource = null;

                cmbCustomer.ItemsSource = customers;
                cmbCustomer.DisplayMemberPath = "Name";
                cmbCustomer.SelectedValuePath = "PartyId";

                cmbCustomer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}");
            }
        }

        private class CustomerInfo
        {
            public int PartyId { get; set; }
            public string Name { get; set; } = string.Empty;
        }
        
        private void DpAsOfDate_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (dpAsOfDate.SelectedDate.HasValue)
            {
                ResetReportState();
            }
        }

        private void CmbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetReportState();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport();

            btnExportPDF.IsEnabled = _agingReport.Any();
            btnExportExcel.IsEnabled = _agingReport.Any();
        }

        private void ResetReportState()
        {

            btnExportPDF.IsEnabled = false;
            btnExportExcel.IsEnabled = false;

            dgAgingReport.ItemsSource = null;

            txtCurrent.Text = "PKR 0.00";
            txtDays31to60.Text = "PKR 0.00";
            txtDays61to90.Text = "PKR 0.00";
            txtOver90.Text = "PKR 0.00";
        }

        private void GenerateReport()
        {
            try
            {
                _agingReport.Clear();
                var asOfDate = dpAsOfDate.SelectedDate ?? DateTime.Now;

                // Get selected customer (if any)
                int? selectedPartyId = null;

                if (cmbCustomer.SelectedItem is CustomerInfo selectedCustomer && selectedCustomer.PartyId != 0)
                {
                    selectedPartyId = selectedCustomer.PartyId;
                }

                // Load aging report from database
                var dbReport = LedgerDataAccess.GetReceivablesAgingReport(asOfDate, selectedPartyId);
                
                _agingReport = dbReport.Select(r => new AgingReportItem
                {
                    PartyId = r.PartyId,
                    CustomerName = r.CustomerName,
                    TotalOutstanding = r.TotalOutstanding,
                    Current = r.Current,
                    Days31to60 = r.Days31to60,
                    Days61to90 = r.Days61to90,
                    Over90 = r.Over90,
                    OldestInvoiceDate = r.OldestInvoiceDate
                }).ToList();
                
                UpdateSummaryCards();
                dgAgingReport.ItemsSource = _agingReport;


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateSummaryCards()
        {
            var current = _agingReport.Sum(r => r.Current);
            var days31to60 = _agingReport.Sum(r => r.Days31to60);
            var days61to90 = _agingReport.Sum(r => r.Days61to90);
            var over90 = _agingReport.Sum(r => r.Over90);
            
            txtCurrent.Text = $"PKR {current:N2}";
            txtDays31to60.Text = $"PKR {days31to60:N2}";
            txtDays61to90.Text = $"PKR {days61to90:N2}";
            txtOver90.Text = $"PKR {over90:N2}";
        }
        
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_agingReport == null || !_agingReport.Any())
            {
                MessageBox.Show("No data to export.");
                return;
            }

            ExportService.ExportToExcel(
                            dgAgingReport,
                            $"ReceivablesAging_{DateTime.Now:yyyyMMdd}.xlsx"
                        );
        }
        
        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_agingReport == null || !_agingReport.Any())
            {
                MessageBox.Show("No data to export.");
                return;
            }

            ExportService.ExportToPDF(
                            dgAgingReport,
                            "Receivables Aging Report",
                            $"ReceivablesAging_{DateTime.Now:yyyyMMdd}.pdf"
                        );
        }
        
        private class AgingReportItem
        {
            public int PartyId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal TotalOutstanding { get; set; }
            public decimal Current { get; set; }
            public decimal Days31to60 { get; set; }
            public decimal Days61to90 { get; set; }
            public decimal Over90 { get; set; }
            public DateTime OldestInvoiceDate { get; set; }
        }
    }
}
