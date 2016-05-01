using BackgroundTasks.Helpers;
using System;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;

namespace EBTL_Control
{
    public sealed class ContentProvider : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        private AppServiceConnection connection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //Take a service deferral so the service isn't terminated
            serviceDeferral = taskInstance.GetDeferral();

            taskInstance.Canceled += OnTaskCanceled;

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            connection = details.AppServiceConnection;

            //Listen for incoming app service requests
            connection.RequestReceived += OnRequestReceived;
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (serviceDeferral != null)
            {
                //Complete the service deferral
                serviceDeferral.Complete();
                serviceDeferral = null;
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //Get a deferral so we can use an awaitable API to respond to the message
            var messageDeferral = args.GetDeferral();

            try
            {
                var input = args.Request.Message;
                string Surname = (string)input["Surname"];
                string Name = (string)input["Name"];
                string Address = (string)input["Address"];
                string BloodType = (string)input["BloodType"];
                Geoposition GeoLocation = (Geoposition)input["GeoLocation"];
                Geopoint GeoPoint = (Geopoint)input["GeoPoint"];
                string EmergencyNumber = (string)input["EmergencyNumber"];

                ToastNotificationManager.History.Clear();
                ToastHelper.PopToast("Data received!!!", "" + GeoPoint.Position.Latitude.ToString() + GeoPoint.Position.Longitude.ToString());

                //Create the response
                var result = new ValueSet();
                result.Add("result", "Data received");

                //Send the response
                await args.Request.SendResponseAsync(result);
            }
            finally
            {
                //Complete the message deferral so the platform knows we're done responding
                messageDeferral.Complete();
            }
        }
    }
}