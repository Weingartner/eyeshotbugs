using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using ReactiveUI;
using Weingartner.EyeShot.Assembly3D;

namespace EyeshotTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();

            this.WhenActivated( Init );

        }

        private void Init( CompositeDisposable obj )
        {
            //Bind the ReactiveWindow ViewModel Property to the DataContext
            this.WhenAnyValue( p => p.ViewModel )
                .BindTo( this, view => view.DataContext );
        }


    }
}
