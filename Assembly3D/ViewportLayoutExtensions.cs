using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using Environment = devDept.Eyeshot.Environment;

namespace Weingartner.Eyeshot.Assembly3D
{
    public static class ViewportLayoutExtensions
    {
        public static void InvalidateAndRegen(this Model vpl, bool withRegen=true)
        {
            if(withRegen)
                RegenAll(vpl);
            vpl.Invalidate();
        }

        /// <summary>
        /// Attempts to regenerate the viewportLayout so that all entities are
        /// correctly regenerated.
        /// </summary>
        /// <param name="vpl"></param>
        public static void RegenAll(this Model vpl)
        {
            if (vpl.renderContext == null)
                return;

            // As of Eyeshot v9.0.218, `EntityList.RegenAllCurved()` triggers
            // a recomputation of the bounding box of all entities,
            // updates `EntityList.VisualRefinementTolerance`
            // and then calls `EntityList.RegenAllCurved(EntityList.VisualRefinementTolerance)`.
            // But because `EntityList.Regen()` already estimates the bounding box
            // and updates `EntityList.VisualRefinementTolerance` we can directly call
            // `EntityList.RegenAllCurved(EntityList.VisualRefinementTolerance)`.
            vpl.Entities.Regen();

            vpl.Entities.RegenAllCurved(1e-5);
            vpl.Labels.Regen();
        }

        /// <summary>
        /// Validate the all the block reference entities refer to real blocks
        /// in the viewport layout
        /// </summary>
        /// <param name="viewportLayout"></param>
        public static void ValidateViewportLayout(this Model viewportLayout)
        {
            var entityList = viewportLayout.Entities;
            viewportLayout.ValidateViewportEntities(entityList);
            foreach (var block in viewportLayout.Blocks)
            {
                viewportLayout.ValidateViewportEntities(block.Entities);
            }
        }

        private static void ValidateViewportEntities(this Model viewportLayout, IEnumerable<Entity> entityList)
        {
            foreach (var entity in entityList)
            {
                var blockRef = entity as BlockReference;
                if (blockRef != null)
                {
                    if (!viewportLayout.Blocks.Contains(blockRef.BlockName))
                        throw new Exception($"Block {blockRef.BlockName} not found in viewport layout");

                    var block = viewportLayout.Blocks[blockRef.BlockName];

                    ValidateViewportEntities(viewportLayout, block.Entities);
                }
            }
        }

        /// <summary>
        /// Add a block to the viewportLayout and return a disposable that when
        /// disposed will remove the block
        /// </summary>
        /// <param name="vpl"></param>
        /// <param name="key"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public static IDisposable AddBlock(this Model vpl, string key, Block block)
        {
            vpl.Blocks.Add(key, block);
            return Disposable.Create(() => vpl.Blocks.Remove(key));
        }

        public static IDisposable AddEntity(this Model vpl, Entity entity)
        {
            vpl.Entities.Add(entity);
            return Disposable.Create(() => vpl.Entities.Remove(entity));
        }

        public static IDisposable SetCurrent(this Model vpl, BlockReference blkRef)
        {
            vpl.Entities.SetCurrent(blkRef);
            return Disposable.Create( () => vpl.Entities.SetCurrent(null));
        }

        public static IObservable<Environment.SelectionChangedEventArgs> SelectionChangedObservable(this Model viewport)
        {
            return Observable.FromEventPattern<Environment.SelectionChangedEventHandler, Environment.SelectionChangedEventArgs>
                ( h=>viewport.SelectionChanged+=h
                  , h=>viewport.SelectionChanged-=h
                )
                .Select(e=>e.EventArgs);
        }
    }

}
