using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmWorkflow.BackendService.ManagementContracts.Emk;
using AlarmWorkflow.Windows.UIContracts.ViewModels;

namespace DispatchingTool
{
    public class ResourceItem : ViewModelBase
    {
        private bool _dispatched;

        public ResourceItem(EmkResource emkResource)
        {
            EmkResourceItem = emkResource;
        }

        /// <summary>
        /// Gets the underlying <see cref="EmkResource"/>-instance
        /// </summary>
        public EmkResource EmkResourceItem { get; private set; }

        /// <summary>
        /// Resources alarmed by the alarmsource can not be recalled. In this case this property is false.
        /// </summary>
        public bool CanGetDispatched { get; set; }

        /// <summary>
        /// Gets and sets whether the underlying resource is dispatched.
        /// </summary>
        public bool Dispatched
        {
            get { return _dispatched; }
            set
            {
                if (_dispatched != value)
                {
                    _dispatched = value;
                    OnPropertyChanged("Dispatched");
                }
            }
        }
    }
}
