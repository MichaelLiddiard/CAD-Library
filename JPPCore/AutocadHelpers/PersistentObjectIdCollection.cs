using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections;
using System.Collections.Generic;

namespace JPP.Core
{
    public class PersistentObjectIdCollection : IEnumerable
    {
        public List<long> Pointers { get; set; }

        private ObjectIdCollection collection;

        public ObjectIdCollection Collection
        {  get
            {
                if (collection == null)
                {
                    BuildCollection();
                }

                return collection;
            }
        }

        public PersistentObjectIdCollection()
        {
            Pointers = new List<long>();
            collection = new ObjectIdCollection();
        }

        private void BuildCollection()
        {
            collection = new ObjectIdCollection();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            
            foreach (long ptr in Pointers)
            {
                ObjectId newObj;
                if(acCurDb.TryGetObjectId(new Handle(ptr), out newObj))
                {
                    collection.Add(newObj);
                }
            }
        }


        //TODO: Find out why objects are being added twice
        public void Add(long id)
        {            
            Pointers.Add(id);
            BuildCollection();
        }

        public void Add(ObjectId id)
        {
            Add(id.Handle.Value);
         
        }

        public void Add(object id)
        {
            Add((long)id);

        }

        //Why are both needed????
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Pointers.GetEnumerator();
        }        
    }
}
