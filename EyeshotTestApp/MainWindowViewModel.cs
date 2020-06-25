using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
    public class MainWindowViewModel : ReactiveObject
    {

        [Reactive] public Assembly3D ViewAssembly3D { get; set; }
        public ReactiveCommand<Unit,Unit> AddModelCommand { get; set; }

        private readonly ISubject<Unit> _ResetZoomObservable = new BehaviorSubject<Unit>( Unit.Default );
        public IObservable<Unit> ResetZoomObservable => _ResetZoomObservable;

        public MainWindowViewModel()
        {
            this.WhenAnyValue(p => p.ViewAssembly3D)
                .Select(_ => Unit.Default)
                .Subscribe(_ResetZoomObservable);

            AddModelCommand = ReactiveCommand.Create
                ( () =>
                {
                    ViewAssembly3D = SomeModel();

                });

        }

        private static Assembly3D SomeModel()
        {
            var res = new Assembly3D();
            var lp = LinearPath.CreateHelix( 0.005, 0.5, 10, false, 1e-4 );
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