using System;
using System.Collections.Generic;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
namespace WebApi
{
    public class Notifications
    {
        public async static void NotificationSend(string label, string description,string reciever)
        {
           
            // This registration token comes from the client FCM SDKs.
            var registrationToken = reciever;

            // See documentation on defining a message payload.
            var message = new Message()
            {
                Data = new Dictionary<string, string>()
    {
        { "myData", "1337" },
    },
                Token = registrationToken,
               // Topic = "all",
                Notification = new Notification()
                {
                    Title = label,
                    Body = description
                }
            };

            // Send a message to the device corresponding to the provided
            // registration token.
            try
            {
                string result = FirebaseMessaging.DefaultInstance.SendAsync(message).Result;
            }
            catch
            {

            }
        }
    }
}
