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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using AlarmWorkflow.Backend.ServiceContracts.Communication;
using AlarmWorkflow.BackendService.DispositioningContracts;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Diagnostics;
using AlarmWorkflow.Windows.UIContracts.ViewModels;
using AlarmWorkflow.BackendService.ManagementContracts;
using AlarmWorkflow.BackendService.ManagementContracts.Emk;

namespace DispatchingTool
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class ViewModel : ViewModelBase, IDispositioningServiceCallback, IOperationServiceCallback
    {
        #region Fields

        private WrappedService<IDispositioningService> _disposingService;
        private WrappedService<IOperationService> _operationService;

        #endregion

        #region Properties
        
        /// <summary>
        /// A collection of <see cref="ResourceItem"/>s.
        /// </summary>
        public ObservableCollection<ResourceItem> Resources { get; set; }

        /// <summary>
        /// Gets or sets the current <see cref="Operation"/>.
        /// At the moment public. Maybe an information in the main window would be helpfull (which operation currently is disblayed)
        /// </summary>
        public Operation CurrentOperation { get; set; }

        /// <summary>
        /// Gets or sets if an error is currently "available"
        /// </summary>
        public bool Error { get; private set; }

        #endregion

        #region DispatchCommand

        /// <summary>
        /// The command assigned to the resource buttons.
        /// </summary>
        public ICommand DispatchCommand { get; set; }

        /// <summary>
        /// Fired when a resource gets clicked.
        /// </summary>
        /// <param name="param">Should be the id of the sending button</param>
        public void DispatchCommand_Execute(object param)
        {
            string id = param as string;
            ResourceItem item = Resources.FirstOrDefault(x => x.EmkResourceItem.Id == id);
            if (item == null)
            {
                //Actually this should not happen!
                //No idea why this could be so.
                return;
            }
            if (_disposingService.Instance.GetDispatchedResources(CurrentOperation.Id).Contains(id))
            {
                _disposingService.Instance.Recall(CurrentOperation.Id, id);
                item.Dispatched = false;
            }
            else
            {
                _disposingService.Instance.Dispatch(CurrentOperation.Id, id);
                item.Dispatched = true;
            }
        }

        #endregion

        public ViewModel()
        {
            Resources = new ObservableCollection<ResourceItem>();
            DispatchCommand = new RelayCommand(DispatchCommand_Execute);
            Error = false;

            try
            {
                _disposingService = ServiceFactory.GetCallbackServiceWrapper<IDispositioningService>(this);
                _operationService = ServiceFactory.GetCallbackServiceWrapper<IOperationService>(this);
            }
            catch (EndpointNotFoundException)
            {
                Error = true;
            }

            Task.Factory.StartNew(Update);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Constants.OfpInterval);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        ~ViewModel()
        {
            if (_disposingService != null)
            {
                _disposingService.Dispose();
            }
            if (_operationService != null)
            {
                _operationService.Dispose();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Task.Factory.StartNew(Update);
        }

        private void Update()
        {

            try
            {
                if (Error)
                {
                    if (_disposingService != null)
                    {
                        _disposingService.Dispose();
                    }
                    _disposingService = ServiceFactory.GetCallbackServiceWrapper<IDispositioningService>(this);

                    if (_operationService != null)
                    {
                        _operationService.Dispose();
                    }
                    _operationService = ServiceFactory.GetCallbackServiceWrapper<IOperationService>(this);

                }
                IList<int> operationIds = _operationService.Instance.GetOperationIds(Constants.OfpMaxAge, Constants.OfpOnlyNonAcknowledged, 1);
                int operationId = operationIds.FirstOrDefault();
                if (operationId != 0)
                {
                    if (CurrentOperation == null || CurrentOperation.Id != operationId)
                    {
                        CurrentOperation = _operationService.Instance.GetOperationById(operationId);
                        App.Current.Dispatcher.Invoke(Resources.Clear);
                    }
                    else
                    {
                        return;
                    }
                }

                using (var service = ServiceFactory.GetServiceWrapper<IEmkService>())
                {
                    List<EmkResource> emkResources = service.Instance.GetAllResources().Where(x => x.IsActive).ToList();
                    IList<OperationResource> alarmedResources = service.Instance.GetFilteredResources(CurrentOperation.Resources);
                    foreach (EmkResource emkResource in emkResources)
                    {
                        ResourceItem resourceItem = new ResourceItem(emkResource);
                        resourceItem.CanGetDispatched = !alarmedResources.Any(x => emkResource.IsMatch(x));
                        App.Current.Dispatcher.Invoke(() => Resources.Add(resourceItem));
                    }
                }

                string[] dispatchedResources = _disposingService.Instance.GetDispatchedResources(CurrentOperation.Id);
                foreach (ResourceItem item in Resources)
                {
                    if (dispatchedResources.Contains(item.EmkResourceItem.Id))
                    {
                        item.Dispatched = true;
                    }
                }
                Error = false;
            }

            catch (Exception ex)
            {
                if (ex is EndpointNotFoundException || ex is InvalidOperationException)
                {
                    CurrentOperation = null;
                    Error = true;
                    App.Current.Dispatcher.Invoke(Resources.Clear);
                }
                else
                {
                    Logger.Instance.LogException(this, ex);
                    throw ex;
                }
            }

            App.Current.Dispatcher.Invoke(() => OnPropertyChanged("Error"));
            App.Current.Dispatcher.Invoke(() => OnPropertyChanged("Resources"));

        }

        #region Implementation of IDispositioningServiceCallback

        public void OnEvent(DispositionEventArgs evt)
        {
        }

        #endregion

        #region Implementation of IOperationServiceCallback

        public void OnOperationAcknowledged(int id)
        {
        }

        #endregion
    }
}
