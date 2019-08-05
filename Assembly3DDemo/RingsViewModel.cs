using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Weingartner.Eyeshot.Assembly3D;
using Weingartner.EyeShot.Assembly3D;
using Environment = devDept.Eyeshot.Environment;

namespace Assembly3DDemo
{
    public class RingsViewModel : Assembly3D
    {
        public  Model Model;

        private double _Angle = 0.0;

        public IDisposable Animate()
        {
            var frameRate = 50;
            var period = 1.0 / frameRate;
            var rotation = 1.0; // radian per second
            return Observable.Interval(TimeSpan.FromSeconds(period), RxApp.MainThreadScheduler)
                             .ObserveOn(this, false)
                             .Subscribe
                             (t =>
                             {
                                 _Angle += rotation * period;
                                 Ring0.Transformation = new Rotation(_Angle, Vector3D.AxisZ);
                                 Ring1.Transformation = new Rotation(-_Angle, Vector3D.AxisZ) 
                                                        * new Rotation(Math.PI/2,Vector3D.AxisX) 
                                                        * new Translation(20 * Vector3D.AxisX);
                                 Model.Invalidate();

                             });

        }




        private readonly SerialDisposable _AnimationControl = new SerialDisposable();
        public ReactiveCommand<Unit,IDisposable> Start { get; }
        public ReactiveCommand<Unit,IDisposable>  Stop { get; }
        public ReactiveCommand<Unit,Unit>  PopSelectionCommand { get; }
        [Reactive] public bool IsSphere { get; set;}

        public ReactiveCommand<Unit,Unit>  ClearSelections { get; }
        [Reactive] public int SelectedRingIndex { get; set;}
        [Reactive] public int SelectionModeIndex{ get; set;}

        /// <summary>
        /// Create a command that will execute the action under the scope of
        /// regenerating the scene correctly.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public ReactiveCommand<Unit,Unit>  CreateEyeshotCommand(Action a)=>ReactiveCommand.Create(()=>ActiveRing.Invoke(a, true));

        public RingsViewModel(Model viewportLayout)
        {
            Model = viewportLayout;
            var rings = new Assembly3D();

            Start = ReactiveCommand.Create(()=>_AnimationControl.Disposable = Animate());
            Stop = ReactiveCommand.Create(()=>_AnimationControl.Disposable = Disposable.Empty);
            ClearSelections = CreateEyeshotCommand
                (() => ActiveRing.Block.Entities.ForEach(e => e.SetSelection(false, ActiveRing.BlockReferenceStack)));


            Ring0 = new Ring();
            Ring1 = new Ring
                    {
                        Transformation =
                            new Rotation(Math.PI / 2, Vector3D.AxisX) * new Translation(20 * Vector3D.AxisX)
                    };


            rings.Add(Ring0);
            rings.Add(Ring1);

            this.WhenAnyValue(p => p.IsSphere)
                .ObserveOn(this, false)
                .Subscribe
                (v =>
                {
                    Ring0.IsSphere = v;
                    Ring1.IsSphere = v;
                });

            Add(rings);

            ActiveRing = Ring0;

            PopSelectionCommand =CreateEyeshotCommand(PopSelection);


            this.WhenAnyValue(p => p.SelectedRingIndex)
                .Subscribe
                (i =>
                {
                    Ring0.Active = false;
                    Ring1.Active = false;
                    ActiveRing = i == 0 ? Ring0 : Ring1;
                    ActiveRing.Active = true;
                });
        }


        [Reactive] public Ring Ring0 { get; set; }
        [Reactive] public Ring Ring1 { get; set; }
        [Reactive] public Ring ActiveRing { get; set; }

        private Stack<Environment.SelectionChangedEventArgs> SelectionStack = new Stack<Environment.SelectionChangedEventArgs>();

        [Reactive] public Environment.SelectionChangedEventArgs CurrentSelection { get; set;}
        public void PushSelection(Environment.SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if(CurrentSelection!=null)
                SelectionStack.Push(CurrentSelection);
            CurrentSelection = selectionChangedEventArgs;
        }

        // Pop a selection off the stack and make the current 
        // viewport selection equal to it.
        private void PopSelection()
        {
            // Clear all the current selections
            ActiveRing.Block.Entities.ForEach(e=>e.ClearSelectionForAllInstances());
            ActiveRing.Block.Entities.ForEach(e=>(e as IFaceSelectable)?.ClearFacesSelectionForAllInstances());

            if (SelectionStack.Count == 0)
                return;

            // Get the selection off the stack
            var s = SelectionStack.Pop();
            CurrentSelection = s;

            if (s.AddedItems.Count == 0)
                return;

            // Assume only one item is selected
            var selectableItem = s.AddedItems[0];

            // Check if it is face selectable and process accordingly
            var subSelectableItem = selectableItem as Environment.SelectedSubItem;
            if (subSelectableItem != null)
            {
                var faceSelectable = (selectableItem.Item as IFaceSelectable);
                faceSelectable?.SetFaceSelection(subSelectableItem.Index, true, selectableItem.Parents);
            }
            // Otherwise treat it as an entity selection
            else
            {
                var entity = selectableItem.Item as Entity;
                entity?.SetSelection(true, selectableItem.Parents);
            }
        }

    }

}