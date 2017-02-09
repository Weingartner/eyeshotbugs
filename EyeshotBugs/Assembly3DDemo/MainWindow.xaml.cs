using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using devDept.Eyeshot;
using ReactiveUI;
using Weingartner.Eyeshot.Assembly3D;

namespace Assembly3DDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public static readonly DependencyProperty RingsViewModelProperty = DependencyProperty.Register
            (
            "RingsViewModel",
            typeof(RingsViewModel),
            typeof(MainWindow),
            new PropertyMetadata(default(RingsViewModel)));

        public RingsViewModel RingsViewModel { get { return (RingsViewModel) GetValue(RingsViewModelProperty); } set { SetValue(RingsViewModelProperty, value); } }

        public MainWindow()
        {
            InitializeComponent();

            RingsViewModel = new RingsViewModel();

            // Bind the assembly property to the viewport.
            // Use the LoadUnloadHandler to dispose / decompile the
            // binding when the control is not visible. This prevents
            // memory leaks
            this.LoadUnloadHandler
                (() => ViewportLayout.BindToViewport(this.WhenAnyValue(p => p.RingsViewModel)));
        }
    }
}
