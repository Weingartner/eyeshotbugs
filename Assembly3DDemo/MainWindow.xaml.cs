using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
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

            EyeshotBugs.Utils.Eyeshot.Unlock(ViewportLayout);

            RingsViewModel = new RingsViewModel(ViewportLayout);

            // Bind the assembly property to the viewport.
            // Use the LoadUnloadHandler to dispose / decompile the
            // binding when the control is not visible. This prevents
            // memory leaks
            this.LoadUnloadHandler
                (() =>
                {
                    // Bind the main viewmodel to the viewport
                    var c = new CompositeDisposable();
                    ViewportLayout.BindToViewport(this.WhenAnyValue(p => p.RingsViewModel))
                    .DisposeWith(c);

                    ViewportLayout.ActionMode = actionType.SelectVisibleByPick;

                    // Set the selection scope to the active ring
                    this.WhenAnyValue(p => p.RingsViewModel.ActiveRing, p => p.RingsViewModel.IsCompiled, (activeRing, isCompiled)=>new {activeRing, isCompiled})
                        .Where(o => o.isCompiled)
                        .Subscribe(o => ViewportLayout.SetSelectionScope(o.activeRing.BlockReferenceStack))
                        .DisposeWith(c);

                    this.WhenAnyValue(p => p.RingsViewModel.SelectionModeIndex)
                        .Subscribe
                        (i =>
                        {
                            switch (i)
                            {
                                case 0:
                                    ViewportLayout.SelectionFilterMode = selectionFilterType.Entity;
                                    break;
                                case 1:
                                    ViewportLayout.SelectionFilterMode = selectionFilterType.Face;
                                    break;
                            }

                        });

                    ViewportLayout
                        .SelectionChangedObservable()
                        .Subscribe
                        (s =>
                        {
                            Console.WriteLine("SelectionEvent");
                            Console.WriteLine($"   Added {String.Join(", ", s.AddedItems.Select(a=>$"{a.Item.GetHashCode()}"))}");
                            Console.WriteLine($"   Removed {String.Join(", ", s.RemovedItems.Select(a=>$"{a.Item.GetHashCode()}"))}");
                            RingsViewModel.PushSelection(s);

                        });

                    //ViewportLayout.Rendered.ShowEdges = false;
                    ViewportLayout.Viewports[0].DisplayMode = displayType.Rendered;

                    return (IDisposable)c;
                });
        }
    }
}
