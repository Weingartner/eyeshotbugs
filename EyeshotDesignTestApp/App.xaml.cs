using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EyeshotDesignTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //devDept.LicenseManager.Unlock("EU23-9V6CU-612NQ-VQ6C-RX7H");
            base.OnStartup( e );
        }
    }
    
}
