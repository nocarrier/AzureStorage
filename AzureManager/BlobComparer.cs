using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureManager
{
    public static class Comparer
    {
        public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }
    }


    public class Blob : IComparable
    {
        public string Name;
        public string Length;
        public string LastModified;

        #region Contructors

        public Blob(string name, string lastModified, string length)
        {
            this.Name = name;
            this.LastModified = lastModified;
            this.Length = length;
        }

        #endregion

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            var blob = obj as Blob;

            if (blob != null)
            {
                var p2 = blob;
                return String.Compare(Name, p2.Name,
                    StringComparison.Ordinal);
            }
            else
                throw new ArgumentException("No Compare for you! This is not a blob...");
        }

        #endregion

        public int CompareTo(Blob blob2, BlobComparer.ComparisonType comparisonMethod)
        {
            switch (comparisonMethod)
            {
                case BlobComparer.ComparisonType.Name:
                    return System.String.Compare(Name, blob2.Name,
                        StringComparison.Ordinal);

                case BlobComparer.ComparisonType.LastModified:
                    return System.String.Compare(LastModified, blob2.LastModified,
                        StringComparison.Ordinal);

                case BlobComparer.ComparisonType.Length:
                    return System.String.Compare(Length, blob2.Length,
                        StringComparison.Ordinal);

                default:
                    return System.String.Compare(Name, blob2.Name,
                        StringComparison.Ordinal);

            }

        }
        
        #region ToString()

        public override string ToString()
        {
            return String.Format("Name: {0} LastModified: {1} Length: {2}", Name, LastModified, Length);
        }

        #endregion
    }

    public class BlobComparer : IComparer<Blob>
    {
        private readonly bool _sortAscending;
        private readonly string _columnToSortOn;

        public enum ComparisonType { Name = 1, LastModified = 2, Length = 3 }
        
        public ComparisonType ComparisonMethod;
        
        public BlobComparer(bool sortAscending, string columnToSortOn)
        {
            _sortAscending = sortAscending;
            _columnToSortOn = columnToSortOn;
        }

        public int Compare(Blob source, Blob destination)
        {
            if (source == null && destination == null) return 0;
            if (source == null) return ApplySortDirection(-1);
            if (destination == null) return ApplySortDirection(1);

            switch (_columnToSortOn)
            {
                case "Name":
                    return ApplySortDirection(SortByName(source, destination));
                case "LastModified":
                    return ApplySortDirection(SortByModifiedDate(source, destination));
                case "Length":
                    return ApplySortDirection(SortByLength(source, destination));
                default:
                    throw new ArgumentOutOfRangeException(
                        string.Format("Can't sort on column {0}, you cwazy wabbit..", _columnToSortOn));
            }
        }

        private int _SortByModifiedDate(Blob x, Blob y)
        {
            return String.Compare(x.LastModified, y.LastModified, 
                StringComparison.Ordinal);
        }

        /// <summary>
        /// If the date is missing, then the blob file is 
        /// either pending, aborted, or has failed....
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int SortByModifiedDate(Blob x, Blob y)
        {
            if (String.IsNullOrEmpty(x.LastModified))
                x.LastModified = DateTime.MaxValue.ToShortDateString();

            if (String.IsNullOrEmpty(y.LastModified))
                y.LastModified = DateTime.MaxValue.ToShortDateString();

            var nx = DateTime.Parse(x.LastModified);
            var ny = DateTime.Parse(y.LastModified);

            return nx.CompareTo(ny);
        }

        private int SortByLength(Blob x, Blob y)
        {
            var lenx = Convert.ToInt32(x.Length);
            var leny = Convert.ToInt32(y.Length);

            return lenx.CompareTo(leny);
        }

        private int SortByName(Blob x, Blob y)
        {
            var name = String.Compare(x.Name, y.Name, StringComparison.Ordinal);
            return name != 0 ? name : String.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        private int ApplySortDirection(int result)
        {
            return _sortAscending ? result : (result * -1);
        }

    }
}