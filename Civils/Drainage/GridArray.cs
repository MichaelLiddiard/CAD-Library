using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    class GridArray<T> : IEnumerable<T>
        where T : class
    {
        private Dictionary<int, Dictionary<int, T>> Rows;

        public GridArray()
        {
            Rows = new Dictionary<int, Dictionary<int, T>>();
        }

        public T this[int column, int row]
        {
            get
            {
                if(Rows.ContainsKey(row))
                {
                    if(Rows[row].ContainsKey(column))
                    {
                        return Rows[row][column];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if(!Rows.ContainsKey(row))
                {
                    Rows[row] = new Dictionary<int, T>();
                }

                Rows[row][column] = value;                
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> array = new List<T>();
            foreach(Dictionary<int, T> r in Rows.Values)
            {
                foreach (T c in r.Values)
                {
                    array.Add(c);
                }
            }

            return array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
