using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weingartner.Eyeshot.Assembly3D
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using System.Runtime.Serialization;
    using devDept.Eyeshot;
    using devDept.Eyeshot.Entities;
    using devDept.Geometry;

    namespace Weingartner.EyeShot
    {
        public class BlockReferenceEx : BlockReference
        {

            private readonly Subject<DrawParams> _DrawSubject = new Subject<DrawParams>();

            public BlockReferenceEx(double x, double y, double z, string blockName, double rotationAngleInRadians) : base(x, y, z, blockName, rotationAngleInRadians)
            {
            }

            public BlockReferenceEx(double x, double y, double z, string blockName, linearUnitsType globalUnits, BlockKeyedCollection blocks, double rotationAngleInRadians) : base(x, y, z, blockName, globalUnits, blocks, rotationAngleInRadians)
            {
            }

            public BlockReferenceEx(double x, double y, double z, string blockName, double sx, double sy, double sz, double rotationAngleInRadians) : base(x, y, z, blockName, sx, sy, sz, rotationAngleInRadians)
            {
            }

            public BlockReferenceEx(Point3D insPoint, string blockName, double rotationAngleInRadians) : base(insPoint, blockName, rotationAngleInRadians)
            {
            }

            public BlockReferenceEx(Point3D insPoint, string blockName, double sx, double sy, double sz, double rotationAngleInRadians) : base(insPoint, blockName, sx, sy, sz, rotationAngleInRadians)
            {
            }

            public BlockReferenceEx(string blockName) : base(blockName)
            {
            }

            public BlockReferenceEx(Transformation t, string blockName) : base(t, blockName)
            {
            }

            public BlockReferenceEx(Transformation t, string blockName, linearUnitsType globalUnits, BlockKeyedCollection blocks) : base(t, blockName, globalUnits, blocks)
            {
            }

            public BlockReferenceEx(BlockReference another) : base(another)
            {
            }

            public BlockReferenceEx(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }

            public IObservable<DrawParams> DrawObservable => _DrawSubject;

            protected override void Draw(DrawParams data)
            {
                base.Draw(data);
            }

            protected override void Draw<T>(T myParams, Model.drawCallback<T> drawCall)
            {
                base.Draw(myParams, drawCall);
            }


        }
    }
}
