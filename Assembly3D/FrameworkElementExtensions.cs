using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Weingartner.EyeShot.Assembly3D
{
    public static class FrameworkElementExtensions
    {
        public static void LoadUnloadHandler(this FrameworkElement control, Func<IDisposable> action)
        {
            var state = false;
            var cleanup = new SerialDisposable();
            Observable.Merge
                (Observable.Return(control.IsLoaded)
                    , control.Events().Loaded.Select(x => true)
                    , control.Events().Unloaded.Select(x => false)
                )
                .Subscribe(isLoadEvent =>
                {
                    if (!state)
                    {
                        // unloaded state
                        if (isLoadEvent)
                        {
                            state = true;
                            cleanup.Disposable = new CompositeDisposable(action());
                        }
                    }
                    else
                    {
                        // loaded state
                        if (!isLoadEvent)
                        {
                            state = false;
                            cleanup.Disposable = Disposable.Empty;
                        }
                    }

                });
        }

        public static void LoadUnloadHandler(this FrameworkElement control, Func<IEnumerable<IDisposable>> action)
        {
            control.LoadUnloadHandler(() => (IDisposable)new CompositeDisposable(action()));
        }

        public static void LoadUnloadHandler(this FrameworkElement control, Action<Action<IDisposable>> action)
        {
            Func<IDisposable> fn = () =>
            {
                var d = new CompositeDisposable();
                action(d.Add);
                return d;
            };
            control.LoadUnloadHandler(fn);
        }

        public static void LoadUnloadHandler<T>(this IObservable<T> @this, FrameworkElement control, Action<T> action)
        {
            control.LoadUnloadHandler(() => @this.Subscribe(action));
        }
    }
}
