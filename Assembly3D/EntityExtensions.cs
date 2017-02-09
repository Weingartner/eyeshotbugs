using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Disposables;
using devDept.Eyeshot.Entities;
using devDept.Geometry;

namespace Weingartner.Eyeshot.Assembly3D
{
    public static class EntityExtensions
    {

        /// <summary>
        /// Sets a group of entities to the same color
        /// </summary>
        /// <param name="source"></param>
        /// <param name="color"></param>
        /// <param name="lineWeight">todo: describe lineWeight parameter on SetColorAndWeight</param>
        public static void SetColorAndWeight( this IEnumerable<Entity> source, Color color, int lineWeight = 1 )
        {
            foreach ( var e in source )
            {
                e.SetColorAndWeight(color,lineWeight);
            }
        }

        public static Entity SetColorAndWeight(this Entity e, Color color, int lineWeight = 2, string lineType="")
        {
            SetColor(e, color);
            if (lineWeight == 0)
                lineWeight = 1;
            e.LineWeight = lineWeight;
            e.LineWeightMethod = colorMethodType.byEntity;
            if (lineType != string.Empty)
            {
                e.LineTypeName = lineType;
                e.LineTypeMethod = colorMethodType.byEntity;
            }
            return e;
        }

        public static IDisposable SetColorAndWeightTransactional(this Entity e, Color color, int lineWeight = 2, string lineType ="")
        {
            var colorUndo = e.Color;
            var weightUndo = e.LineWeight;
            var d = Disposable.Create(() => e.SetColorAndWeight(colorUndo, (int) weightUndo, lineType));
            e.SetColorAndWeight(color, lineWeight, lineType);

            return d;
        }

        public static void SetColor(this Entity e, Color color)
        {
            e.Color = color;
            e.ColorMethod = colorMethodType.byEntity;
        }


        /// <summary>
        /// Create a block reference automatically for the block. The block
        /// name is created with a unique identifier for the block object.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static BlockReference BlockReference( this NamedBlock block )
        {
            return new BlockReference(new Identity(), block.Name);
        }

    }

}
