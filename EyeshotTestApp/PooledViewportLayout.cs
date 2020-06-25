using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using Weingartner.EyeShot.Assembly3D;

namespace EyeshotTestApp
{
    public class PooledViewportLayout : ContentControl
    {
        public static readonly DependencyProperty ViewportLayoutProperty = DependencyProperty.Register
            (
             "ViewportLayout",
             typeof(WgViewportLayout),
             typeof(PooledViewportLayout),
             new PropertyMetadata(default(WgViewportLayout)));

        public WgViewportLayout ViewportLayout
        {
            get => (WgViewportLayout)GetValue(ViewportLayoutProperty);
            set => SetValue(ViewportLayoutProperty, value);
        }

        private readonly ISet<Func<WgViewportLayout, IDisposable>> _Handlers = new HashSet<Func<WgViewportLayout, IDisposable>>();

        public IDisposable LoadUnloadHandler(Func<WgViewportLayout, IEnumerable<IDisposable>> fn)
            => LoadUnloadHandler(v => (IDisposable)new CompositeDisposable(fn(v)));

        public IDisposable LoadUnloadHandler(Func<WgViewportLayout, IDisposable> fn)
        {
            _Handlers.Add(fn);
            return Disposable.Create(() => _Handlers.Remove(fn));
        }

        public PooledViewportLayout()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            this.LoadUnloadHandler(() =>
            {
                var mvl = Pool.Acquire();
                Content = mvl;
                var d = new CompositeDisposable(_Handlers.Select(h => h(mvl)));

                Disposable.Create
                           (() =>
                           {
                               Content = null;
                               Pool.Release(mvl);
                           })
                          .DisposeWith(d);

                return (IDisposable)d;
            });

            Content = new Grid();

        }

        public static Pool<WgViewportLayout> Pool = new Pool<WgViewportLayout>(WgViewportLayout.CreateViewportLayout);


    }

    public class Pool<T>
    {
        private readonly Func<T>          _Factory;
        private readonly ConcurrentBag<T> _Objects = new ConcurrentBag<T>();

        public Pool(Func<T> factory) => _Factory = factory;

        public T Acquire()
        {
            T obj;
            return _Objects.TryTake(out obj) ? obj : _Factory();
        }

        public void Release(T obj)
        {
            _Objects.Add(obj);
        }
    }
}