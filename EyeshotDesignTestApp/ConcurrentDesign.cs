using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Graphics;
using ReactiveUI;
using ToolBar = devDept.Eyeshot.Control.ToolBar;

namespace EyeshotDesignTestApp
{
    public class ConcurrentDesign : ContentControl
    {
        private static readonly ConcurrentBag<Design> Designs = new ConcurrentBag<Design>();

        public static readonly DependencyProperty ViewportContentProperty =
            DependencyProperty.Register
                ( nameof(ViewportContent)
                  , typeof(Entity[])
                  , typeof(ConcurrentDesign)
                  , new PropertyMetadata( Array.Empty<Entity>() ) );

        public Entity[] ViewportContent
        {
            get => (Entity[])GetValue( ViewportContentProperty );
            set => SetValue( ViewportContentProperty, value );
        }

        private static readonly Transformation Identity = new Identity();
        public ConcurrentDesign()
        {
            this.LoadUnloadHandlerFromReactiveUi
                ( () =>
                 {
                     var design = Designs.TryTake( out var d ) ? d : Builder.BuildDesign();
                     design.CreateControl();
                     Content = design;

                     var disp = Disposable.Empty;
                     

                     var d1 = this.WhenAnyValue( x => x.ViewportContent )
                                .WhereNotNull()
                                .Do( _ => disp.Dispose() )
                                // ctor
                                .Select( e =>
                                {
                                    var block = new Block ( Guid.NewGuid() .ToString() );
                                    var blockRef = new BlockReference( Identity, block.Name );
                                    block.Entities.AddRange( e );
                                    
                                    var designDoc = new DesignDocument();
                                    disp = AddStuff( designDoc, block, blockRef );
                                    return designDoc;
                                } )
                                .Do( doc => design.LoadDocument( doc ) )
                                .Subscribe();

                     var d2 = Disposable.Create( (ctl: this, design), t =>
                     {
                         t.ctl.Content = null;

                         Designs.Add( t.design );
                     } );
                     return new CompositeDisposable( d1, d2 );
                 }
                );

        }

        private static IDisposable AddStuff(DesignDocument designDoc, Block block, BlockReference blockRef)
        {
            var cd = new CompositeDisposable();
            designDoc.Blocks.Add( block );
            cd.Add( Disposable.Create((designDoc, block), t => t.designDoc.Blocks.Remove( t.block.Name )) );
            designDoc.RootBlock.Entities.Add( blockRef );
            cd.Add(Disposable.Create((designDoc.RootBlock,blockRef), t => t.RootBlock.Entities.Remove( t.blockRef )));
            return cd;
        }
    }

    public static class Builder
    {
        public static Design BuildDesign()
        {
            var design = new Design();
            var toolBar = new ToolBar
                          {
                              Position = ToolBar.positionType.HorizontalTopCenter
                              , Height   = 10
                          };
            toolBar.Buttons.Add(new ZoomFitToolBarButton());

            var vp = new Viewport
                     {
                         Background = new BackgroundSettings
                                      {
                                          StyleMode = backgroundStyleType.Solid,
                                          ColorTheme = colorThemeType.Auto,
                                          TopColor = Brushes.AntiqueWhite,
                                      },
                         ToolBars = new ObservableCollection<ToolBar>
                                    {
                                        toolBar
                                    },
                     };

            design.Viewports.Add(vp);

            return design;

        }

    }
    public static class Extensions
    {
        public static IDisposable LoadUnloadHandlerFromReactiveUi(this FrameworkElement control, Func<IDisposable> block)
        {
            var cleanup = new SerialDisposable();

            // pulled from ReactiveUI ActivationForViewFetcher.GetActivationForView
            var viewLoaded = control.Events().Loaded.Select( _ => true );
            var isHitTestVisible = control.Events().IsHitTestVisibleChanged.Select( args => (bool)args.NewValue );
            var viewUnloaded = control.Events().Unloaded.Select( _ => false );

            IObservable<bool>? windowActive = control is not Window window 
                ? Observable.Empty<bool>() 
                : window.Events().Closed.Select(_ => false);

            return viewLoaded
                   .Merge( viewUnloaded )
                   .Merge( isHitTestVisible )
                   .Merge( windowActive )
                   .DistinctUntilChanged()
                   .Subscribe
                       ( activated =>
                       {
                           // make sure the cleanup happens before we invoke the block to get the new disposables
                           cleanup.Disposable = Disposable.Empty;

                           if (activated)
                           {
                               cleanup.Disposable = block();
                           }
                       } );
        }
    }
}
