using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    [Serializable]
    public class DirectoryPatchPhaseProgress
    {
        public enum States
        {
            Unstarted,
            Indeterminate,
            Started,
            Finished,
        }

        public States _State;
        public DiscreteProgress _Items;
        public DiscreteProgress _Size;

        public States State { get; set; }
        public DiscreteProgress Items { get; set; }
        public DiscreteProgress Size { get; set; }

        public DirectoryPatchPhaseProgress()
        {
            Items = new DiscreteProgress();
            Size = new DiscreteProgress();
        }

        public void SetTotals(long totalItems, long totalSize)
        {
            Items.Total = totalItems;
            Size.Total = totalSize;
        }

        public void AddItem(long size)
        {
            Items.Total += 1;
            Size.Total += size;
        }

        public void AdvanceItem(long size)
        {
            Items.Done += 1;
            Size.Done += size;
        }

        public override string ToString()
        {
            switch (State)
            {
                case States.Unstarted: return "not started";
                case States.Indeterminate: return "indeterminate";
                case States.Started: return string.Format("{0}/{1} B ({2}); item {3} of {4}", Size.Done, Size.Total, Size.Total != 0 ? Size.Fraction.ToString("P0") : "? %", Items.Done, Items.Total);
                case States.Finished: return "finished";
                default: throw new Exception();
            }
        }
    }
}
