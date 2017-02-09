using System;
using devDept.Eyeshot;

namespace Weingartner.Eyeshot.Assembly3D
{
    public class NamedBlock : Block
    {
        public string Name { get; private set; }
        public NamedBlock()
        {
            Name = Guid.NewGuid().ToString();
        }
    }
}
