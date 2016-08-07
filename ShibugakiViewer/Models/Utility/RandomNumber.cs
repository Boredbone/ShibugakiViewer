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
        //private int Number { get; set; }

        //private List<int> History { get; set; }

        //private bool _fieldIsRandom;
        //public bool IsRandom
        //{
        //    get { return this._fieldIsRandom; }
        //    set
        //    {
        //        if (this._fieldIsRandom != value)
        //        {
        //            this._fieldIsRandom = value;
        //            if (this._fieldIsRandom)
        //            {
        //                this.RefreshRandomSequence();
        //            }
        //        }
        //    }
        //}


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
                    //this.RefreshRandomSequence();
                }
            }
        }

        public RandomNumber()
        {
            this.RandomSequence = new List<int>();
        }

        //public RandomNumber(int length, int initialNumber)
        //{
        //    this.Number = initialNumber;
        //    this.Length = length;
        //}
        //public RandomNumber(int length) : this(length, 0)
        //{
        //    //this.Number = 0;
        //    //this.Length = length;
        //}


        public int GetNext()
        {
            if (this.Length <= 0)
            {
                this.Clear();
                return 0;
            }

            while (true)
            {
                while (this.Index >= this.RandomSequence.Count)
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

        /*
        public int GetNumber(int index)
        {
            if (index < 0)
            {
                return 0;
            }

            if (this.History == null)
            {
                this.History = Enumerable.Range(this.Number, index).ToList();
                //this.History[this.History.Count - 1] = this.Number;
            }

            while (index >= this.History.Count)
            {
                var number = this.GenerateNextNumber();
                this.History.Add(number);
            }
            //if (index >= this.History.Count)
            //{
            //    var number = this.GenerateNextNumber();
            //    this.History.Add(number);
            //    return number;
            //}
            return this.History[index];
        }

        private int GenerateNextNumber()
        {
            int number;
            if (this.IsRandom)
            {
                this.Index++;
                if (this.Index >= this.RandomSequence.Length)
                {
                    this.RefreshRandomSequence();
                    if (this.Length > 2 && this.Number == this.RandomSequence[0])
                    {
                        this.Index++;
                    }
                }
                number = this.RandomSequence[this.Index];
            }
            else
            {
                number = this.History.Last() + 1;
                if (number >= this.Length)
                {
                    number = 0;
                }
            }
            this.Number = number;
            return number;
        }*/

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
