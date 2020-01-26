using System.Collections;
using System.Collections.Generic;

namespace WorkRebalancer
{
    public interface IWorkAmount
    {
        object Ref { get; set; }
        void Set(float percentOfBaseValue);
        void Restore();
        bool HasWorkValue();
    }
}