using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JPP.Core.Annotations;

namespace JPP.Core
{
    public class Branch : INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Name { get; set; }

        public ObservableCollection<Branch> ChildBranches
        {
            get { return _childBranches; }
            set
            {
                _childBranches = value;
                OnPropertyChanged("ChildBranches");
            }
        }

        ObservableCollection<Branch> _childBranches;

        public ObservableCollection<Leaf> Children {
            get { return _children; }
            set
            {
                _children = value;
                OnPropertyChanged("Children");
            }
        }

        ObservableCollection<Leaf> _children;

        public List<object> Combined
        {
            get
            {
                List<object> temp = new List<object>();
                temp.AddRange(ChildBranches);
                temp.AddRange(Children);
                return temp;
            }
        }

        public Branch(string path)
        {
            Path = path;
            Name = path.Split('\\').Last(); ;

            ChildBranches = new ObservableCollection<Branch>();
            Children = new ObservableCollection<Leaf>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
