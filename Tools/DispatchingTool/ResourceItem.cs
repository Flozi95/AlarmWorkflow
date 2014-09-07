// This file is part of AlarmWorkflow.
// 
// AlarmWorkflow is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// AlarmWorkflow is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AlarmWorkflow.  If not, see <http://www.gnu.org/licenses/>.

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
