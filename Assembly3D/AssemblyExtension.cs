using System;
using System.Reactive.Linq;

namespace Weingartner.Eyeshot.Assembly3D
{
    public static class AssemblyExtension
    {
        /// <summary>
        /// Ensures that any subscribers are invoked on the
        /// schedular associated with assembly. It's realy
        /// just a friendly reactive extensions wrapper for
        /// the Assembly.Invoke method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <param name="assembly3D"></param>
        /// <param name="regen">Regenate the viewport on invocation. Is expensive.</param>
        /// <returns></returns>
        public static IObservable<T> ObserveOn<T>
            (this IObservable<T> @this
            , Assembly3D assembly3D
            , bool regen = true)
        {
            return Observable.Create<T>
                ( o => @this
                .Subscribe
                    ( v => assembly3D.Invoke(() => o.OnNext(v), regen)
                    , e => assembly3D.Invoke(() => o.OnError(e), regen)
                    , () => assembly3D.Invoke(o.OnCompleted, regen)
                    )
                );
        }

        public static IObservable<Assembly3D> ObserveOnViewport
            (this IObservable<Assembly3D> @this
            , bool regen = true)
        {
            return Observable.Create<Assembly3D>
                ( o => @this.Subscribe
                    ( v => v.Invoke(() => o.OnNext(v), regen)
                    , o.OnError
                    , o.OnCompleted
                    )
                );
        }
    }
}
