using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Graphics;
using ReactiveUI;
using Splat;
using Weingartner.EyeShot.Properties;
using Weingartner.ReactiveUI;
using Weingartner.Utils;
using Weingartner.Utils.Lens;
using Weingartner.Utils.Units;
using Brushes = System.Windows.Media.Brushes;
using Rotation = devDept.Geometry.Rotation;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace Weingartner.EyeShot
{
    public class WgViewportLayout : Model 
    {
        #region properties

        public Plane CurrentDefinitionPlane
        {
            get
            {
                Camera.GetFrame(out _, out var xAxis, out var yAxis, out _); 
                return new Plane(Point3D.Origin, xAxis, yAxis);
            }
        }

        private Point _MouseLocation;
        private Point _StartPointScreen;
        private bool _IsSnapEnabled;
        private ViewportMeasurement _CurrentMeasurement;
        private Point3D _Current;
        private ViewportMeasurement _MeasurementToMove;
        private readonly ICollection<ViewportMeasurement> _Measurements = new List<ViewportMeasurement>();
        public LengthUnits LengthUnit { get; set; }
        public AngleUnits AngleUnit { get; set; }
        public GlobalAppSettings AppSettings { get; set; }
        public double DisplayScalingFactor { get; set; } = 1.0;
        public bool IsOrtho { get; set; }
        public bool SnappingEnabled { get; set; }


        public static readonly DependencyProperty ShowEdgesObservableProperty = DependencyProperty.Register
            (
             "ShowEdgesObservable"
             , typeof(bool)
             , typeof(WgViewportLayout)
             , new PropertyMetadata( true ));

        public bool ShowEdgesObservable
        {
            get => (bool) GetValue( ShowEdgesObservableProperty );
            set => SetValue( ShowEdgesObservableProperty, value );
        }

        public static readonly DependencyProperty ResetZoomObservableProperty = DependencyProperty.Register
            ("ResetZoomObservable"
            , typeof(IObservable<Unit>)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(Observable.Never<Unit>())
            );

        public IObservable<Unit> ResetZoomObservable
        {
            get => (IObservable<Unit>)GetValue(ResetZoomObservableProperty);
            set => SetValue(ResetZoomObservableProperty, value ?? Observable.Never<Unit>());
        }

        public static readonly DependencyProperty Assembly3DProperty = DependencyProperty.Register
            ("Assembly3D"
             , typeof(Assembly3D.Assembly3D)
             , typeof(WgViewportLayout)
             , new PropertyMetadata(default(Assembly3D.Assembly3D))
             );

        public Assembly3D.Assembly3D Assembly3D
        {
            get => (Assembly3D.Assembly3D)GetValue(Assembly3DProperty);
            set => SetValue(Assembly3DProperty, value);
        }

        public static readonly DependencyProperty GridStepProperty = DependencyProperty.Register
            ("GridStep"
            , typeof(double)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(new Grid().Step)
            );

        public double GridStep
        {
            get => (double)GetValue(GridStepProperty);
            set => SetValue(GridStepProperty, value);
        }

        public static readonly DependencyProperty GridBoundaryProperty = DependencyProperty.Register
            ("GridBoundary"
            , typeof(double)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(0.1)
            );


        public double GridBoundary
        {
            get => (double)GetValue(GridBoundaryProperty);
            set => SetValue(GridBoundaryProperty, value);
        }

        public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register
            ("ViewType"
            , typeof(viewType)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(viewType.Top)
            );

        public viewType ViewType
        {
            get => (viewType)GetValue(ViewTypeProperty);
            set => SetValue(ViewTypeProperty, value);
        }

        public static readonly DependencyProperty GridMajorLineEveryProperty = DependencyProperty.Register
            ("GridMajorLineEvery"
            , typeof(int)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(10)
            );

        public int GridMajorLineEvery
        {
            get => (int)GetValue(GridMajorLineEveryProperty);
            set => SetValue(GridMajorLineEveryProperty, value);
        }

        public static readonly DependencyProperty ProjectionTypeProperty = DependencyProperty.Register
            (
                "ProjectionType",
                typeof(projectionType),
                typeof(WgViewportLayout),
                new PropertyMetadata(default(projectionType)));

        public projectionType ProjectionType
        {
            get => (projectionType)GetValue(ProjectionTypeProperty);
            set => SetValue(ProjectionTypeProperty, value);
        }


        #endregion

        #region legend

        public static readonly DependencyProperty LegenMinValueProperty = DependencyProperty.Register
            ( "LegenMinValue"
            , typeof(double)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(0.0));

        public double LegenMinValue
        {
            get => (double) GetValue(LegenMinValueProperty);
            set => SetValue(LegenMinValueProperty, value);
        }

        public static readonly DependencyProperty LegendMaxValueProperty = DependencyProperty.Register
            ("LegendMaxValue"
            , typeof(double)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(100.0));

        public double LegendMaxValue
        {
            get => (double) GetValue(LegendMaxValueProperty);
            set => SetValue(LegendMaxValueProperty, value);
        }

        public static readonly DependencyProperty LegendVisibleProperty = DependencyProperty.Register
            ("LegendVisible"
            , typeof(bool)
            , typeof(WgViewportLayout)
            , new PropertyMetadata(false));

        public bool LegendVisible
        {
            get => (bool) GetValue(LegendVisibleProperty);
            set => SetValue(LegendVisibleProperty, value);
        }

        internal IDisposable  SetupLegend(Viewport viewport)
        {
            return this.WhenAnyValue(p => p.LegenMinValue).CombineLatest(this.WhenAnyValue(p => p.LegendMaxValue),
                    this.WhenAnyValue(p => p.LegendVisible),
                    Utils.Units.Units.GetUnitsObservable(),
                    (min, max, vis, units) => new { min, max, vis, units})
                .WhenViewportLayoutReady(this)
                .Subscribe(lgnd =>
                {
                    viewport.Legends.Clear();

                    if (lgnd.vis)
                    {
                        var lengthScale = LengthUnits.Meters.ScaleTo(lgnd.units.LengthUnits);

                        var legend = new Legend
                                     {
                            Visibility = Visibility.Visible,
                            Title = "",
                            Subtitle = $"Legend in [{lgnd.units.LengthUnits.ToAbbreviation()}]",
                            TextColor = Brushes.White,
                            TitleColor = Brushes.White,
                            Min = lengthScale * lgnd.min,
                            Max = lengthScale * lgnd.max,
                            ItemSize = new Size(20, 20),
                            Height = 200
                        };
                        viewport.Legends.Add(legend);
                    }

                    viewport.Invalidate();
                });

            
        }


        #endregion

        public WgViewportLayout()
        {
            Unlock(License.Key);
            GridStep = 0.1;
            GridMajorLineEvery = 10;
            ProjectionType = projectionType.Orthographic;
            ViewType = viewType.Top;

            //###########LineTypes#########################
            LineTypes.Add("Dash", new[] { 0.001f, -0.0005f });
            LineTypes.Add("DashDot", new[] { 0.001f, -0.0005f, 0.0f, -0.0005f });
            //#############################################

            //############get app settings#################
            var service = Locator.Current.GetService<ILens<GlobalAppSettings>>();
            AppSettings = service != null ? service.Current : GlobalAppSettings.Default;
            //#############################################

            AutoRefresh = true;

            Observable
                .FromEventPattern<MouseEventHandler,MouseEventArgs>
                ( h=>MouseMove+=h
                , h=>MouseMove-=h
                )
                .Sample(TimeSpan.FromSeconds(1.0/30))
                .ObserveOn(Dispatcher)
                .Subscribe(MouseMoveHandler);

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.LoadUnloadHandler(Init);
            }

            // Use this to debug solids with bad normals
            //this.Backface.ColorMethod = backfaceColorMethodType.Cull;

            //this.StartAnimation();

            // Clear the measurements when the viewport is unloaded
            this.LoadUnloadHandler( () => Disposable.Create( ClearMeasurements ) );

        }

        private IEnumerable<IDisposable> Init()
        {
            yield return Utils.Units.Units
                .GetUnitsObservable()
                .ObserveOn(this)
                .Subscribe(settings =>
            {
                LengthUnit = settings.LengthUnits;
                AngleUnit = settings.AngleUnits;
                Invalidate();
            });

            // Bind the assembly property to the viewport
            yield return EyeShot.Assembly3D.Assembly3D.BindAssemblyToViewport
                ( this
                , p => p.Assembly3D
                , () =>
                     {
                         //if (!AutoZoomOff)
                             //ZoomFit();
                     });

            //#####This is necessary for getting the correct      ####
            //#####mouse position even if the Display is scaled!! ####
            DisplayScalingFactor = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            //#################################################################
        }

        //public override void DrawVertices(Viewport viewport)
        //{
        //    base.DrawVertices(viewport);

        //    gl.Enable(gl.COLOR_LOGIC_OP);
        //    gl.LogicOp(gl.XOR);

        //    gl.Color3ub(255, 255, 255);
        //    gl.Disable(gl.DEPTH_TEST);

        //    base.DrawVertices();

        //    gl.Enable(gl.DEPTH_TEST);
        //    gl.Disable(gl.COLOR_LOGIC_OP);
        //}

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                SetView(viewType.Top);
                ZoomFit();
                Invalidate();
            }

            if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ActionMode = actionType.Rotate;
                ZoomFit();
                RotateCamera(Vector3D.AxisX,90,true,true);
                ActionMode = actionType.None;
                Invalidate();
            }


            if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ClippingPlane1.Cancel();
                if (ClippingPlane1.Plane == Plane.XY)
                {
                    ClippingPlane1.Plane = Plane.YZ;
                }else if (ClippingPlane1.Plane == Plane.YZ)
                {
                    ClippingPlane1.Plane = Plane.ZX;
                }else if (ClippingPlane1.Plane == Plane.ZX)
                {
                    ClippingPlane1.Plane = Plane.XY;
                }
                else
                {
                    ClippingPlane1.Plane = Plane.YZ;
                }
                ClippingPlane1.Edit(Color.FromArgb(100, 0, 100, 100));
                Invalidate();
            }

            base.OnKeyDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released && _MeasurementToMove != null)
            {
                _Measurements.Remove(_MeasurementToMove);
                var tempMeas = _MeasurementToMove.MoveTextTo(_Current);

                _Measurements.Add(tempMeas);
                _MeasurementToMove = null;
                PaintBackBuffer();
                SwapBuffers();
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                if (_CurrentMeasurement == null)
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        IsOrtho = true;
                    }
                    StartDistanceMeasurement();
                }
                else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    StartAngleMeasurement();
                }
                else
                {
                    AddMeasurementPoint();
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed && e.ClickCount == 2 )
            {
                _CurrentMeasurement = null;
                _Measurements.Clear();
                Invalidate();
            }
            else if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2 )
            {
                ZoomFit();
            }
            else if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                _StartPointScreen = _MouseLocation;
            }




            base.OnMouseDown(e);
        }

        private void MouseMoveHandler(EventPattern<MouseEventArgs> eventPattern)
        {
            var e = eventPattern.EventArgs;

            var currentMousePos = Mouse.GetPosition(this);
            currentMousePos = new System.Windows.Point(currentMousePos.X*DisplayScalingFactor, currentMousePos.Y*DisplayScalingFactor);
            _MouseLocation = RenderContextUtility.ConvertPoint(currentMousePos);

            if (IsOrtho)
            {
                var xDiff = Math.Abs(_StartPointScreen.X - _MouseLocation.X);
                var yDiff = Math.Abs(_StartPointScreen.Y - _MouseLocation.Y);
                _MouseLocation = xDiff > yDiff 
                    ? new Point(_MouseLocation.X, _StartPointScreen.Y) 
                    : new Point(_StartPointScreen.X, _MouseLocation.Y);
            }

            _IsSnapEnabled = IsSnapEnabled();
            PickBoxSize = IsSnapEnabled() ? 50 : 6;

            if (ScreenToPlane(_MouseLocation, CurrentDefinitionPlane, out _Current))
            {
                SnapToEntity();
            }

            if (e.LeftButton == MouseButtonState.Pressed && _MeasurementToMove == null)
            {
                var measurementsToMove = _Measurements
                    .Where(m =>
                    {
                        var textScreenPoint = WorldToScreen(m.TextPosition);
                        var flippedYTextScreenPoint = new Point2D(textScreenPoint.X, Size.Height- textScreenPoint.Y);

                        return _StartPointScreen.X >= flippedYTextScreenPoint.X && _StartPointScreen.X < flippedYTextScreenPoint.X + 2*m.HalfHandleSize
                        && _StartPointScreen.Y >= flippedYTextScreenPoint.Y - m.HalfHandleSize && _StartPointScreen.Y < flippedYTextScreenPoint.Y + m.HalfHandleSize;
                    })
                    .ToList();

                if (measurementsToMove.Any())
                {
                    _MeasurementToMove = measurementsToMove.First();
                }
            }

            // This code is required to draw the measurements correctly
            // Notice: PaintBackBuffer / SwapBuffers() are optimized --> they don't redraw the 
            // scene but a texture of the scene (that is captured internally after some camera movement or mouse click).
            // Invalidate() causes a full redraw of the scene, instead.

            // paint the viewport surface
            PaintBackBuffer();
            // consolidates the drawing
            SwapBuffers();
        }


        private void StartDistanceMeasurement()
        {
            _StartPointScreen = _MouseLocation;
            _CurrentMeasurement = new DistanceMeasurement
                ( this
                , _Current
                , CurrentDefinitionPlane
                , DrawTextOnTransparentBackground
                );
        }

        private void StartAngleMeasurement()
        {
            _StartPointScreen = _MouseLocation;

            var isDistanceMeas = _CurrentMeasurement.GetType()==typeof(DistanceMeasurement);
            var centerPoint = isDistanceMeas 
                ? ((DistanceMeasurement)_CurrentMeasurement).StartPoint 
                : ((AngleMeasurement)_CurrentMeasurement).CenterPoint;

            if (!isDistanceMeas)
            {
                AddMeasurementPoint();
            }

            _CurrentMeasurement = new AngleMeasurement
                ( this
                , _Current
                , centerPoint 
                , CurrentDefinitionPlane
                , DrawTextOnTransparentBackground
                );
        }

        protected void DrawTextOnTransparentBackground(int i, int i1, string s)
        {
            DrawText
                (i
                , i1
                , s
                , new Font(System.Drawing.FontFamily.GenericSansSerif, AppSettings.MeasurementFontSize)
                , Color.Black
                , Color.FromArgb(200,Color.White)
                , ContentAlignment.MiddleLeft);
        }

        private void AddMeasurementPoint()
        {
            _CurrentMeasurement.AddPoint(_Current);
            if (_CurrentMeasurement.TryFinish())
            {
                if (_CurrentMeasurement.GetType() == typeof(DistanceMeasurement))
                {
                    _CurrentMeasurement.TextPosition = _Current;
                }
                else
                {
                    var meas = ((AngleMeasurement) _CurrentMeasurement);
                    _CurrentMeasurement.TextPosition = meas.GetArcCenterPoint();
                }
                _Measurements.Add(_CurrentMeasurement);
                IsOrtho = false;
            }
            _CurrentMeasurement = null;
        }

        private bool IsSnapEnabled()
        {
            var toolBars = GetToolBars();
            return !toolBars[0].Contains(_MouseLocation) && ! GetViewCubeIcon().Contains(_MouseLocation);
        }

        protected override void DrawOverlay(DrawSceneParams myParams)
        {
            if (_Current != null || ScreenToPlane(_MouseLocation, CurrentDefinitionPlane, out _Current))
            {
                // TODO GL
                /*
                gl.LineWidth(1.0f);

                gl.Enable(gl.COLOR_LOGIC_OP);
                gl.LogicOp(gl.XOR);

                gl.Color3ub(255, 255, 255);
                gl.Disable(gl.DEPTH_TEST);
                */
                //this.renderContext.EnableThickLines();
                renderContext.SetLineSize(1);
                renderContext.SetColorWireframe(Color.White);

                var measurementsToDraw = _Measurements.ToList();
                if (_CurrentMeasurement != null)
                {
                    _CurrentMeasurement.TempPoint = _Current;
                    measurementsToDraw.Add(_CurrentMeasurement);
                }
                else if (_IsSnapEnabled)
                {
                    measurementsToDraw.Add(new Marker(this, _Current, DrawTextOnTransparentBackground));
                }

                foreach (var measurement in measurementsToDraw)
                {
                    measurement.Draw(AngleUnit, LengthUnit, CurrentDefinitionPlane );
                }

            }

            base.DrawOverlay(myParams);
        }

        private void SnapToEntity()
        {
            if (!SnappingEnabled)
            {
                return;
            }

            Entities.SetCurrent((BlockReference)Entities.FirstOrDefault());
            while (Entities.CurrentBlockReference != null)
            {
                var entityIndex = GetEntityUnderMouseCursor(_MouseLocation);
                var entities = Blocks[Entities.CurrentBlockReference.BlockName].Entities;
                var activeEntity = entityIndex >= 0 ? entities[entityIndex] : null;

                Entities.SetCurrent(activeEntity as BlockReference);

                if (entityIndex != -1)
                {
                    var pts = SnappingHelpers.SnapPoints(activeEntity);
                    if (pts.Any())
                    {
                        var snap = FindClosestPoint(pts);
                        _Current = snap;
                    }
                }
            }
        }

        private Point3D FindClosestPoint(Point3D[] snapPoints)
        {

            var minDist = double.MaxValue;

            var i = 0;
            var index = 0;

            foreach (var vertex in snapPoints)
            {
                var vertexScreen = WorldToScreen(vertex);
                var currentScreen = new Point2D(_MouseLocation.X, Size.Height - _MouseLocation.Y);

                var dist = Point2D.Distance(vertexScreen, currentScreen);

                if (dist < minDist)
                {
                    index = i;
                    minDist = dist;
                }

                i++;
            }

            var snap = (Point3D)snapPoints.GetValue(index);
            
            return snap;
        }


        public void ClearMeasurements()
        {
            _Measurements.Clear();
        }


        public static WgViewportLayout CreateViewportLayout()
        {
            var vpl = new WgViewportLayout();

            vpl.Viewports.Add(CreateViewport(vpl));

            vpl.AskForAntiAliasing = false;
            vpl.AntiAliasing = false;
            vpl.Rendered.SilhouettesDrawingMode = silhouettesDrawingType.Never;
            vpl.DisplayMode = displayType.Rendered;
            vpl.Rendered.ShadowMode = shadowType.Realistic;
            // This causes a bug 
            // See https://devdept.zendesk.com/hc/en-us/requests/10356?page=1
            vpl.Rendered.RealisticShadowQuality = realisticShadowQualityType.High;
            vpl.AmbientLight = Color.White;
            vpl.ButtonStyle.HighlightColor = Brushes.OrangeRed;
            vpl.VertexSize = 3;

            var lightVector1 = Vector3D.AxisY;
            const double angleInRadians = Math.PI / 8;
            lightVector1.TransformBy(new Rotation(-angleInRadians, Vector3D.AxisX));
            lightVector1.TransformBy(new Rotation(angleInRadians / 10, Vector3D.AxisZ));

            vpl.Light1.YieldShadow = true;
            vpl.Light1.Direction = lightVector1;
            vpl.Light1.Active = true;
            vpl.Light1.Color = Color.White;

            vpl.DefaultColor = Color.Blue;

            return vpl;
        }

        private static Viewport CreateViewport(WgViewportLayout vpl)
        {
            var backGroundTopColor  = vpl.AppSettings.ColorTheme==ColorTheme.Light? Brushes.WhiteSmoke : Brushes.Black;
            var vp = new Viewport
                     {
                         Background =
                         {
                             StyleMode = backgroundStyleType.Solid,
                             ColorTheme = colorThemeType.Auto,
                             TopColor = backGroundTopColor,
                         },
                         CoordinateSystemIcon = new CoordinateSystemIcon { LabelColor = Brushes.White },
                         ToolBars = new ObservableCollection<ToolBar>
                                    {
                                        new ToolBar
                                        {
                                            Position = ToolBar.positionType.HorizontalTopCenter,
                                            Height = 10
                                        }
                                    },
                         OriginSymbol = new OriginSymbol(),
                         ViewCubeIcon = new ViewCubeIcon { HighlightColor = Brushes.OrangeRed }
                     };
            vp.Grids.Add(new Grid { Step = 10, AutoSize = true, AlwaysBehind = true, BorderColor = Brushes.Transparent});
            vp.ToolBars[0].Buttons.Add(new ZoomWindowToolBarButton());
            vp.ToolBars[0].Buttons.Add(new ZoomToolBarButton());
            vp.ToolBars[0].Buttons.Add(new PanToolBarButton());
            vp.ToolBars[0].Buttons.Add(new RotateToolBarButton());
            vp.ToolBars[0].Buttons.Add(new ZoomFitToolBarButton());
            vp.ToolBars[0].Buttons.Add(new MagnifyingGlassToolBarButton());

            //############## custom toolbar buttons #############################
            var assembly = Assembly.GetExecutingAssembly();
            var autoZoomButton = CreateAutoZoomOffButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(autoZoomButton);
            var clippingButton = CreateClippingButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(clippingButton);
            var shadingButton = CreateShadingButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(shadingButton);
            var wireFrameButton = CreateWireFrameButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(wireFrameButton);
            var snappingButton = CreateSnappingButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(snappingButton);

            var separator = CreateSeparator(assembly);
            vp.ToolBars[0].Buttons.Add(separator);

            var gridShowButton = CreateGridButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(gridShowButton);
            var markerButton = CreateMarkerButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(markerButton);
            var originSymbolButton = CreateOriginSymbolButton(vpl, assembly);
            vp.ToolBars[0].Buttons.Add(originSymbolButton);
            var helpButton = CreateHelpButton(vpl,  assembly);
            vp.ToolBars[0].Buttons.Add(helpButton);
            //###################################################################

            vpl.SetupLegend(vp);
            vpl.SetupPropertyHandlers(vp);

            return vp;
        }

        internal IDisposable SetupPropertyHandlers(Viewport viewport)
        {
            
            var c = new CompositeDisposable();

            this.WhenAnyValue(p => p.GridBoundary)
                .WhenViewportLayoutReady(this)
                .Subscribe(x =>
                {
                    viewport.Grid.Min = new Point2D(-x, -x);
                    viewport.Grid.Max = new Point2D(x, x);
                })
                .DisposeWith(c);

            this.WhenAnyValue(p => p.GridMajorLineEvery)
                .WhenViewportLayoutReady(this)
                .Subscribe(x => viewport.Grid.MajorLinesEvery = x)
                .DisposeWith(c);

            this.WhenAnyValue(p => p.ProjectionType)
                .WhenViewportLayoutReady(this)
                .Subscribe(x => Camera.ProjectionMode = x)
                .DisposeWith(c);

            this.WhenAnyValue(p => p.ViewType)
                .WhenViewportLayoutReady(this)
                .CombineLatest(this.Events().Loaded,(e,_)=>e) // Every time it is loaded reset the view.
                .Subscribe(SetView)
                .DisposeWith(c);

            this.WhenAnyValue(p => p.GridStep)
                .WhenViewportLayoutReady(this)
                .Subscribe(x => viewport.Grid.Step = x)
                .DisposeWith(c);

            this.WhenAnyObservable(p => p.ResetZoomObservable)
                .WhenViewportLayoutReady(this)
                .Delay(TimeSpan.FromMilliseconds(200),RxApp.MainThreadScheduler)
                .Subscribe(_ => ZoomFitAndInvalidate(!AutoZoomOff))
                .DisposeWith(c); 

            this.WhenAnyValue(p => p.ShowEdgesObservable)
                .WhenViewportLayoutReady(this)
                .Subscribe(b => Rendered.ShowEdges = b)
                .DisposeWith(c); 

            return c;
        }

        #region Factorites
        public static ToolBarButton CreateSeparator(Assembly assembly)
        {
            var separatorImage = Images.separatorButton.ToBitmapImage();

            var separator = new ToolBarButton
                ( separatorImage
                  , ""
                  , ""
                  , ToolBarButton.styleType.Separator
                  , true);

            return separator;
        }

        public static ToolBarButton CreateSnappingButton(WgViewportLayout vpl, Assembly assembly)
        {
            var wireFrameImage = Images.snappingButton.ToBitmapImage();

            var snappingButton = new ToolBarButton
                ( wireFrameImage
                  , "SnappingButton"
                  , "Enable/ Disable snapping"
                  , ToolBarButton.styleType.ToggleButton
                  , true);

            snappingButton.Click += (sender, e) =>
            {
                vpl.SnappingEnabled = !vpl.SnappingEnabled;
                vpl.Invalidate();
            };
            return snappingButton;
        }

        public static ToolBarButton CreateWireFrameButton(Model vpl, Assembly assembly)
        {
            var wireFrameImage = Images.wireFrameButton.ToBitmapImage();

            var wireFrameButton = new ToolBarButton
                ( wireFrameImage
                  , "WireFrameButton"
                  , "Switch wireFrame mode"
                  , ToolBarButton.styleType.PushButton
                  , true);

            wireFrameButton.Click += (sender, e) =>
            {
                vpl.DisplayMode = vpl.DisplayMode == displayType.Rendered? displayType.Wireframe : displayType.Rendered;
                vpl.Invalidate();
            };
            return wireFrameButton;
        }

        public static bool AutoZoomOff { get; set; }

        public static ToolBarButton CreateAutoZoomOffButton(Model vpl, Assembly assembly)
        {
            var autoZoomOffImage = Images.autoZoomButton.ToBitmapImage();

            var autoZoomOffButton = new ToolBarButton
                ( autoZoomOffImage
                  , "AutoZoomButton"
                  , "Turn off AutoZoom"
                  , ToolBarButton.styleType.ToggleButton
                  , true);
            autoZoomOffButton.Click += (sender, e) =>
            {
                AutoZoomOff = autoZoomOffButton.Pushed;
            };
            return autoZoomOffButton;
        }

        public static ToolBarButton CreateHelpButton(Model vpl, Assembly assembly)
        {
            var helpImage = Images.helpButton.ToBitmapImage();
             

            var helpButton = new ToolBarButton
                ( helpImage
                  , "HelpButton"
                  , "Viewport help"
                  , ToolBarButton.styleType.PushButton
                  , true);
            var helpLayout = new WgViewportLayoutHelpPanel(vpl);

            helpButton.Click += (sender, e) => helpLayout.Show();

            return helpButton;
        }

        public static ToolBarButton CreateMarkerButton(Model vpl, Assembly assembly)
        {
            var markerImage = Images.markerbutton.ToBitmapImage();

            var markerButton = new ToolBarButton
                ( markerImage
                  , "MarkerButton"
                  , "Show/hide markers"
                  , ToolBarButton.styleType.PushButton
                  , true);
            markerButton.Click += (sender, e) =>
            {
                vpl.ShowVertices = !vpl.ShowVertices;
                vpl.Invalidate();
            };
            return markerButton;
        }

        public static ToolBarButton CreateOriginSymbolButton(Model vpl, Assembly assembly)
        {
            var originImage = Images.originSymbolButton.ToBitmapImage();

            var originSymbolButton = new ToolBarButton
                ( originImage
                  , "OriginSymbolButton"
                  , "Show/hide origin symbol"
                  , ToolBarButton.styleType.PushButton
                  , true);
            originSymbolButton.Click += (sender, e) =>
            {
                vpl.GetOriginSymbol().Visible = !vpl.GetOriginSymbol().Visible;
                vpl.Invalidate();
            };
            return originSymbolButton;
        }

        public static ToolBarButton CreateShadingButton(Model vpl, Assembly assembly)
        {
            var shadingImage = Images.shadingButton.ToBitmapImage();

            var shadingButton = new ToolBarButton
                ( shadingImage
                  , "ShadingButton"
                  , "Shading"
                  , ToolBarButton.styleType.PushButton
                  , true);
            shadingButton.Click += (sender, e) =>
            {
                vpl.Rendered.PlanarReflections = !vpl.Rendered.PlanarReflections;
                vpl.Invalidate();
            };
            return shadingButton;
        }

        public static ToolBarButton CreateGridButton(Model vpl, Assembly assembly)
        {
            var gridShowImage = Images.gridButton.ToBitmapImage();

            var gridShowButton = new ToolBarButton
                ( gridShowImage
                  , "GridShowButton"
                  , "Show/hide grid"
                  , ToolBarButton.styleType.PushButton
                  , true);
            gridShowButton.Click += (sender, e) =>
            {
                vpl.GetGrid().Visible = !vpl.GetGrid().Visible;
            };
            return gridShowButton;
        }

        public static void UseClippingPlane(Model vpl, bool useClipping)
        {
            if (useClipping)
            {
                if (!vpl.ClippingPlane1.Active)
                {
                    vpl.ClippingPlane1.Plane = Plane.ZX;
                    vpl.ClippingPlane1.Edit( Color.FromArgb(100, 0, 100, 100));
                }
            }
            else
            {
                vpl.ClippingPlane1.Cancel();
            }
        }

        public static ToolBarButton CreateClippingButton(Model vpl, Assembly assembly)
        {
            var clippingImage = Images.clipButton.ToBitmapImage();

            var clippingButton = new ToolBarButton
                ( clippingImage
                  , "ClippingButton"
                  , "Clipping"
                  , ToolBarButton.styleType.PushButton
                  , true);
            clippingButton.Click += (sender, e) =>
            {
                var isCurrentlyClipping = vpl.ClippingPlane1.Active;
                UseClippingPlane(vpl, !isCurrentlyClipping);
                vpl.Invalidate();
            };
            return clippingButton;
        }
        #endregion

        public void ZoomFitAndInvalidate(bool zoomFit)
        {
            if (zoomFit && Viewports.Count > 0)
            {
                ZoomFit();
            }
            Invalidate();
        }
    }

    internal class Marker : ViewportMeasurement
    {
        public override bool IsFinished => true;

        public Point ScreenPoint { get; set; }

        public Marker(Model viewportLayout, Point3D point, Action<int, int, string> drawText)
            : base(viewportLayout, drawText)
        {
            var screenPt = viewportLayout.WorldToScreen( point );
            ScreenPoint = new Point((int)screenPt.X, (int)screenPt.Y); 
        }


        public override void Draw(AngleUnits angleUnit, LengthUnits lengthUnit, Plane currentPlane)
        {
            DrawMark(ScreenPoint);
        }

        public override void AddPoint(Point3D point)
        {
        }

        public override bool TryFinish()
        {
            return true;
        }

    }
}
