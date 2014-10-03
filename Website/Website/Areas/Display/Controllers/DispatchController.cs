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
using System.Linq;
using System.Net;
using System.Web.Mvc;
using AlarmWorkflow.Backend.ServiceContracts.Communication;
using AlarmWorkflow.BackendService.ManagementContracts;
using AlarmWorkflow.BackendService.ManagementContracts.Emk;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.BackendService.DispositioningContracts;
using AlarmWorkflow.Website.Reports.Areas.Display.Models;

namespace AlarmWorkflow.Website.Reports.Areas.Display.Controllers
{
    public class DispatchController : Controller
    {
        //
        // GET: /Display/Dispatch/

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Dispatches/Recalls the given resource for the given operation. 
        /// </summary>
        /// <param name="operation">The id of the operation.</param>
        /// <param name="resource">The id of the emkresource which has to be recalled/dispatched</param>
        /// <returns>A JSON Result (Recalled/Dispatched) or a HTTP 500 error.</returns>
        public ActionResult Dispatch(int operation, string resource)
        {
            var disposingService = ServiceFactory.GetCallbackServiceWrapper<IDispositioningService>(new DispositioningServiceCallback());
            JsonResult result = new JsonResult();

            try
            {
                if (disposingService.Instance.GetDispatchedResources(operation).Any(x => x.Equals(resource)))
                {
                    disposingService.Instance.Recall(operation, resource);
                    result.Data = "Recalled";
                }
                else
                {
                    disposingService.Instance.Dispatch(operation, resource);
                    result.Data = "Dispatched";
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
            disposingService.Dispose();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return result;
        }

        /// <summary>
        /// Returns a JSON List containing all emkresources. 
        /// This resources get marked as dispatched/alarmed if allready done (by Windows Tool or alarmsource)
        /// </summary>
        /// <param name="id">The if of the operation which is requested.</param>
        /// <returns>Returns a JSON List containing all emkresources (list of <see cref="ResourceItem"/>s)</returns>
        public ActionResult Resources(int id)
        {
            Operation operation;
            try
            {
                using (var service = ServiceFactory.GetCallbackServiceWrapper<IOperationService>(new OperationServiceCallback()))
                {
                    operation = service.Instance.GetOperationById(id);
                    if (operation == null)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
            List<ResourceItem> resources = new List<ResourceItem>();
            var disposingService = ServiceFactory.GetCallbackServiceWrapper<IDispositioningService>(new DispositioningServiceCallback());
            using (var service = ServiceFactory.GetServiceWrapper<IEmkService>())
            {
                List<EmkResource> emkResources = service.Instance.GetAllResources().Where(x => x.IsActive).ToList();
                IList<OperationResource> alarmedResources = service.Instance.GetFilteredResources(operation.Resources);
                foreach (EmkResource emkResource in emkResources)
                {
                    ResourceItem resourceItem = new ResourceItem(emkResource);
                    resourceItem.CanGetDispatched = !alarmedResources.Any(x => emkResource.IsMatch(x));

                    resources.Add(resourceItem);
                }
            }

            string[] dispatchedResources = disposingService.Instance.GetDispatchedResources(operation.Id);
            foreach (ResourceItem item in resources)
            {
                if (dispatchedResources.Contains(item.EmkResourceItem.Id))
                {
                    item.Dispatched = true;
                }
            }
            disposingService.Dispose();
            JsonResult result = new JsonResult();
            result.Data = resources;
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return result;
        }

    }
}
