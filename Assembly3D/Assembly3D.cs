using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using DynamicData.Binding;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Weingartner.EyeShot.Assembly3D.Weingartner.Utils;
using Debugger = System.Diagnostics.Debugger;
using Label = devDept.Eyeshot.Labels.Label;
using Thread = System.Threading.Thread;
using Unit = System.Reactive.Unit;

namespace Weingartner.EyeShot.Assembly3D
{

    public interface IRequiresRedraw : IDisposable
    {
        IObservable<Unit> RedrawRequiredObservable { get; }
    }

    /// <summary>
    /// This class represents an hierarchical assembly of 3D objects in a cleaner way
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
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class Assembly3D : ReactiveObject
    {
        private ObservableCollectionExtended<Assembly3D> SubAssemblies { get; }
        private ObservableCollectionExtended<Label> Labels { get; }

        public bool HasChildAssemblies => SubAssemblies.Count > 0;

        private readonly Subject<Assembly3D> _Changed = new Subject<Assembly3D>();

        public IObservable<Assembly3D> ChangedObservable => _Changed;

        private int _ChangeDelayed;
        public IDisposable DelayChange()
        {
            _ChangeDelayed++;
            return Disposable.Create
                (() =>
                {
                    _ChangeDelayed--;
                    FireChange();
                });

        }

        //private bool IsChangeDelayed =>
        //    _ChangeDelayed > 0 || Parent != null && Parent != this && Parent.IsChangeDelayed;

        private bool IsChangeDelayed =>
            _ChangeDelayed > 0;

        public void FireChange()
        {
            if (!IsChangeDelayed && IsCompiled)
                _Changed.OnNext(this);
        }

        /// <summary>
        /// The transformation of this assembly relative to it's parent assembly or the
        /// viewport. Updating this property will automatically update the viewport. There
        /// is no need to manually invalidate the viewport.
        /// </summary>
        private Transformation _Transformation;

        private Action<Action, bool> _Invoker;
        private readonly ISubject<int> _MouseEnterObservable = new Subject<int>();

        /// <summary>
        /// Run the action on the scheduler / task associated with the assembly.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="regen"></param>
        /// <returns></returns>
        public void Invoke(Action a, bool regen = false)
        {
            Validate();
            _Invoker.Invoke(() =>
            {
                AssertCorrectThread();
                a();
            }, regen);
        }

        public void Validate()
        {
            if (AssemblyViewport != null && IsCompiled)
            {
                Debug.Assert(AssemblyViewport.Blocks.Contains(Block), "Viewport does not contain block reference");
                Debug.Assert(AssemblyViewport.Blocks.Contains(Block.Name), "Viewport does not contain block reference");
                foreach (var ent in Block.Entities.OfType<BlockReference>())
                {
                    Debug.Assert(AssemblyViewport.Blocks.Contains(ent.BlockName), "Viewport does not contain block reference");
                }
                foreach (var sub in SubAssemblies)
                {
                    sub.Validate();
                }
            }
        }

        public Transformation Transformation
        {
            get => _Transformation;
            set { AssertCorrectThread(); this.RaiseAndSetIfChanged(ref _Transformation, value); }
        }


        public void AssertCorrectThread()
        {
            if (AssemblyViewport == null)
                return;
            if (AssemblyViewport.Dispatcher.Thread == Thread.CurrentThread)
                return;
            if (Debugger.IsAttached)
                Debugger.Break();
            throw new Exception( "This code needs to be run on the control thread" );
        }
        public Transformation AccumulatedTransformation => BlockReferenceStack.Aggregate((Transformation)new Identity(), (t, block) => t * block.Transformation);

        /// <summary>
        /// Returns a stack of block references where the leaf
        /// items are at the top of the stack ( ie first to pop off )
        /// </summary>
        public Stack<BlockReference> BlockReferenceStack
        {
            get
            {
                Validate();
                var childToParent = new List<BlockReference>();
                var parent = this;
                while (parent != null)
                {
                    childToParent.Add(parent.BlockReference);
                    parent = parent.Parent;
                }

                return new Stack<BlockReference>(((IEnumerable<BlockReference>)childToParent).Reverse());

            }
        }


        readonly SerialKeyedDisposable<Entity> _RedrawSubscriptions = new SerialKeyedDisposable<Entity>();


        private readonly ISubject<int> _MouseExitObservable = new Subject<int>();

        public Assembly3D Clear()
        {
            AssertCorrectThread();
            using (DelayChange())
            {
                RemoveAllEntities();
                using (SubAssemblies.SuspendNotifications())
                {
                    foreach (var a in SubAssemblies.ToList())
                        Remove(a);
                }

                foreach (var label in Labels.ToList())
                    Remove(label);
            }
            return this;
        }


        #region Adders

        /// <summary>
        /// Add a child assembly to this assembly. Transformations
        /// on the child assembly will be relative to the reference
        /// coordinate system of this ( the parent ) assembly.
        /// </summary>
        /// <param name="a"></param>
        public Assembly3D Add(Assembly3D a)
        {
            AssertCorrectThread();
            using (DelayChangeNotifications())
            {
                SubAssemblies.Add(a);
                a.Parent?.Remove(a);
                a.Parent = this;

                if (Block.Entities.Contains(a.BlockReference))
                    throw new Exception("Assembly already added");

                if (IsCompiled)
                {
                    Debug.Assert(!a.IsCompiled);
                    CompileSubAssembly(a);
                }
                Add(a.BlockReference);
            }
            FireChange();
            return this;
        }

        /// <summary>
        /// Add a range of entities to the assembly
        /// </summary>
        /// <param name="es"></param>
        /// <param name="color"></param>
        public Assembly3D Add(IEnumerable<Entity> es, Color? color = null)
        {
            es = es ?? new Entity[] { };

            using (DelayChange())
            {
                var entities = es as IList<Entity> ?? es.ToList();

                if (color != null)
                    entities.SetColor(color.Value);

                if (entities.Any(e => Block.Entities.Contains(e)))
                    throw new Exception("Entity is already included");

                foreach (var entity in entities)
                    Add(entity);
            }
            return this;
        }
        public Assembly3D Remove(IEnumerable<Entity> es)
        {
            AssertCorrectThread();
            es = es ?? new Entity[] { };

            using (DelayChange())
            {
                var entities = es as IList<Entity> ?? es.ToList();

                foreach (var entity in entities)
                    Remove(entity);
            }
            return this;
        }

        public void Add(Entity e)
        {
            AssertCorrectThread();
            if (Block.Entities.Contains(e))
                throw new ArgumentException("Entity has already been added once");
            Block.Entities.Add(e);
            if (e is IRequiresRedraw irr)
            {
                _RedrawSubscriptions.Register(e, () => irr.RedrawRequiredObservable.Subscribe(_ => FireChange()));
            }
            FireChange();
        }        
        
        public void Add(Entity e, Color? color = null)
        {
            AssertCorrectThread();
            if (Block.Entities.Contains(e))
                throw new ArgumentException("Entity has already been added once");
            Block.Entities.Add(e);
            if (e is IRequiresRedraw irr)
            {
                _RedrawSubscriptions.Register(e, () => irr.RedrawRequiredObservable.Subscribe(_ => FireChange()));
            }
            FireChange();
        }


        public Assembly3D Replace(Assembly3D assembly3D)
        {
            using (DelayChange())
            {
                Clear();
                Add(assembly3D);
            }
            return this;
        }

        public Assembly3D Add(Label label)
        {
            Labels.Add(label);
            FireChange();
            return this;
        }

        public Assembly3D Add(IEnumerable<Label> labels)
        {
            using (DelayChange())
            {
                foreach (var label in labels)
                {
                    Add(label);
                }
            }
            return this;
        }

        #endregion


        #region removers

        public void Remove(Assembly3D a)
        {
            AssertCorrectThread();
            using (DelayChangeNotifications())
            {
                SubAssemblies.Remove(a);
                a.Parent = null;

                Remove(a.BlockReference);
            }
            if (IsCompiled)
                a.Decompile();

            if (true)
                FireChange();
        }

        public void Remove(Label a)
        {
            Labels.Remove(a);
        }

        public void Remove(Entity e)
        {
            AssertCorrectThread();
            Block.Entities.Remove(e);
            _RedrawSubscriptions.Dispose(e);
            FireChange();
        }

        private void RemoveAllEntities()
        {
            Block.Entities.Clear();
            _RedrawSubscriptions.Dispose();
        }
        #endregion



        #region Mouse Handling

        /// <summary>
        /// Triggered when a child entity is entered
        /// </summary>
        public IObservable<Entity> MouseEnterObservable
        {
            get
            {
                return _MouseEnterObservable
                    .Where(i => i != -1)
                    .Select(i => Block.Entities[i]);
            }
        }

        /// <summary>
        /// Triggered when a child entity is exited 
        /// </summary>
        public IObservable<Entity> MouseExitObservable
        {
            get
            {
                return _MouseExitObservable
                    .Where(i => i != -1)
                    .Select(i => Block.Entities[i]);
            }
        }

        #endregion

        #region constructors
        // ReSharper disable ExplicitCallerInfoArgument
        public Assembly3D(IEnumerable<Entity> es, [CallerFilePath] string path = "", [CallerLineNumber] int line = 0) : this(path, line)
        // ReSharper restore ExplicitCallerInfoArgument
        {
            Add(es);
        }

        public override string ToString()
        {
            return $"{_Name} : {Block.Name}";
        }

        //private static bool _InvalidateWaiting;
        private static readonly System.Collections.Generic.HashSet<Model> ViewportsToInvalidate = new System.Collections.Generic.HashSet<Model>();

        public static void Invalidate(Model viewport)
        {
            if (viewport == null)
                return;

            lock (ViewportsToInvalidate)
                ViewportsToInvalidate.Add(viewport);
            viewport.Invalidate();

            //if (_InvalidateWaiting)
            //    return;

            //_InvalidateWaiting = true;
            //await Task.Delay( TimeSpan.FromSeconds( 1 / 60.0 ) );

            //lock (ViewportsToInvalidate)
            //{
            //    foreach (var cpl in ViewportsToInvalidate)
            //    {
            //        cpl.Invalidate();
            //    }

            //}

            //_InvalidateWaiting = false;

        }

        public static BlockReference BlockReferenceEx(NamedBlock block)
        {
            return new BlockReference(new Identity(), block.Name);
        }

        /// <summary>
        /// Create an assembly with no children yet
        /// </summary>
        public Assembly3D([CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
        {
            _Name = $"{path}:{line}";

            _Invoker = (action, regen) => {
                action();
            };
            Transformation = new Identity();
            SubAssemblies = new ObservableCollectionExtended<Assembly3D>();
            Labels = new ObservableCollectionExtended<Label>();
            var block = new NamedBlock();
            BlockReference = BlockReferenceEx(block);
            Block = block;


            this
                .WhenAnyValue(p => p.Transformation)
                .Subscribe(v =>
                {
                    BlockReference.Transformation = v;
                    BlockReference.RegenMode = regenType.NotNeeded;
                    Invalidate(AssemblyViewport);
                });

            ChangedObservable
               .ObserveOn(this)
               .Subscribe(e =>
               {
                   if (IsCompiled)
                   {
                       Regen();
                       Invalidate(AssemblyViewport);
                   }
               });
        }


        #endregion

        public BlockReference BlockReference { get; }


        #region compilers
        /// <summary>
        /// Compile the assembly and collect all change notification
        /// source and merge them into a single observable.
        /// </summary>
        /// <returns></returns>
        public void Compile
            (Action<Action, bool> invoker
            , Model assemblyViewport
            , bool addToViewportLayout = false)
        {
            if (IsCompiled)
            {
                if (assemblyViewport != null && !Equals(assemblyViewport, AssemblyViewport))
                    throw new Exception("Trying to assign assembly to multiple viewports is not allowed");
                Validate();
                return;
            }

            IEnumerable<IDisposable> Compile()
            {
                yield return AssemblyViewport.AddBlock(BlockReference.BlockName,Block);

                yield return Disposable.Create(DecompileAllSubAssemblies);
                CompileAllSubAssemblies();

                yield return Disposable.Create(DecompileAllLabels);
                CompileAllLabels();

                if (addToViewportLayout)
                {
                    AssemblyViewport.Entities.Add(BlockReference);
                    yield return Disposable.Create(() => AssemblyViewport.Entities.Remove(BlockReference));
                }

                yield return OnCompiled(AssemblyViewport);
            }

            using (DelayChangeNotifications())
            {
                AssemblyViewport = assemblyViewport;
                _Invoker = invoker;
                AssertCorrectThread();
                _Subscriptions = new CompositeDisposable(Compile());

                IsCompiled = true;
            }

            Validate();
            FireChange();
        }

        protected virtual IDisposable OnCompiled(Model assemblyViewport)
        {
            return Disposable.Empty;
        }

        private void CompileAllLabels()
        {
            foreach (var label in Labels)
            {
                LabelAdded(label);
            }
        }

        private void CompileSubAssembly(Assembly3D assembly3D)
        {
            assembly3D.Compile
                (_Invoker
               , assemblyViewport: AssemblyViewport
                );
        }

        #endregion
        #region decompilers

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
            AssertCorrectThread();
            if (IsCompiled)
            {
                _Subscriptions.Dispose();
            }
            IsCompiled = false;

            DecompileAllSubAssemblies();
        }
        #endregion

        private IDisposable _Subscriptions = Disposable.Empty;
        public Model AssemblyViewport { get; private set; }
        private readonly string _Name;

        [Reactive] public bool IsCompiled { get; private set; }


        #region handlers

        private void LabelAdded(Label label)
        {
            if (AssemblyViewport == null)
                return;
            AssemblyViewport.Labels.Add(label);
            FireChange();
        }

        private void LabelRemoved(Label label, bool fire = true)
        {
            if (!IsCompiled)
                return;

            AssemblyViewport.Labels.Remove(label);
            if (fire)
                FireChange();
        }

        #endregion

        public Assembly3D Parent { get; private set; }


        #region selection

        public NamedBlock Block { get; }

        public const double DefaultRegenTolerance = 1e-5;

        /// <summary>
        /// The tolerance with which Regen will get called.
        /// </summary>
        public Option<double> RegenTolerance { get; set; } = Option<double>.None;

        public double CurrentOrParentRegenTolerance =>
            RegenTolerance.IfNone(() => Parent?.CurrentOrParentRegenTolerance ?? DefaultRegenTolerance);


        /// <summary>
        /// Set this assembly as the current selection scope. The
        /// assembly must have been compiled and attached to the view
        /// port before calling. 
        /// </summary>
        public void SetAsSelectionScope()
        {
            if (!IsCompiled)
                throw new Exception("Assembly must be compiled and attached to viewport to call this");
            AssemblyViewport.SetSelectionScope(BlockReferenceStack);
        }


        #endregion
        public void Regen()
        {
            // This check is here because sometimes deep in eyeshot
            // there is a null reference exception and it occurs 
            // when the renderContext is null
            if (AssemblyViewport.renderContext != null)
            {
                var p = new RegenParams(CurrentOrParentRegenTolerance, AssemblyViewport);
                BlockReference.Regen(p);
                Block.Entities.RegenAllCurved();
                AssemblyViewport.Entities.Regen();
                AssemblyViewport.Invalidate();
                AssemblyViewport.AdjustNearAndFarPlanes();
            }
        }


        /// <summary>
        /// Provides the viewport with change handling on a property that contains and Assembly3D.
        /// When the property changes it is compiled and it's components are added correctly to the
        /// 3D viewport.
        /// </summary>
        /// <param name="assemblyViewportLayoutAdapter"></param>
        /// <param name="selector">for example <![CDATA[p=>p.Assembly]]> where Assembly is of type Assembly3D</param>
        /// <param name="postAddAction"></param>
        /// <returns></returns>
        public static IDisposable BindAssemblyToViewport
            (WgViewportLayout assemblyViewportLayoutAdapter
            , Expression<Func<WgViewportLayout, Assembly3D>> selector
            , Action postAddAction)
        {
            return assemblyViewportLayoutAdapter
                  .Ready()
                  .Select(_ => assemblyViewportLayoutAdapter
                              .WhenAnyValue(selector)
                              .Where(v => v!=null)
                              .DistinctUntilChanged(p => p?.Block.Name))
                  .Switch()
                  .SubscribeDisposable
                       (asm => asm.BindToViewportLayout
                                (assemblyViewportLayoutAdapter
                                , postAddAction));
        }

        /// <summary>
        /// Add the assembly to the viewport. This also hooks up listeners to
        /// any transformations on the assembly and invalidates the viewport
        /// to ensure a redraw.
        /// </summary>
        /// <param name="viewportLayout"></param>
        /// <param name="postAddAction"></param>
        public IDisposable BindToViewportLayout(WgViewportLayout viewportLayout, Action postAddAction = null)
        {
            return viewportLayout.Dispatcher.Invoke
                (() =>
                {
                    Decompile();
                    Compile
                        (invoker: (action, regen) =>
                             viewportLayout.Dispatcher.InvokeAsync
                                 (() =>
                                 {
                                     action();
                                     if (IsCompiled && regen)
                                         Regen();
                                 })
                        , assemblyViewport: viewportLayout
                        , addToViewportLayout: true
                        );

                    postAddAction?.Invoke();
                    viewportLayout.Invalidate();

                    return Disposable.Create(Decompile);
                });

        }        
        
        /// <summary>
        /// Provides the viewport with change handling on a property that contains and Assembly3D.
        /// When the property changes it is compiled and it's components are added correctly to the
        /// 3D viewport.
        /// </summary>
        /// <param name="assemblyViewportLayoutAdapter"></param>
        /// <param name="selector">for example <![CDATA[p=>p.Assembly]]> where Assembly is of type Assembly3D</param>
        /// <param name="postAddAction"></param>
        /// <returns></returns>
        public static IDisposable BindAssemblyToViewport
            (Model assemblyViewportLayoutAdapter
            , Expression<Func<Model, Assembly3D>> selector
            , Action postAddAction)
        {
            return assemblyViewportLayoutAdapter
                  .Ready()
                  .Select(_ => assemblyViewportLayoutAdapter
                              .WhenAnyValue(selector)
                              .Where(v => v!=null)
                              .DistinctUntilChanged(p => p?.Block.Name))
                  .Switch()
                  .SubscribeDisposable
                       (asm => asm.BindToViewportLayout
                                (assemblyViewportLayoutAdapter
                                , postAddAction));
        }

        /// <summary>
        /// Add the assembly to the viewport. This also hooks up listeners to
        /// any transformations on the assembly and invalidates the viewport
        /// to ensure a redraw.
        /// </summary>
        /// <param name="viewportLayout"></param>
        /// <param name="postAddAction"></param>
        public IDisposable BindToViewportLayout(Model viewportLayout, Action postAddAction = null)
        {
            return viewportLayout.Dispatcher.Invoke
                (() =>
                {
                    Decompile();
                    Compile
                        (invoker: (action, regen) =>
                             viewportLayout.Dispatcher.InvokeAsync
                                 (() =>
                                 {
                                     action();
                                     if (IsCompiled && regen)
                                         Regen();
                                 })
                        , assemblyViewport: viewportLayout
                        , addToViewportLayout: true
                        );

                    postAddAction?.Invoke();
                    viewportLayout.Invalidate();

                    return Disposable.Create(Decompile);
                });

        }
    }
}