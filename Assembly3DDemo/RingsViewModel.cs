using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using devDept.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Weingartner.Eyeshot.Assembly3D;

namespace Assembly3DDemo
{
    public class RingsViewModel : Assembly3D
    {

        private double _Angle = 0.0;

        public IDisposable Animate()
        {
            var frameRate = 50;
            var period = 1.0 / frameRate;
            var rotation = 1.0; // radian per second
            return Observable.Interval(TimeSpan.FromSeconds(period), RxApp.MainThreadScheduler)
                             .ObserveOn(this)
                             .Subscribe
                             (t =>
                             {
                                 _Angle += rotation * period;
                                 Ring0.Transformation = new Rotation(_Angle, Vector3D.AxisZ);
                                 Ring1.Transformation = new Rotation(-_Angle, Vector3D.AxisZ) 
                                                        * new Rotation(Math.PI/2,Vector3D.AxisX) 
                                                        * new Translation(20 * Vector3D.AxisX);
                             });

        }


        private readonly SerialDisposable _AnimationControl = new SerialDisposable();
        public ReactiveCommand Start { get; }
        public ReactiveCommand Stop { get; }
        [Reactive] public bool IsSphere { get; set;}

        public RingsViewModel()
        {
            var rings = new Assembly3D();

            Start = ReactiveCommand.Create(()=>_AnimationControl.Disposable = Animate());
            Stop = ReactiveCommand.Create(()=>_AnimationControl.Disposable = Disposable.Empty);

            Ring0 = new Ring();
            Ring1 = new Ring();

            Ring1.Transformation = new Rotation(Math.PI/2,Vector3D.AxisX) * new Translation(20 * Vector3D.AxisX);

            rings.Add(Ring0);
            rings.Add(Ring1);

            this.WhenAnyValue(p => p.IsSphere)
                .ObserveOn(rings)
                .Subscribe
                (v =>
                {
                    Ring0.IsSphere = v;
                    Ring1.IsSphere = v;
                });

            Add(rings);
        }

        [Reactive] public Ring Ring0 { get; set; }
        [Reactive] public Ring Ring1 { get; set; }
    }

}