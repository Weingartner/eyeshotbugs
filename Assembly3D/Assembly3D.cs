using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Labels;
using devDept.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Point = devDept.Eyeshot.Entities.Point;

namespace Weingartner.Eyeshot.Assembly3D
{
    /// <summary>
    /// This class represents an heirarchial assembly of objects in a cleaner way
    /// than EyeShot naturally allows. You don't have to manually create block
    /// references and add them to the viewport and manually refresh the viewport
    /// if you wish to change the transformations and animate.
    /// 
    /// Smooth animations are ensured by throttling and sampling cascades of
    /// updates in such a way that the resulting jitter is minimized. 
    /// 
    /// The pattern is to subclass Assembly and provide your own driving
    /// properties to control the Transformation properties of your
    /// sub assemblies.
    ///
    /// You can add and remove entities and sub assemblies from
    /// the assembly at runtime by calling the Add and Remove
    /// methods respectively.
    /// 
    /// If you want to change other properties of your entities
    /// at runtime other that the Transformation then you
    /// should call fireChanged after your changes are done
    /// and this will notify the viewport.
    /// 
    /// To add the root assembly to the ViewPort call method
    /// AddToViewPort
    /// </summary>
    public class Assembly3D : ReactiveObject
    {
        public ReactiveList<Assembly3D> SubAssemblies { get; }
        public ReactiveList<Label> Labels { get; }

        private readonly Subject<Assembly3D> _Changed = new Subject<Assembly3D>();

        public IObservable<Assembly3D> ChangedObservable => _Changed;

        public void FireChange(Assembly3D a)
        {
            _Changed.OnNext(a);
        }


        /// <summary>
        /// The transformation of this assembly relative to it's parent assembly or the
        /// viewport. Updating this property will automatically update the viewport. There
        /// is no need to manually invalidate the viewport.
        /// </summary>
        Transformation _Transformation;

        private Action<Action, bool> _Invoker;
        private readonly ISubject<int> _MouseEnterObservable = new Subject<int>();

        /// <summary>
        /// Run the action on the scheduler / task associated with the assembly.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="regen"></param>
        /// <returns></returns>
        public void Invoke(Action a, bool regen)
        {
            _Invoker.Invoke(() =>
            {
                AssertCorrectThread();
                a();
            }, regen);
        }

        public Transformation Transformation
        {
            get { return _Transformation; }
            set { AssertCorrectThread(); this.RaiseAndSetIfChanged(ref _Transformation, value); }
        }

        private void AssertCorrectThread()
        {
            if (IsCompiled)
                _AssemblyViewport.AssertCorrectThread();
        }

        public Assembly3D Replace(Assembly3D assembly3D)
        {
            Clear();
            Add(assembly3D);
            return this;
        }

        public Assembly3D Add(Entity e)
        {
            AssertCorrectThread();
            if (Block.Entities.Contains(e))
                throw new ArgumentException("Entity has allready been added once");

            Block.Entities.Add(e);
            FireChange(this);
            return this;
        }

        public void Remove(Entity e)
        {
            AssertCorrectThread();
            Block.Entities.Remove(e);
            FireChange(this);
        }

        public Assembly3D Clear()
        {
            AssertCorrectThread();
            Block.Entities.Clear();
            foreach (var a in SubAssemblies.ToList())
            {
                Remove(a);
            }

            foreach (var label in Labels.ToList())
            {
                Remove(label);
            }
            FireChange(this);
            return this;
        }

        /// <summary>
        /// Add a range of entities to the assembly
        /// </summary>
        /// <param name="es"></param>
        public Assembly3D Add(IEnumerable<Entity> es)
        {
            var enumerable = es as IList<Entity> ?? es.ToList();
            if (enumerable.Any(e => Block.Entities.Contains(e)))
                throw new Exception("Entity is allready included");
            Block.Entities.AddRange(enumerable);
            FireChange(this);
            return this;
        }
        public Assembly3D Add(IEnumerable<Entity> es, Color c, int weight = 1)
        {
            var enumerable = es as IList<Entity> ?? es.ToList();
            foreach (var entity in enumerable)
            {
                entity.Color = c;
                entity.ColorMethod = colorMethodType.byEntity;
                entity.LineWeight = weight;
                entity.LineWeightMethod = colorMethodType.byEntity;
            }
            Add(enumerable);
            return this;
        }

        public Assembly3D Add(params Entity[] es)
        {
            Add(es.ToList());
            return this;
        }

        /// <summary>
        /// Add a child assembly to this assembly. Transformations
        /// on the child assembly will be relative to the reference
        /// coordinate system of this ( the parent ) assembly.
        /// </summary>
        /// <param name="a"></param>
        public Assembly3D Add(Assembly3D a)
        {
            AssertCorrectThread();
            SubAssemblies.Add(a);
            return this;
        }

        public Assembly3D Add(Label label)
        {
            Labels.Add(label);
            return this;
        }

        public Assembly3D Add(IEnumerable<Label> labels)
        {
            foreach (var label in labels)
            {
                Labels.Add(label);
            }
            return this;
        }


        public void Remove(Label a)
        {
            Labels.Remove(a);
        }

        /// <summary>
        /// Add a range of assemblies to the current assembly.
        /// </summary>
        /// <param name="es"></param>
        public void Add(IEnumerable<Assembly3D> es)
        {
            SubAssemblies.AddRange(es);
        }
        public void Add(params Assembly3D[] es)
        {
            SubAssemblies.AddRange(es);
        }

        /// <summary>
        /// Create an assembly with no children yet
        /// </summary>
        public Assembly3D() 
        {

            _Invoker = (action, regen) => {
                action();
            };
            Transformation = new Identity();
            SubAssemblies = new ReactiveList<Assembly3D>();
            Labels = new ReactiveList<Label>();
            var block = new NamedBlock();
            BlockReference = block.BlockReference();
            Block = block;

            SubAssemblies
                .ItemsRemoved
                .Subscribe(e => SubAssemblyRemoved(e));

            SubAssemblies
                .ItemsAdded
                .StartWith(SubAssemblies)
                .Subscribe(SubAssemblyAdded);

            this
                .WhenAnyValue(p => p.Transformation)
                .Subscribe(v => BlockReference.Transformation = v);

            Labels
                .ItemsAdded
                .StartWith(Labels)
                .Subscribe(LabelAdded);

            Labels
                .ItemsRemoved
                .Subscribe(a => LabelRemoved(a));

            this.WhenAnyValue(p => p.Transformation)
                .Skip(1)
                .Select(t => this)
                .Merge(ChangedObservable)
                .Subscribe(e =>
                {
                    if (IsCompiled)
                        _ChangeObserver.OnNext(e);
                });


        }


        public Assembly3D(params Entity[] es) : this()
        {
            Add(es);
        }
        public Assembly3D(params Assembly3D[] es) : this()
        {
            Add(es);
        }
        public Assembly3D(IEnumerable<Entity> es) : this()
        {
            Add(es);
        }

        public BlockReference BlockReference { get; }

        public NamedBlock Block { get; }


        /// <summary>
        /// Compile the assembly and collect all change notification
        /// source and merge them into a single observable.
        /// </summary>
        /// <returns></returns>
        public void Compile
            ( Action<Action, bool> invoker
            , Action<Assembly3D> changeAction
            , IAssemblyViewportLayoutAdapter assemblyViewport
            , bool addToViewportLayout = false)
        {
            if (IsCompiled)
                return;

            _Subscriptions =
                new CompositeDisposable(CompileImpl(invoker, Observer.Create(changeAction), assemblyViewport, addToViewportLayout));

        }

        private IEnumerable<IDisposable> CompileImpl
            (Action<Action, bool> invoker, IObserver<Assembly3D> changeObserver, IAssemblyViewportLayoutAdapter assemblyViewport, bool addToViewport)
        {
            _AssemblyViewport = assemblyViewport;
            _Invoker = invoker;
            _ChangeObserver = changeObserver;

            AssertCorrectThread();

            yield return _AssemblyViewport.AddBlock(BlockReference.BlockName, Block);

            yield return Disposable.Create(DecompileAllSubAssemblies);
            CompileAllSubAssemblies();

            yield return Disposable.Create(DecompileAllLabels);
            CompileAllLabels();

            if (addToViewport)
            {
                yield return assemblyViewport.AddBlockReference(BlockReference);
            }

            IsCompiled = true;
        }

        private void CompileAllLabels()
        {
            foreach (var label in Labels)
            {
                LabelAdded(label);
            }
        }

        private void DecompileAllLabels()
        {
            foreach (var label in Labels)
            {
                LabelRemoved(label, false);
            }
        }

        private void CompileAllSubAssemblies()
        {
            foreach (var subAssembly in SubAssemblies)
            {
                CompileSubAssembly(subAssembly);
            }
        }

        private void DecompileAllSubAssemblies()
        {
            foreach (var subAssembly in SubAssemblies)
            {
                subAssembly.Decompile();
            }
        }

        public void Decompile()
        {
            if (IsCompiled)
            {
                _Subscriptions.Dispose();
            }
            IsCompiled = false;

            DecompileAllSubAssemblies();
        }

        public class LifecycleEvent
        {

            public enum LifecycleEventType
            {
                Added, Removed
            }

            public LifecycleEvent(Assembly3D parent, LifecycleEventType eventType)
            {
                Parent = parent;
                EventType = eventType;
            }

            public Assembly3D Parent { get; }

            public LifecycleEventType EventType { get; }
        }
        private readonly ISubject<LifecycleEvent> _LifecycleSubject = new Subject<LifecycleEvent>();
        private IDisposable _Subscriptions = Disposable.Empty;
        private IAssemblyViewportLayoutAdapter _AssemblyViewport;
        private IObserver<Assembly3D> _ChangeObserver;
        public IObservable<LifecycleEvent> LifecycleObservable => _LifecycleSubject;

        [Reactive] public bool IsCompiled { get; private set;}


        public void Remove(Assembly3D a)
        {
            AssertCorrectThread();
            SubAssemblies.Remove(a);
        }

        private void SubAssemblyRemoved(Assembly3D assembly3D, bool fire = true)
        {
            assembly3D.Parent = null;

            Block.Entities.Remove(assembly3D.BlockReference);

            if (IsCompiled)
                assembly3D.Decompile();

            assembly3D._LifecycleSubject.OnNext(new LifecycleEvent(this, LifecycleEvent.LifecycleEventType.Removed));
            if (fire)
                FireChange(this);
        }

        private void SubAssemblyAdded(Assembly3D assembly3D)
        {
            assembly3D.Parent?.Remove(assembly3D);
            assembly3D.Parent = this;

            if (Block.Entities.Contains(assembly3D.BlockReference))
                throw new Exception("Assembly already added");

            if (IsCompiled)
            {
                Debug.Assert(!assembly3D.IsCompiled);
                CompileSubAssembly(assembly3D);
            }
            Block.Entities.Add(assembly3D.BlockReference);

            assembly3D._LifecycleSubject.OnNext(new LifecycleEvent(this, LifecycleEvent.LifecycleEventType.Added));
            FireChange(this);
        }

        public Assembly3D Parent { get; private set; }

        private void CompileSubAssembly(Assembly3D assembly3D)
        {
            assembly3D.Compile
                (_Invoker
                , changeAction: a => _Changed.OnNext(a)
                , assemblyViewport: _AssemblyViewport
                );
        }

        private void LabelAdded(Label label)
        {
            if (!IsCompiled)
                return;
            _AssemblyViewport.AddLabel(label);
            FireChange(this);
        }

        private void LabelRemoved(Label label, bool fire = true)
        {
            if (!IsCompiled)
                return;

            _AssemblyViewport.RemoveLabel(label);
            if (fire)
                FireChange(this);
        }

    }
}