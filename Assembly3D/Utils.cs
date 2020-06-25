using System.IO;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive.Disposables;
using System.Windows.Media.Imaging;
using devDept.Eyeshot;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Weingartner.EyeShot.Assembly3D
{

    namespace Weingartner.Utils
    {
        public static class Utils
        {
            public static void Unlock( Model vpl )
            {
                var licenseFile = @".\eyeshotlicense.txt";
                if (!File.Exists( licenseFile ))
                {
                    var newkey = PromptDialog
                       .Prompt
                            ( "Please enter eyeshot key (first time only)"
                            , "Enter License"
                            , ""
                            , PromptDialog.InputType.Text );

                    File.WriteAllText( licenseFile, newkey );

                    MessageBox.Show( $@"Key '{newkey}' stored at '{Path.GetFullPath( licenseFile )}'" );
                }

                var key = File.ReadAllText( licenseFile );

                vpl.Unlock( key.Trim() );
            }

            public static BitmapImage ToBitmapImage( this Bitmap bitmap )
            {
                using (var memory = new MemoryStream())
                {
                    bitmap.Save( memory, ImageFormat.Png );
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption  = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }
        }
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


        public static class EnumerableExtensions
        {
            public static Option<T> FirstOrNone<T>( this IEnumerable<T> This )
            {
                foreach (var thi in This)
                {
                    return thi;
                }

                return None;
            }
        }

    }
}