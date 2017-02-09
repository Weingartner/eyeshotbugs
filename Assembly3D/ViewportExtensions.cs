using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using devDept.Eyeshot;

namespace Weingartner.Eyeshot.Assembly3D
{
    public static class ViewportExtensions
    {
        /// <summary>
        /// Observe `ViewportLayout.Loaded` and trigger immediately when the viewport is already loaded.
        /// </summary>
        /// <param name="viewport"></param>
        /// <returns></returns>
        public static IObservable<Unit> Ready(this ViewportLayout viewport)
        {
            return Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>
                (h => viewport.Loaded += h
                , h => viewport.Loaded -= h
                )
                .Select(_ => true)
                .StartWith(viewport.IsLoaded)
                .Where(v=>v)
                .Select(x => Unit.Default)
                .Take(1)
                .ObserveOn(viewport);
        }
    }
}
