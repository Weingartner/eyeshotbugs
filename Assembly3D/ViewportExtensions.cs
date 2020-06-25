using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using Weingartner.EyeShot.Assembly3D.Weingartner.Utils;

namespace Weingartner.EyeShot.Assembly3D
{
    public static class ViewportExtensions
    {
        /// <summary>
        /// Observe `ViewportLayout.Loaded` and trigger immediately when the viewport is already loaded.
        /// </summary>
        /// <param name="viewport"></param>
        /// <returns></returns>
        public static IObservable<Unit> Ready(this Model viewport)
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


        public static IObservable<T> WhenViewportLayoutReady<T>(this IObservable<T> obs, Model viewportLayout)
        {
            return viewportLayout.Ready()
                                 .Select(_ => obs)
                                 .Switch()
                                 .ObserveOn(viewportLayout.Dispatcher);
        }

        public static Entity[] Deserialize(string txt)
        {
            txt = GeomTest.Unzip(Convert.FromBase64String(txt));
            using (var stream = new MemoryStream(Convert.FromBase64String(txt)))
            {
                var binaryFmt = new BinaryFormatter();
                return (Entity[])binaryFmt.Deserialize(stream);
            }
        }

        public static string Serialize(params Entity[] entities)
        {
            var binaryFmt    = new BinaryFormatter();
            var memoryStream = new MemoryStream();
            using (memoryStream)
            {
                binaryFmt.Serialize(memoryStream, entities);
            }
            return Convert.ToBase64String(GeomTest.Zip(Convert.ToBase64String(memoryStream.ToArray())));
        }

        /// <summary>
        /// Extract the ViewportLayout from the Viewport via reflection.
        /// </summary>
        /// <param name="vp"></param>
        /// <returns></returns>
        public static Model GetViewportLayout(this Viewport vp)
        {
            FieldInfo FieldInfo()
            {
                return _VplFieldInfo ?? (_VplFieldInfo = vp
                                                        .GetType()
                                                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                                        .Where(p => p.FieldType.Name.Contains("ViewportLayout"))
                                                        .FirstOrNone()
                                                        .IfNone(() => throw new KeyNotFoundException("ViewportLayout")));
            }
            return (Model)FieldInfo().GetValue(vp);
        }

        private static FieldInfo _VplFieldInfo;
    }
}
