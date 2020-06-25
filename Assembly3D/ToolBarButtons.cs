using System.Drawing;
using System.Reflection;
using devDept.Eyeshot;
using devDept.Geometry;
using Weingartner.EyeShot.Assembly3D;
using Weingartner.Eyeshot.Assembly3D.Properties;
using Weingartner.EyeShot.Assembly3D.Weingartner.Utils;

namespace Weingartner.EyeShot
{
    public static class ToolBarButtons
    {
        public static ToolBarButton CreateSeparator(Assembly assembly)
        {
            var foo = new System.Windows.Media.Imaging.BitmapImage();
            var separatorImage = Resources.separatorButton.ToBitmapImage();

            var separator = new ToolBarButton
                (separatorImage
                  , ""
                  , ""
                  , ToolBarButton.styleType.Separator
                  , true);

            return separator;
        }

        public static ToolBarButton CreateWireFrameButton(Model vpl, Assembly assembly)
        {
            var wireFrameImage = Resources.wireFrameButton.ToBitmapImage();

            var wireFrameButton = new ToolBarButton
                (wireFrameImage
                  , "WireFrameButton"
                  , "Switch wireFrame mode"
                  , ToolBarButton.styleType.PushButton
                  , true);

            wireFrameButton.Click += (sender, e) =>
            {
                vpl.DisplayMode = vpl.DisplayMode == displayType.Rendered ? displayType.Wireframe : displayType.Rendered;
                vpl.Invalidate();
            };
            return wireFrameButton;
        }

        public static ToolBarButton CreateAutoZoomOffButton(Model vpl, Assembly assembly)
        {
            var autoZoomOffImage = Resources.autoZoomButton.ToBitmapImage();

            var autoZoomOffButton = new ToolBarButton
                (autoZoomOffImage
                  , "AutoZoomButton"
                  , "Turn off AutoZoom"
                  , ToolBarButton.styleType.ToggleButton
                  , true);
            autoZoomOffButton.Click += (sender, e) =>
            {
                WgViewportLayout.AutoZoomOff = autoZoomOffButton.Pushed;
            };
            return autoZoomOffButton;
        }

        public static ToolBarButton CreateMarkerButton(Model vpl, Assembly assembly)
        {
            var markerImage = Resources.markerbutton.ToBitmapImage();

            var markerButton = new ToolBarButton
                (markerImage
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
            var originImage = Resources.originSymbolButton.ToBitmapImage();

            var originSymbolButton = new ToolBarButton
                (originImage
                  , "OriginSymbolButton"
                  , "Show/hide origin symbol"
                  , ToolBarButton.styleType.PushButton
                  , true);
            originSymbolButton.Click += (sender, e) =>
            {
                vpl.ActiveViewport.OriginSymbol.Visible = !vpl.ActiveViewport.OriginSymbol.Visible;
                vpl.Invalidate();
            };
            return originSymbolButton;
        }

        public static ToolBarButton CreateShadingButton(Model vpl, Assembly assembly)
        {
            var shadingImage = Resources.shadingButton.ToBitmapImage();

            var shadingButton = new ToolBarButton
                (shadingImage
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
            var gridShowImage = Resources.gridButton.ToBitmapImage();

            var gridShowButton = new ToolBarButton
                (gridShowImage
                  , "GridShowButton"
                  , "Show/hide grid"
                  , ToolBarButton.styleType.PushButton
                  , true);
            gridShowButton.Click += (sender, e) =>
            {
                vpl.ActiveViewport.Grid.Visible = !vpl.ActiveViewport.Grid.Visible;
            };
            return gridShowButton;
        }

        public static ToolBarButton CreateSaveButton(Model vpl, Assembly assembly)
        {
            var saveImage = Resources.saveButton.ToBitmapImage();
            var saveButton = new ToolBarButton
                (saveImage
                , "SaveButton"
                , "Save Assembly"
                , ToolBarButton.styleType.PushButton
                , true);
            saveButton.Click += (sender, args) =>
            {
                vpl.Save();
            };
            return saveButton;
        }

        public static ToolBarButton CreateClippingButton(Model vpl, Assembly assembly)
        {
            var clippingImage = Resources.clipButton.ToBitmapImage();

            var clippingButton = new ToolBarButton
                (clippingImage
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

        private static void UseClippingPlane(Model vpl, bool useClipping)
        {
            if (useClipping)
            {
                if (!vpl.ClippingPlane1.Active)
                {
                    vpl.ClippingPlane1.Plane = Plane.ZX;
                    vpl.ClippingPlane1.Edit(Color.FromArgb(100, 0, 100, 100));
                }
            }
            else
            {
                vpl.ClippingPlane1.Cancel();
            }
        }
    }
}