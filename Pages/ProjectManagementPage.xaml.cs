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
    /// Interaction logic for ProjectManagementPage.xaml
    /// </summary>
    public partial class ProjectManagementPage : Page
    {
        public class Project
        {
            public int ProjectIdDb { get; set; }
            public string ProjectId { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        private List<Project> _projects = new();
        private bool _isLoaded = false;

        public ProjectManagementPage()
        {
            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadDataFromDatabase();
                if (cmbStatus != null) cmbStatus.ItemsSource = new List<string> { "Active", "In Progress", "Completed", "On Hold" };
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDataFromDatabase()
        {
            try
            {
                if (dgProjects != null) dgProjects.IsEnabled = false;

                var projectList = await Task.Run(() => ProjectDataAccess.GetAllProjects());
                var mappedProjects = new List<Project>();
                
                foreach (var p in projectList)
                {
                    if (int.TryParse(p.ProjectId, out int idDb))
                    {
                        mappedProjects.Add(new Project
                        {
                            ProjectIdDb = idDb,
                            ProjectId = "PRJ" + p.ProjectId.PadLeft(3, '0'),
                            ProjectName = p.ProjectName,
                            Location = p.Location,
                            Status = p.Status
                        });
                    }
                }

                _projects = mappedProjects;
                if (dgProjects != null)
                {
                    dgProjects.ItemsSource = null;
                    dgProjects.ItemsSource = _projects;
                    dgProjects.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (dgProjects != null) dgProjects.IsEnabled = true;
                MessageBox.Show($"Error loading projects: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            if (string.IsNullOrWhiteSpace(txtProjectName.Text))
            {
                MessageBox.Show("Please enter Project Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSave.IsEnabled = false;

            try
            {
                string projectName = txtProjectName.Text.Trim();
                string location = txtLocation.Text?.Trim() ?? "";
                string status = cmbStatus.SelectedItem?.ToString() ?? Constants.StatusActive;

                if (_selectedProject != null)
                {
                    // Update existing
                    await Task.Run(() => ProjectDataAccess.UpdateProject(
                        _selectedProject.ProjectIdDb,
                        projectName,
                        location,
                        status
                    ));
                }
                else
                {
                    // Add new
                    await Task.Run(() => ProjectDataAccess.InsertProject(
                        projectName,
                        location,
                        status
                    ));
                }

                await LoadDataFromDatabase();
                MessageBox.Show("Project saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnClear_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
            }
        }


        private Project? _selectedProject;

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            txtProjectId.Clear();
            txtProjectName.Clear();
            txtLocation.Clear();
            cmbStatus.SelectedIndex = -1;
            if (dgProjects != null)
            {
                dgProjects.SelectedItem = null;
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("Please select a project to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete project '{_selectedProject.ProjectName}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (!_isLoaded) return;
            btnDelete.IsEnabled = false;

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await Task.Run(() => ProjectDataAccess.DeleteProject(_selectedProject.ProjectIdDb));
                    await LoadDataFromDatabase();
                    BtnClear_Click(sender, e);
                    MessageBox.Show("Project deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    MessageBox.Show("Cannot delete project because it is in use by plots or sales records.", "Dependency Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void DgProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (dgProjects.SelectedItem is Project selectedProject)
            {
                _selectedProject = selectedProject;
                txtProjectId.Text = selectedProject.ProjectId;
                txtProjectName.Text = selectedProject.ProjectName;
                txtLocation.Text = selectedProject.Location;
                
                // Find and select the status in the combobox
                if (cmbStatus.ItemsSource is List<string> statusList)
                {
                    var statusIndex = statusList.IndexOf(selectedProject.Status);
                    if (statusIndex >= 0)
                    {
                        cmbStatus.SelectedIndex = statusIndex;
                    }
                    else
                    {
                        cmbStatus.SelectedIndex = -1;
                    }
                }
            }
            else
            {
                // Clear selection
                _selectedProject = null;
            }
        }
    }
}



