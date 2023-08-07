#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitParametersAddin.TokenHandlers;
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
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            string userId = app.LoginUserId;
            //string userName = app.Username;

            var view = doc.ActiveView;
            var vName = view.Name;
            var vId=view.Id;
            var vTemplateId = view.ViewTemplateId;
            var vTemplate = vTemplateId.ToString() == "-1" ? "None" : doc.GetElement(vTemplateId).Name;

            var token = TokenHandler.Login();
            string _token = token.ToString();

            MainWindow window = new MainWindow(_token, uiapp);
            window.ShowDialog();
            
            return Result.Succeeded;
        }

    }    
}
