using System.Windows;
using System.Windows.Controls;
using devDept.Eyeshot.Entities;

namespace EyeshotDesignTestApp
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button2_OnClick(object sender, RoutedEventArgs e)
        {

            var view = Stuff.Uc2;

            ViewHost.Content = null;
            view.DataContext = Stuff.Uc2Vm;

            ViewHost.Content = view;
        }

        private void Button3_OnClick(object sender, RoutedEventArgs e)
        {
            var view = Stuff.Uc3;

            ViewHost.Content = null;
            view.DataContext = Stuff.Uc3Vm;

            ViewHost.Content = view;
        }
    }
    public static class Stuff
    {
        /// <summary>
        /// Reproduce the issue:
        /// - Click on "Button Not working"
        /// - Click on ZoomFit button
        /// - Switch to Tab B
        /// - Switch back to Tab A
        /// - Click on ZoomFit button
        /// --> see the exception: System.Collections.Generic.KeyNotFoundException: 'The given key 'Tahoma' was not present in the dictionary.' 	
        /// </summary>
        public static UserControl Uc2 = new UserControl2();
        public static UserControl2ViewModel Uc2Vm = new UserControl2ViewModel ( Mesh.CreateSphere( 0.8,512, 512 ), new Text(0,0,0,"SomeText",1));

        /// <summary>
        /// Reproduce the issue:
        /// - Click on "Button Working"
        /// - Click on ZoomFit button
        /// - Switch to Tab B
        /// - Switch back to Tab A
        /// - Click on ZoomFit button
        /// --> no exception
        /// </summary>
        public static UserControl Uc3 = Uc2;
        public static UserControl2ViewModel Uc3Vm = new UserControl2ViewModel ( Mesh.CreateSphere( 0.8,512, 512 ));
    }
}
