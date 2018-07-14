using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    class SparseGridArray<T>
    {
        private Dictionary<int, Dictionary<int, T>> Rows;

        public SparseGridArray()
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
                        return default(T);
                    }
                }
                else
                {
                    return default(T);
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
    }
}
