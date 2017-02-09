using System;
using System.Drawing;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Weingartner.Eyeshot.Assembly3D;

namespace Assembly3DDemo
{
    public class Ring : Assembly3D
    {
        [Reactive] public bool IsSphere { get; set;}

        public Ring()
        {
            this.WhenAnyValue(p => p.IsSphere)
                .Subscribe(v => Replace(MakeRing(v)));
        }
        
        private static Assembly3D MakeRing(bool isSphere)
        {
            var d = 10;
            var ring = new Assembly3D();
            for (int i = 0; i < d; i++)
            {

                var mesh = isSphere
                               ? Mesh.CreateSphere(8, 10, 10)
                               : Mesh.CreateBox(8, 8, 8);
                mesh.SetColor(Color.Green);
                mesh.Translate(20, 0, 0);
                mesh.Rotate(Math.PI * 2 / d * i, Vector3D.AxisZ);
                ring.Add(mesh);
            }
            return ring;
        }
    }
}