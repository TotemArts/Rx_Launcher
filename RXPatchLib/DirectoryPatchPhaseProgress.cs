using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    [Serializable]
    public class DirectoryPatchPhaseProgressItem
    {
        private readonly DirectoryPatchPhaseProgress _parent;
        private long _done;
        private long _total;

        public DirectoryPatchPhaseProgressItem(DirectoryPatchPhaseProgress parent)
        {
            _parent = parent;
            _done = 0;
            _total = 0;
        }
        public long Done
        {
            get
            {
                return _done;
            }
            set
            {
                _parent.Size.Done += value - _done;
                _done = value;
            }
        }
        public long Total
        {
            get
            {
                return _total;
            }
            set
            {
                _parent.Size.Total += value - _total;
                _total = value;
            }
        }
        public void Finish()
        {
            Done = Total;
            ++_parent.Items.Done;
        }
    }
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

        public States State { get; set; }
        public DiscreteProgress Items { get; set; }
        public DiscreteProgress Size { get; set; }
        public byte DownloadThreads { get; set; }

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

        public DirectoryPatchPhaseProgressItem AddItem()
        {
            Items.Total += 1;
            return new DirectoryPatchPhaseProgressItem(this);
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
