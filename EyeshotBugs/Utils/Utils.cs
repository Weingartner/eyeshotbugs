using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Graphics;

namespace EyeshotBugs.Utils
{
    public static class Eyeshot
    {
        private static Lazy<EyeshotWindow> _ViewLazy = new Lazy<EyeshotWindow>(CreateEyeshotWindow);
        public static EyeshotWindow ViewportLayout => _ViewLazy.Value;
        
        public static ViewportLayout CreateViewportLayout()
        {
            var vpl = new ViewportLayout();
            Unlock(vpl);

            vpl.Viewports.Add(CreateViewport());

            vpl.AskForAntiAliasing = false;
            vpl.AntiAliasing = false;
            vpl.Rendered.SilhouettesDrawingMode = silhouettesDrawingType.Never;
            vpl.DisplayMode = displayType.Rendered;
            vpl.Rendered.ShadowMode = shadowType.Realistic;
            vpl.Rendered.PlanarReflections = false;
            vpl.Rendered.ShowEdges = true;
            vpl.Rendered.RealisticShadowQuality = realisticShadowQualityType.High;
            vpl.AmbientLight = Color.White;
            vpl.GetGrid().Step = 1;
            vpl.GetGrid().MajorLinesEvery = 10;
            vpl.GetGrid().AutoStep = false;
            vpl.Backface.ColorMethod = backfaceColorMethodType.EntityColor;

            var lightVector1 = Vector3D.AxisY;
            const double angleInRadians = Math.PI / 8;
            lightVector1.TransformBy(new Rotation(-angleInRadians, Vector3D.AxisX));
            lightVector1.TransformBy(new Rotation(angleInRadians / 10, Vector3D.AxisZ));

            vpl.Light1.YieldShadow = true;
            vpl.Light1.Direction = lightVector1;
            vpl.Light1.Active = true;
            vpl.Light1.Color = Color.White;
            return vpl;
        }

        private static void Unlock(ViewportLayout vpl)
        {
            var licenseFile = @".\eyeshotlicense.txt";
            if (!File.Exists(licenseFile))
            {
                var newkey = PromptDialog
                    .Prompt
                    ("Please enter eyeshot key (first time only)", "Enter License", "", PromptDialog.InputType.Text);

                File.WriteAllText(licenseFile,newkey);

                MessageBox.Show($@"Key '{newkey}' stored at '{Path.GetFullPath(licenseFile)}'");
            }

            var key = File.ReadAllText(licenseFile);
            
            vpl.Unlock(key.Trim());
        }


        public static Point3D ToEyeshot(this Vector3D v)=>new Point3D(v.X, v.Y, v.Z);

        private static Viewport CreateViewport()
        {
            var vp = new Viewport
            {
                CoordinateSystemIcon = new CoordinateSystemIcon (),
                OriginSymbol = new OriginSymbol(),
                ViewCubeIcon = new ViewCubeIcon ()
            };
            vp.Grids.Add(new Grid { Step = 10, AutoSize = true });
            var toolbar = new ToolBar {Position = ToolBar.positionType.HorizontalTopCenter};
            toolbar.Buttons.Add(new ZoomWindowToolBarButton());
            toolbar.Buttons.Add(new ZoomToolBarButton());
            toolbar.Buttons.Add(new PanToolBarButton());
            toolbar.Buttons.Add(new RotateToolBarButton());
            toolbar.Buttons.Add(new ZoomFitToolBarButton());
            vp.ToolBars.Add(toolbar);
            return vp;
        }

        public class EyeshotWindow : Window
        {
            private readonly TaskCompletionSource<ViewportLayout> _Tcs;
            public ViewportLayout ViewportLayout { get; }

            public EyeshotWindow(ViewportLayout viewportLayout)
            {
                ViewportLayout = viewportLayout;
                _Tcs = new TaskCompletionSource<ViewportLayout>();
                Closing += (sender, args) =>
                {
                    _Tcs.SetResult(viewportLayout);
                    Hide();
                    ViewportLayout.Entities.Clear();
                    args.Cancel = true;
                };
            }

            public Task ClosedTask => _Tcs.Task;
        }

        public static EyeshotWindow CreateEyeshotWindow()
        {
            var viewportLayout = CreateViewportLayout();
            var window = new EyeshotWindow(viewportLayout);
            window.Content = viewportLayout;
            window.Show();
            return window;
        }

        public static Task RunSTADesktop(Action action)
        {
            return RunSTADesktop
                (() =>
                {
                    action();
                    return Task.FromResult(Unit.Default);
                });
        }

        public static async Task RunSTADesktop(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<Unit>();
            var thread = new Thread(() =>
            {
                // Set up the SynchronizationContext so that any awaits
                // resume on the STA thread as they would in a GUI app.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                tcs.SetResult(Unit.Default);
                Dispatcher.Run();
            });
            thread.Name = "Eyeshot.STA";

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            await tcs.Task;
            var invoke = Dispatcher.FromThread(thread)?.Invoke(async () =>
            {
                await action();
            });
            if (invoke != null)
                await invoke;
        }
    }
    public static class EyeshotExtensions
    {
        public static void AddTo(this IEnumerable<Entity> e, Eyeshot.EyeshotWindow vl)
        {
            vl.Dispatcher.Invoke (() => { if (!vl.IsVisible) vl.Show(); });
            e.ForEach(k => k.AddTo(vl));
        }

        public static void AddTo(this Entity e, Eyeshot.EyeshotWindow vl)
        {
            vl.Dispatcher.Invoke
                (() =>
                {
                    if (!vl.IsVisible)
                        vl.Show();
                    vl.ViewportLayout.Entities.Add(e);
                });
        }


    }
}
