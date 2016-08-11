using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShibugakiViewer.Models.Utility
{
    public class RandomNumber
    {
        private List<int> RandomSequence { get; set; }
        private int Index { get; set; }

        private static Random random = new Random();

        private int _fieldLength;
        public int Length
        {
            get { return this._fieldLength; }
            set
            {
                if (this._fieldLength != value)
                {
                    this._fieldLength = value;
                }
            }
        }

        public RandomNumber()
        {
            this.RandomSequence = new List<int>();
        }
        

        public int GetNext()
        {
            if (this.Length <= 0)
            {
                this.Clear();
                return 0;
            }

            while (true)
            {
                while (this.Index + 1 >= this.RandomSequence.Count)
                {
                    this.ExpandBack();
                }
                var result = this.RandomSequence[this.Index + 1];
                if (result < this.Length)
                {
                    return result;
                }
                this.Clear();
            }
        }

        public int MoveNext()
        {
            var next = this.GetNext();
            this.Index++;
            return next;
        }

        public int MovePrev()
        {
            if (this.Length <= 0)
            {
                this.Clear();
                return 0;
            }
            while (true)
            {
                this.Index--;

                while (this.Index < 0)
                {
                    var length = this.ExpandForward();
                    this.Index += length;
                }

                var result = this.RandomSequence[this.Index];
                if (result < this.Length)
                {
                    return result;
                }
                this.Clear();
            }
        }

        public void Clear()
        {
            this.Index = 0;
            this.RandomSequence.Clear();
        }


        public void ReplaceIfDifferent(int value)
        {
            if (this.Index >= this.RandomSequence.Count)
            {
                return;
            }
            this.RandomSequence[this.Index] = value;
        }
        

        private void ExpandBack()
        {
            this.RandomSequence.AddRange(this.GenerateSequence());
        }
        private int ExpandForward()
        {
            var array = this.GenerateSequence();
            this.RandomSequence.InsertRange(0, array);
            return array.Length;
        }

        private int[] GenerateSequence()
        {
            switch (this.Length)
            {
                case 0:
                case 1:
                    return new[] { 0 };

                case 2:
                    return new int[] { 0, 1 };

                default:
                    return Enumerable
                        .Range(0, this.Length)
                        .OrderBy(x => random.Next(this.Length))
                        .ToArray();
            }
        }
    }
}
