using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.PeerResolvers;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using AlarmWorkflow.Backend.ServiceContracts.Communication;
using AlarmWorkflow.BackendService.DispositioningContracts;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Windows.UIContracts;
using AlarmWorkflow.Windows.UIContracts.ViewModels;
using AlarmWorkflow.BackendService.ManagementContracts;
using AlarmWorkflow.BackendService.ManagementContracts.Emk;

namespace DispatchingTool
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class ViewModel : ViewModelBase, IDispositioningServiceCallback, IOperationServiceCallback
    {
        public IList<ResourceItem> Resources { get; set; }

        public ICommand DispatchCommand { get; set; }

        public Operation CurrentOperation { get; set; }

        public bool DispatchCommand_CanExecute(object param)
        {
            return true;
        }

        public void DispatchCommand_Execute(object param)
        {
            using (var service = ServiceFactory.GetCallbackServiceWrapper<IDispositioningService>(this))
            {
                string id = param as string;
                ResourceItem item = Resources.FirstOrDefault(x => x.EmkResourceItem.Id == id);
                if (service.Instance.GetDispatchedResources(CurrentOperation.Id).Contains(id))
                {
                    service.Instance.Recall(CurrentOperation.Id, id);
                    item.Dispatched = false;
                }
                else
                {
                    service.Instance.Dispatch(CurrentOperation.Id, id);
                    item.Dispatched = true;
                }
            }
        }

        public ViewModel()
        {
            Resources = new List<ResourceItem>();
            DispatchCommand = new RelayCommand(DispatchCommand_Execute, DispatchCommand_CanExecute);
            Update();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Constants.OfpInterval);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Update();
        }

        private void Update()
        {
            try
            {
                using (var service = ServiceFactory.GetCallbackServiceWrapper<IOperationService>(this))
                {
                    IList<int> operationIds = service.Instance.GetOperationIds(Constants.OfpMaxAge, Constants.OfpOnlyNonAcknowledged, 1);
                    int operationId = operationIds.FirstOrDefault();
                    if (operationId != 0)
                    {
                        if (CurrentOperation == null || CurrentOperation.Id != operationId)
                        {
                            CurrentOperation = service.Instance.GetOperationById(operationId);
                            Resources.Clear();
                        }
                        else
                        {
                            return;
                        }
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
                        Resources.Add(resourceItem);
                    }
                }
                using (var service = ServiceFactory.GetCallbackServiceWrapper<IDispositioningService>(this))
                {
                    string[] dispatchedResources = service.Instance.GetDispatchedResources(CurrentOperation.Id);
                    foreach (ResourceItem item in Resources)
                    {
                        if (dispatchedResources.Contains(item.EmkResourceItem.Id))
                        {
                            item.Dispatched = true;
                        }
                    }
                }
                App.Current.Dispatcher.Invoke(() => OnPropertyChanged("Resources"));
            }
            catch (EndpointNotFoundException ex)
            {
                UIUtilities.ShowError(Properties.Resources.NoServiceConnection);
                Environment.Exit(1);
            }
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
