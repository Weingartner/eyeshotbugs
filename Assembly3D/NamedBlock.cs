using System;
using devDept.Eyeshot;

namespace Weingartner.EyeShot.Assembly3D
{
    [Serializable]
    public class NamedBlock : Block
    {
        public NamedBlock() : base(Guid.NewGuid().ToString())
        {
        }
    }
}
