using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Weingartner.EyeShot.Assembly3D;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Color = System.Drawing.Color;


namespace EyeshotTestApp
{
    public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; }
        [Reactive] public Assembly3D ViewAssembly3D { get; set; }
        public ReactiveCommand<Unit,Unit> AddModelCommand { get; set; }

        readonly ObservableAsPropertyHelper<List<Entity>> _EntityList;
        public   List<Entity>                             EntityList => _EntityList.Value;

        readonly ObservableAsPropertyHelper<List<Block>> _BlockList;
        public   List<Block>                             BlockList => _BlockList.Value;

        private readonly ISubject<Unit> _ResetZoomObservable = new BehaviorSubject<Unit>( Unit.Default );
        public IObservable<Unit> ResetZoomObservable => _ResetZoomObservable;


        public MainWindowViewModel()
        {
            Activator = new ViewModelActivator();
            this.WhenActivated( Init );

            this.WhenAnyValue( p => p.ViewAssembly3D )
                .WhereNotNull()
                .Select( a => a.AssemblyModel.RootBlock.Entities.ToList() )
                .ToProperty( this, p => p.EntityList, out _EntityList );

            this.WhenAnyValue( p => p.ViewAssembly3D )
                .WhereNotNull()
                .Select( a => a.AssemblyModel.Blocks.ToList() )
                .ToProperty( this, p => p.BlockList, out _BlockList );

            AddModelCommand = ReactiveCommand.Create
                ( () =>
                {
                    ViewAssembly3D = SomeModel();

                });

        }

        private void Init( CompositeDisposable disposables)
        {
            var assemblyObservable = this.WhenAnyValue(p => p.ViewAssembly3D);
            assemblyObservable.Select(_ => Unit.Default)
                              .Subscribe(_ResetZoomObservable)
                              .DisposeWith(disposables);

        }

        private static Assembly3D SomeModel()
        {
            var res = new Assembly3D();
            var lp = LinearPath.CreateHelix( 0.005, 0.5, 5, false, 1e-4 );
            var rail = Curve.CubicSplineInterpolation( lp.Vertices );
            var plane = new Plane(rail.StartPoint,rail.StartTangent);
            var circle = Region.CreateCircle( plane, 0.03 );
            var helix = circle.SweepAsBrep( rail, 1e-4, sweepMethodType.RoadlikeTop );
            res.Add( new PlanarEntity( plane ), Color.Magenta );
            res.Add( helix, Color.Gray );
            return res;
        }
    }
}