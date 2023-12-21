using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace RevitParametersAddin.Services
{
    public class AddSharedParameter
    {
        public bool AddProjParameter(UIApplication uiapp, string parameterId)
        {
            try
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Application app = uiapp.Application;
                Document doc = uidoc.Document;
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Transaction Download Parameters");
                    ForgeTypeId forgeTypeId = new ForgeTypeId(parameterId);
                    ParameterDownloadOptions parameterDownloadOptions = ParameterUtils.DownloadParameterOptions(forgeTypeId);
                    ParameterUtils.DownloadParameter(doc, parameterDownloadOptions, forgeTypeId);

                    tx.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                //TaskDialog.Show("Error", ex.Message);
                return false;
            }
        }
        public bool AddProjectParameter(UIApplication uiapp, Element ele, string groupName, string name)
        {
            try
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Application app = uiapp.Application;
                Document doc = uidoc.Document;

                // buffer the current shared parameter file name and apply a new empty parameter file instead
                string sharedParameterFile = app.SharedParametersFilename;
                string tempSharedParameterFile = Path.GetTempFileName() + ".txt";
                using (File.Create(tempSharedParameterFile)) { }
                app.SharedParametersFilename = tempSharedParameterFile;


                DefinitionFile sharedParamsFile = app.OpenSharedParameterFile();
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Bind Parameter");
                    DefinitionGroup sharedParamsGroup = sharedParamsFile.Groups.get_Item(groupName);
                    if (null == sharedParamsGroup)
                    {
                        sharedParamsGroup = sharedParamsFile.Groups.Create(groupName);
                    }

                    Definition parameterDef = sharedParamsGroup.Definitions.get_Item(name);
                    if (null == parameterDef)
                    {
                        ExternalDefinitionCreationOptions opt = new ExternalDefinitionCreationOptions(name, SpecTypeId.String.Text); // 2015
                        opt.Visible = true;
                        parameterDef = sharedParamsGroup.Definitions.Create(opt);
                    }

                    CategorySet categorySet = app.Create.NewCategorySet();

                    Category categorySelectedElement = ele.Category;
                    categorySet.Insert(categorySelectedElement); // Add more categories if needed
                    Autodesk.Revit.DB.Binding binding = app.Create.NewInstanceBinding(categorySet);

                    var my_forge_type_id2 = GroupTypeId.Data;

                    doc.ParameterBindings.Insert(parameterDef, binding, my_forge_type_id2);

                    // apply old shared parameter file
                    app.SharedParametersFilename = sharedParameterFile;

                    // delete temp shared parameter file
                    System.IO.File.Delete(tempSharedParameterFile);

                    t.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return false;
            }
        }


        public bool AddParameterToFile(UIApplication uiapp, string filePath, Element ele, string groupName, string name)
        {
            try
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Application app = uiapp.Application;
                Document doc = uidoc.Document;

                //open a shared parameters filename
                app.SharedParametersFilename = filePath;
                app.OpenSharedParameterFile();

                DefinitionFile defFile = app.OpenSharedParameterFile();
                var my_forge_type_id2 = GroupTypeId.Data;
                var type = new ForgeTypeId { };


                //Open shared parameter file
                DefinitionFile parafile = app.OpenSharedParameterFile();

                // Now, add the shared parameter to the desired categories (e.g., Walls)
                Category categorySelectedElement = ele.Category;
                CategorySet categories = app.Create.NewCategorySet();
                categories.Insert(categorySelectedElement); // Add more categories if needed

                InstanceBinding binding = app.Create.NewInstanceBinding(categories);

                // Associate the shared parameter with the selected categories
                using (Transaction transaction = new Transaction(doc, "Associate Shared Parameter"))
                {
                    transaction.Start();
                    //Create a group
                    DefinitionGroup sharedParamsGroup = parafile.Groups.get_Item(groupName);
                    if (null == sharedParamsGroup)
                    {
                        sharedParamsGroup = parafile.Groups.Create(groupName);
                    }

                    BindingMap bindingMap = doc.ParameterBindings;
                    //Create a invisible "InvisibleParam" of text type.
                    ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions2 = new ExternalDefinitionCreationOptions(name, SpecTypeId.String.Text);
                    Definition invisibleParamDef = sharedParamsGroup.Definitions.Create
                        (ExternalDefinitionCreationOptions2);
                    bindingMap.Insert(invisibleParamDef, binding);

                    transaction.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return false;
            }
        }

        public static Element GetSingleSelectedElementOrPrompt(UIDocument uidoc)
        {
            Element e = null;
            ICollection<ElementId> ids = uidoc.Selection.GetElementIds();

            try
            {
                Reference r = uidoc.Selection.PickObject(
                  ObjectType.Element,
                  "Please pick an element");
                e = uidoc.Document.GetElement(r);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
            return e;
        }
    }
}
