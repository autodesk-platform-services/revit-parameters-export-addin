using Autodesk.Revit.UI;
using RevitParametersAddin.Models;
using RevitParametersAddin.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RevitParametersAddin
{
    /// <summary>
    /// Interaction logic for ParametersWindow.xaml
    /// </summary>
    public partial class ParametersWindow : Window
    {
        private readonly Parameters param = new Parameters();
        private readonly string token;

        private readonly string account_id;
        private readonly string group_id;
        private readonly string collection_id;

        public ParametersWindow(string _account_id, string _group_id, string _collection_id, string _token)
        {
            token = _token;
            account_id = _account_id;
            group_id = _group_id;
            collection_id = _collection_id;
            InitializeComponent();
        }

        private async void AddNewParameter(object sender, RoutedEventArgs e)
        {
            string parameterName = ParameterName.Text;
            await param.CreateParameter(account_id, collection_id, parameterName, token);
        }
    }
}
