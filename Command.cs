#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitParametersAddin.TokenHandlers;
using System;
using Application = Autodesk.Revit.ApplicationServices.Application;
#endregion

namespace RevitParametersAddin
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            try
            {
                var token = TokenHandler.Login();
                string _token = token.ToString();

                MainWindow window = new MainWindow(_token, uiapp);
                window.ShowDialog();
                return Result.Succeeded;
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return Result.Failed;
            }
        }

    }    
}
