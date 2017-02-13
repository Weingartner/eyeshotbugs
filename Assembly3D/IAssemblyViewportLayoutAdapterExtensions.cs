using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace Weingartner.Eyeshot.Assembly3D
{
    // ReSharper disable once InconsistentNaming
    public static class IAssemblyViewportLayoutAdapterExtensions
    {
        /// <summary>
        /// Provides the viewport with change handling on a property that contains and Assembly3D.
        /// When the property changes it is compiled and it's components are added correctly to the
        /// 3D viewport.
        /// </summary>
        /// <param name="assemblyViewportLayoutAdapter"></param>
        /// <param name="assemblyObserver">for example <![CDATA[p=>p.Assembly]]> where Assembly is of type Assembly3D</param>
        /// <param name="postAddAction"></param>
        /// <returns></returns>
        public static IDisposable BindAssemblyToViewport
            ( this IAssemblyViewportLayoutAdapter assemblyViewportLayoutAdapter
            , IObservable<Assembly3D> assemblyObserver
            , Action postAddAction
            )
        {
            return assemblyViewportLayoutAdapter
                .Ready()
                .Select(_ => assemblyObserver
                    .Where(v=>v!=null)
                    .DistinctUntilChanged(p => p.Block.Name))
                .Switch()
                .SubscribeDisposable(asm => SetRootAssembly(assemblyViewportLayoutAdapter, asm, postAddAction));
        }

        /// <summary>
        /// Add the assembly to the viewport. This also hooks up listeners to
        /// any transformations on the assembly and invalidates the viewport
        /// to ensure a redraw.
        /// </summary>
        /// <param name="assemblyViewportLayoutAdapter"></param>
        /// <param name="assembly3D"></param>
        /// <param name="postAddAction"></param>
        public static IDisposable SetRootAssembly(this IAssemblyViewportLayoutAdapter assemblyViewportLayoutAdapter, Assembly3D assembly3D, Action postAddAction=null)
        {
            return assemblyViewportLayoutAdapter.Invoke
                (() =>
                {
                    var handler = new Subject<Assembly3D>();

                    var invalidateRegistration = handler
                        .Synchronize()
                        .Throttle(TimeSpan.FromSeconds(1.0 / 120))
                        .Sample(TimeSpan.FromSeconds(1.0 / 60))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(o => assemblyViewportLayoutAdapter.Invalidate(false));

                    assembly3D.Decompile();
                    assembly3D.Compile
                        ( invoker: (action, regen) =>
                              assemblyViewportLayoutAdapter.Invoke
                                  (() =>
                                  {
                                      action();
                                      assembly3D.FireChange(assembly3D);
                                      if(regen)
                                          assemblyViewportLayoutAdapter.Invalidate(true);
                                  })
                        , changeAction: handler.OnNext
                        , assemblyViewport: assemblyViewportLayoutAdapter
                        , addToViewportLayout: true
                        );


                    assemblyViewportLayoutAdapter.Invalidate(true);
                    postAddAction?.Invoke();

                    return Disposable.Create
                        (() =>
                        {
                            assembly3D.Decompile();
                            invalidateRegistration.Dispose();
                            handler.Dispose();
                        });
                });

        }
    }
}