﻿// This file is part of AlarmWorkflow.
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
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Windows.UIContracts.ViewModels;

namespace DispatchingTool
{
    /// <summary>
    /// A class containing all information required for dispatching resources. 
    /// (If a resource is allready dispatched and can "recalled" or is fixed by the alarming institute)
    /// </summary>
    public class ResourceItem : ViewModelBase
    {
        #region Fields

        private bool _dispatched;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of a ResourceItem.
        /// </summary>
        /// <param name="emkResource">The <see cref="EmkResource"/>. May not be null!</param>
        public ResourceItem(EmkResource emkResource)
        {
            Assertions.AssertNotNull(emkResource, "emkResource");

            EmkResourceItem = emkResource;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying <see cref="EmkResource"/>-instance
        /// </summary>
        public EmkResource EmkResourceItem { get; private set; }

        /// <summary>
        /// Resources alarmed by the alarming institute can not be recalled. In this case this property is false.
        /// </summary>
        public bool CanGetDispatched { get; set; }

        /// <summary>
        /// Gets and sets whether the resource is dispatched or not.
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

        #endregion
    }
}
