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

        #region De-/Constructor

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

        #endregion

        #region Event-Handler

        private void timer_Tick(object sender, EventArgs e)
        {
            Task.Factory.StartNew(Update);
        }

        #endregion

        #region Methods

        private void Update()
        {
            try
            {
                //Reconnect to the service if the last connection was not successful.
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

                Error = false;

                //Get last operation. At the moment only dispatching works for the last operation.
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
                else
                {
                    App.Current.Dispatcher.Invoke(Resources.Clear);
                    CurrentOperation = null;
                    OnPropertyChanged("CurrentOperation");
                    return;
                }

                //Get the emk-configuration 
                using (var service = ServiceFactory.GetServiceWrapper<IEmkService>())
                {
                    List<EmkResource> emkResources = service.Instance.GetAllResources().Where(x => x.IsActive).ToList();
                    IList<OperationResource> alarmedResources = service.Instance.GetFilteredResources(CurrentOperation.Resources);
                    foreach (EmkResource emkResource in emkResources)
                    {
                        ResourceItem resourceItem = new ResourceItem(emkResource);

                        //Resources alarmed by the alarming institute can not be dispatched or recalled!
                        resourceItem.CanGetDispatched = !alarmedResources.Any(x => emkResource.IsMatch(x));
                        App.Current.Dispatcher.Invoke(() => Resources.Add(resourceItem));
                    }
                }

                //Get the resources which are allready dispatched to current operation.
                string[] dispatchedResources = _disposingService.Instance.GetDispatchedResources(CurrentOperation.Id);
                foreach (ResourceItem item in Resources)
                {
                    if (dispatchedResources.Contains(item.EmkResourceItem.Id))
                    {
                        item.Dispatched = true;
                    }
                }
            }
            catch (Exception ex)
            {
                //This exceptions could occur if the service connection was lost! All other exceptions are not ok --> Throw them!
                if (ex is EndpointNotFoundException || ex is InvalidOperationException || ex is CommunicationException)
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

            //Update the properties
            App.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged("Error");
                OnPropertyChanged("Resources");
                OnPropertyChanged("CurrentOperation");
            });
        }

        #endregion

        #region Implementation of IDispositioningServiceCallback

        /// <summary>
        /// Called by the service after a resource was dispatched or recalled.
        /// </summary>
        /// <param name="evt">The event data that describes the event.</param>
        public void OnEvent(DispositionEventArgs evt)
        {
            if (CurrentOperation != null && evt.OperationId == CurrentOperation.Id)
            {
                if (evt.Action == DispositionEventArgs.ActionType.Dispatch)
                {
                    using (var service = ServiceFactory.GetServiceWrapper<IEmkService>())
                    {
                        EmkResource emkResource = service.Instance.GetAllResources().FirstOrDefault(x => x.IsActive && x.Id == evt.EmkResourceId);
                        if (emkResource != null)
                        {
                            ResourceItem resourceItem = new ResourceItem(emkResource);
                            resourceItem.CanGetDispatched = true;
                            resourceItem.Dispatched = true;
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                OnPropertyChanged("Resources");
                                Resources.Add(resourceItem);
                            });
                        }
                    }
                }
                else if (evt.Action == DispositionEventArgs.ActionType.Recall)
                {
                    ResourceItem resource = Resources.FirstOrDefault(x => x.EmkResourceItem.Id == evt.EmkResourceId);
                    if (resource != null)
                    {
                        Resources.Remove(resource);
                    }
                }
            }
        }

        #endregion

        #region Implementation of IOperationServiceCallback

        /// <summary>
        /// Called when an operation was acknowledged.
        /// </summary>
        /// <param name="id">The id of the operation that was acknowledged.</param>
        public void OnOperationAcknowledged(int id)
        {
            if (CurrentOperation != null && CurrentOperation.Id == id)
            {
                CurrentOperation = null;
                App.Current.Dispatcher.Invoke(() =>
                {
                    Resources.Clear();
                    OnPropertyChanged("CurrentOperation");
                });
            }
        }

        #endregion
    }
}
