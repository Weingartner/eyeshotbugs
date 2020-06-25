using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Eyeshot.Translators;
using devDept.Serialization;

namespace Weingartner.EyeShot.Assembly3D
{
    public static class ModelExtensions
    {
        public static void Save(this Model model)
        {
            using (var saveFileDialog = new System.Windows.Forms.SaveFileDialog
                                       {
                                           Filter = "Eyeshot (*.eye)|*.eye"
                                          ,
                                           AddExtension = true
                                          ,
                                           CheckPathExists = true
                                       })
            {

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    model.Save(saveFileDialog.FileName);
                }
            }
        }

        public static void Save(this Model model, string fileName)
        {
            try
            {
                var saveFile  = Path.ChangeExtension(fileName, ".eye");
                var writeFile = new WriteFile(new WriteFileParams(model) { Purge = false }, saveFile);
                model.DoWork(writeFile);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}