namespace Weingartner.EyeShot.Assembly3D
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;

    namespace Weingartner.Utils
    {
        /// <summary>
        /// This is a disposable that stores child disposables
        /// in a dictionary. Attempting to register a new
        /// disposable with the same key will first dispose
        /// the original disposable before calling the
        /// factory function to create the new disposable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public sealed class SerialKeyedDisposable<T> : IDisposable
        {
            private Dictionary<T, IDisposable> _Dictionary = new Dictionary<T, IDisposable>();

            public void Dispose(T key)
            {
                lock (this)
                {
                    Register(key, () => Disposable.Empty);
                    _Dictionary.Remove(key);
                }
            }

            public void Register(T key, Func<IDisposable> fn)
            {

                lock (this)
                {
                    IDisposable previous;
                    if (_Dictionary.TryGetValue(key, out previous))
                    {
                        previous.Dispose();
                    }
                    _Dictionary[key] = fn();
                }

            }
            public void Register(T key, Func<IEnumerable<IDisposable>> keyedDisposable)
            {
#pragma warning disable CC0022 // Should dispose object
                Register(key, () => (IDisposable)new CompositeDisposable(keyedDisposable?.Invoke()));
#pragma warning restore CC0022 // Should dispose object
            }

            public void Dispose()
            {
                foreach (var disposable in _Dictionary)
                {
                    disposable.Value.Dispose();
                }
                _Dictionary = new Dictionary<T, IDisposable>();
            }

        }
    }
}