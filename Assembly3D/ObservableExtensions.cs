using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Weingartner.Eyeshot.Assembly3D
{
    public static class ObservableExtensions
    {
        public static IDisposable SubscribeDisposable<T>(this IObservable<T> observable, Func<T, Action> action) =>
            SubscribeDisposable(observable, v => Disposable.Create(action(v)));

        public static IDisposable SubscribeDisposable<T>(this IObservable<T> observable, Func<T, IDisposable> action)
        {
            var d = new SerialDisposable();
            return observable
                .Finally(() => d.Dispose())
                .Subscribe(e =>
                {
                    d.Disposable = Disposable.Empty;
                    d.Disposable = action(e);
                });
        }


        public static IDisposable SubscribeDisposable<T>(this IObservable<T> observable, Func<T, IEnumerable<IDisposable>> action)
        {
            return observable.SubscribeDisposable(t => (IDisposable) new CompositeDisposable(action(t)));
        }
    }
}