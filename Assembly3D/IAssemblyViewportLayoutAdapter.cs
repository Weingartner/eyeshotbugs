using System;
using System.Reactive;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Labels;

namespace Weingartner.Eyeshot.Assembly3D
{
    public interface IAssemblyViewportLayoutAdapter
    {
        void AssertCorrectThread();
        void AddLabel(Label label);
        void RemoveLabel(Label label);

        IDisposable AddBlock(string blockReferenceBlockName, NamedBlock block);
        IDisposable AddBlockReference(BlockReference blockReference);

        void Invoke(Action action );
        T Invoke<T>(Func<T> action );

        void Invalidate(bool withRegen);

        IObservable<Unit> Ready();
        IDisposable SetCurrent(BlockReference o);
    }
}