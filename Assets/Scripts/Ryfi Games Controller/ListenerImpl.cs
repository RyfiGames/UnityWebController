using System;
using Quobject.EngineIoClientDotNet.ComponentEmitter;

namespace RyfiGames.Controller
{
    public class ListenerImpl : IListener, IComparable<IListener>
    {
        private static int id_counter = 0;
        private int Id;
        public delegate void TwoParam(object a, object b);
        public delegate void ThreeParam(object a, object b, object c);
        public delegate void FourParam(object a, object b, object c, object d);
        private readonly Action<object> fn1;
        private readonly TwoParam fn2;
        private readonly ThreeParam fn3;
        private readonly FourParam fn4;

        public ListenerImpl(Action<object> fn)
        {
            this.fn1 = fn;
            this.Id = ListenerImpl.id_counter++;
        }
        public ListenerImpl(TwoParam tp)
        {
            this.fn2 = tp;
        }
        public ListenerImpl(ThreeParam tp)
        {
            this.fn3 = tp;
        }
        public ListenerImpl(FourParam tp)
        {
            this.fn4 = tp;
        }

        public void Call(params object[] args)
        {
            if (this.fn1 != null)
                this.fn1(args.Length != 0 ? args[0] : (object)null);
            else if (this.fn2 != null)
                this.fn2.Invoke(args[0], args[1]);
            else if (this.fn3 != null)
                this.fn3.Invoke(args[0], args[1], args[2]);
            else
                this.fn4.Invoke(args[0], args[1], args[2], args[3]);
        }

        public int CompareTo(IListener other)
        {
            return this.GetId().CompareTo(other.GetId());
        }

        public int GetId()
        {
            return this.Id;
        }
    }
}
