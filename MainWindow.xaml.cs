
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
                this.HubsList.SelectedIndex = -1;
            },TaskScheduler.FromCurrentSynchronizationContext());

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
                                //GridParameters.Columns[2].MaxWidth = 0;
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

            //Get all the Parameters            
            var paramst = await param.GetParameters(hubId, groupId, collId, _token);

            //clear Previous records
            GridParameters.Columns.Clear();
            GridParameters.ItemsSource = null;

            GridParameters.Columns.Clear();
            GridParameters.ItemsSource = null;

            if (paramst.Count > 0)
            {
                // Set Parameters to ItemSource
                GridParameters.ItemsSource = paramst as List<Models.ParametersViewModel>;
            }
            else
            {
                var empty = new List<dynamic>();
                empty.Add(new Tuple<string>("Oooops! No Data was found in the selection that you made. Please change the Hub"));
                GridParameters.ItemsSource = empty.ToList();
            }
        }

        private void CreateNewParameter(object sender, RoutedEventArgs e)
        {
            try
            {
                string collectionId = hubId;
                if (CollectionList.SelectedIndex > -1)
                {
                    var Id = CollectionList.SelectedItem.GetType().GetProperty("Item2");
                    collectionId = (String)(Id.GetValue(CollectionList.SelectedItem, null));
                }
                ParametersWindow window = new ParametersWindow(hubId, hubId, collectionId, _token);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                this.ShowDialog();
                TaskDialog.Show("Revit", "Error Occured : " + ex.Message);
            }
        }

        private void ExportSharedParamToFile(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                AddSharedParameter subWindow = new AddSharedParameter();
                var row_list = GetDataGridRows(GridParameters);

                string collectionName = "Default Collection";
                if (CollectionList.SelectedIndex > -1)
                {
                    var Id = CollectionList.SelectedItem.GetType().GetProperty("Item1");
                    collectionName = (String)(Id.GetValue(CollectionList.SelectedItem, null));
                }

                //open a shared parameters filename
                System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
                FileInfo filePath = null;
                openFileDialog1.InitialDirectory = (@"C:\Temp");  // add your file path to the shared parameters file here:
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (string path in openFileDialog1.FileNames)
                    {
                        filePath = new FileInfo(path);
                    }
                }

                // Allow the user to pick a Revit object
                Reference pickedRef = _app.ActiveUIDocument.Selection.PickObject(ObjectType.Element);

                // Retrieve the Element based on the picked Reference
                Element ele = _app.ActiveUIDocument.Document.GetElement(pickedRef.ElementId);

                foreach (DataGridRow single_row in row_list)
                {
                    if (single_row.IsSelected == true)
                    {
                        var items = single_row.Item.GetType().GetProperty("Item1");
                        subWindow.AddParameterToFile(_app, filePath.FullName.ToString(), ele, collectionName, (String)(items.GetValue(single_row.Item, null)));
                    }
                }
                TaskDialog.Show("Revit", "Parameter Saved");
                this.ShowDialog();
            }
            catch (Exception ex)
            {
                this.ShowDialog();
                TaskDialog.Show("Revit", "Error Occured : "+ex.Message);
            }
        }
        private void AddSharedParameter(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                AddSharedParameter subWindow = new AddSharedParameter();

                var row_list = GetDataGridRows(GridParameters);

                //string collectionName = "Default Collection";
                //if (CollectionList.SelectedIndex > -1)
                //{
                //    var Id = CollectionList.SelectedItem.GetType().GetProperty("Item1");
                //    collectionName = (String)(Id.GetValue(CollectionList.SelectedItem, null));
                //}

                // Allow the user to pick a Revit object
                //Reference pickedRef = _app.ActiveUIDocument.Selection.PickObject(ObjectType.Element);

                // Retrieve the Element based on the picked Reference
                //Element ele = _app.ActiveUIDocument.Document.GetElement(pickedRef.ElementId);

                foreach (DataGridRow single_row in row_list)
                {
                    if (single_row.IsSelected == true)
                    {
                        var ParameterName = single_row.Item.GetType().GetProperty("Name");
                        var ParameterId = single_row.Item.GetType().GetProperty("Id");

                        subWindow.AddProjParameter(_app, (String)(ParameterId.GetValue(single_row.Item, null)));
                        //subWindow.AddProjectParameter(app, ele, collectionName, (String)(ParameterName.GetValue(single_row.Item, null)));
                    }
                }
                TaskDialog.Show("Revit", "Parameter Applied");
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
