
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Newtonsoft.Json.Linq;
using RevitParametersAddin.Services;
using RevitParametersAddin.TokenHandlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Shapes;
using DataGrid = System.Windows.Controls.DataGrid;

namespace RevitParametersAddin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string hubId;
        private string groupId;
        private string collId;
        private readonly string _token;
        UIApplication _app;

        static Parameters _param;

        public MainWindow(string threeleggedtoken, UIApplication app)
        {
            _token = threeleggedtoken;
            _app = app;

            _param = new Parameters();

            InitializeComponent();

            // Get Hub data on initial window 
            _param.GetHubs(_token).ContinueWith(taskhub =>
            {
                IEnumerable<dynamic> items = taskhub.Result;

                this.HubsList.ItemsSource = items;
                this.HubsList.DisplayMemberPath = "Item1";
                this.HubsList.SelectedValuePath = "Item2";
                this.HubsList.SelectedIndex = 0;
            },TaskScheduler.FromCurrentSynchronizationContext());

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            foreach (var column in dg.Columns)
            {
                column.Width = new DataGridLength(dg.ActualWidth / dg.Columns.Count, DataGridLengthUnitType.Pixel);
            }
        }
        public void GetACCParameters()
        {
            // Get selected Hub Id
            hubId = (string) this.HubsList.SelectedValue;

            // Get Group Id
            _param.GetGroups(hubId, _token).ContinueWith(groups =>
            {
                IEnumerable<dynamic> groupsItems = groups.Result;

                if(groupsItems.Count() > 0)
                {
                    groupId = groupsItems.OrderByDescending(x => x.Item1)
                    .Select(x => x.Item2)
                    .FirstOrDefault();

                    // Get Collections
                    _param.GetCollections(hubId, groupId, _token).ContinueWith(colls =>
                    {
                        IEnumerable<dynamic> collections = colls.Result;
                        this.CollectionList.ItemsSource = collections;
                        this.CollectionList.DisplayMemberPath = "Item1";
                        this.CollectionList.SelectedValuePath = "Item2";
                        this.CollectionList.SelectedIndex = 0;

                        // Get Collection Id of the first 
                        collId = (string) this.CollectionList.SelectedValue;

                        // Show parameters of the first Collection
                        _param.GetParameters(hubId, groupId, collId, _token).ContinueWith(paramst =>
                        {
                            // Clear the columns first
                            GridParameters.Columns.Clear();
                            GridParameters.ItemsSource = null;

                            if (paramst.Result.Count > 0)
                            {
                                // Set Parameters to ItemSource
                                GridParameters.ItemsSource = paramst.Result as List<Models.ParametersViewModel>;
                                GridParameters.Columns[4].MaxWidth = 0;
                            }
                            else
                            {
                                var empty = new List<dynamic>();
                                empty.Add(new Tuple<string>("Oooops! No Data was found in the selection that you made. Please change the Hub"));
                                GridParameters.ItemsSource = empty.ToList();
                            }
                        },
                        TaskScheduler.FromCurrentSynchronizationContext());
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    var empty = new List<dynamic>();
                    empty.Add(new Tuple<string>("Oooops! No Group was found in the Hub that you selected. Please change the Hub."));
                    GridParameters.ItemsSource = empty.ToList();
                }
                
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void HubsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetACCParameters();
        }

        private async void CollectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Parameters param = new Parameters();

            //Get the ID of the collection
            var Id = CollectionList.SelectedItem.GetType().GetProperty("Item2");
            var collId = (String)(Id.GetValue(CollectionList.SelectedItem, null));
            GridParameters.CanUserAddRows = false;
            //initialize the datagrid table and clear any records            
            GridParameters.Columns.Clear();
            GridParameters.ItemsSource = null;
            //Initialize empty row for the datagrid table
            var _datagrid = new List<dynamic>();
            _datagrid.Add(new Tuple<string>("Loading parameters..."));
            GridParameters.ItemsSource = _datagrid.ToList();
            
            //Get all the Parameters            
            var paramst = await param.GetParameters(hubId, groupId, collId, _token);
            if (paramst.Count > 0)
            {
                // Set Parameters to ItemSource
                GridParameters.ItemsSource = paramst as List<Models.ParametersViewModel>;
                GridParameters.Columns[4].MaxWidth = 0;
            }
            else
            {
                _datagrid.Clear();

                //If no data was found in the collection
                _datagrid.Add(new Tuple<string>("Oooops! No Data was found in the selection that you made. Please change the Collection or Hub"));
                GridParameters.ItemsSource = _datagrid.ToList();
            }
        }
        private void AddSharedParameter(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                AddSharedParameter subWindow = new AddSharedParameter();
                var row_list = GetDataGridRows(GridParameters);
                int countAddedParameters = 0;
                foreach (DataGridRow single_row in row_list)
                {
                    if (single_row.IsSelected == true)
                    {
                        var ParameterName = single_row.Item.GetType().GetProperty("Name");
                        var ParameterId = single_row.Item.GetType().GetProperty("Id");
                        if(subWindow.AddProjParameter(_app, (String)(ParameterId.GetValue(single_row.Item, null))))
                        {
                            countAddedParameters = countAddedParameters + 1;
                        }
                    }
                }
                TaskDialog.Show("Revit", countAddedParameters + " Parameter(s) Applied");
                this.ShowDialog();
            }
            catch(Exception ex) {
                this.ShowDialog(); 
                TaskDialog.Show("Revit", "Error Occured : "+ex.Message);
            }
        }
        public IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
            }
        }
    }
}
