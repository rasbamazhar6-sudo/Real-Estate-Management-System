using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Project.Data;

namespace Project.Pages
{
    /// <summary>
    /// Interaction logic for PlotVisualDashboardPage.xaml
    /// </summary>
    public partial class PlotVisualDashboardPage : Page
    {
        public class PlotItem
        {
            public string PlotNo { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public decimal Size { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public Brush StatusColor
            {
                get
                {
                    return Status switch
                    {
                        "Available" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                        "Reserved" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                        "Sold" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                        "Booked" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"))
                    };
                }
            }
            public Brush TextColor => Status == "Available" || Status == "Booked" ? Brushes.White : Brushes.Black;
        }

        private List<PlotItem> _allPlots = new();

        public class ProjectFilterItem
        {
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
        }

        public PlotVisualDashboardPage()
        {
            InitializeComponent();
            try
            {
                LoadProjects();
                LoadDataFromDatabase();
                LoadPlots();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProjects()
        {
            try
            {
                var projects = ProjectDataAccess.GetAllProjects();
                var projectList = new List<ProjectFilterItem> 
                { 
                    new ProjectFilterItem { ProjectId = 0, ProjectName = "All Projects" }
                };
                projectList.AddRange(projects.Select(p => new ProjectFilterItem 
                { 
                    ProjectId = int.Parse(p.ProjectId),
                    ProjectName = p.ProjectName 
                }));

                if (cmbProject != null)
                {
                    cmbProject.ItemsSource = projectList;
                    cmbProject.DisplayMemberPath = "ProjectName";
                    cmbProject.SelectedValuePath = "ProjectId";
                    cmbProject.SelectedIndex = 0;
                }
                
                cmbStatusFilter.ItemsSource = new List<string> { "All Status", "Available", "Reserved", "Sold", "Booked" };
                cmbStatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                var plotList = PlotManagementDataAccess.GetAllPlots();
                _allPlots = plotList.Select(p => new PlotItem
                {
                    PlotNo = p.PlotNo,
                    Status = p.Status,
                    Size = p.SizeMarla,
                    ProjectName = p.ProjectName
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading plots: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadPlots()
        {
            if (_allPlots == null || cmbProject == null || cmbStatusFilter == null || plotGrid == null)
                return;

            var filteredPlots = _allPlots.AsEnumerable();

            // Filter by project
            if (cmbProject.SelectedItem is ProjectFilterItem selectedProject)
            {
                if (selectedProject.ProjectId != 0 && !string.IsNullOrEmpty(selectedProject.ProjectName))
                {
                    filteredPlots = filteredPlots.Where(p => p.ProjectName == selectedProject.ProjectName);
                }
            }

            // Filter by status
            if (cmbStatusFilter.SelectedItem != null && cmbStatusFilter.SelectedItem.ToString() != "All Status")
            {
                filteredPlots = filteredPlots.Where(p => p.Status == cmbStatusFilter.SelectedItem.ToString());
            }

            plotGrid.ItemsSource = filteredPlots.ToList();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDataFromDatabase();
                LoadPlots();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing plots: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CmbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && cmbProject?.SelectedItem != null)
                LoadPlots();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && cmbStatusFilter?.SelectedItem != null)
                LoadPlots();
        }
    }
}

