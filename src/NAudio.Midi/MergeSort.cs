using System;
using System.Collections.Generic;

namespace NAudio.Utils
{
    class MergeSort
    {
        /// <summary>
        /// In-place and stable implementation of MergeSort
        /// </summary>
        static void Sort<T>(IList<T> list, int lowIndex, int highIndex, IComparer<T> comparer)
        {
            if (lowIndex >= highIndex)
            {
                return;
            }


            int midIndex = (lowIndex + highIndex) / 2;


            // Partition the list into two lists and Sort them recursively
            Sort(list, lowIndex, midIndex, comparer);
            Sort(list, midIndex + 1, highIndex, comparer);

            // Merge the two sorted lists
            int endLow = midIndex;
            int startHigh = midIndex + 1;


            while ((lowIndex <= endLow) && (startHigh <= highIndex))
            {
                // MRH, if use < 0 sort is not stable
                if (comparer.Compare(list[lowIndex], list[startHigh]) <= 0)
                {
                    lowIndex++;
                }
                else
                {
                    // list[lowIndex] > list[startHigh]
                    // The next element comes from the second list, 
                    // move the list[start_hi] element into the next 
                    //  position and shuffle all the other elements up.
                    T t = list[startHigh];

                    for (int k = startHigh - 1; k >= lowIndex; k--)
                    {
                        list[k + 1] = list[k];
                    }

                    list[lowIndex] = t;
                    lowIndex++;
                    endLow++;
                    startHigh++;
                }
            }
        }

        /// <summary>
        /// MergeSort a list of comparable items
        /// </summary>
        public static void Sort<T>(IList<T> list) where T : IComparable<T>
        {
            Sort(list, 0, list.Count - 1, Comparer<T>.Default);
        }

        /// <summary>
        /// MergeSort a list 
        /// </summary>
        public static void Sort<T>(IList<T> list, IComparer<T> comparer)
        {
            Sort(list, 0, list.Count - 1, comparer);
        }
    }
}
