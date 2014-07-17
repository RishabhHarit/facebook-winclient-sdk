﻿// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

#if WP8
using Windows.System;
#endif

namespace Facebook.Client
{
    /// <summary>
    /// This class mimics the functionality provided by WebAuthenticationOptions available in Win8.
    /// </summary>
    internal enum WebAuthenticationOptions
    {
        None,
        SilentMode
    }

    /// <summary>
    /// This class mimics the functionality provided by WebAuthenticationStatus available in Win8.
    /// </summary>
    internal enum WebAuthenticationStatus
    {
        Success = 0,

        UserCancel = 1,

        ErrorHttp = 2
    }

    /// <summary>
    /// This class mimics the functionality provided by WebAuthenticationResult available in Win8.
    /// </summary>
    internal sealed class WebAuthenticationResult
    {
        public string ResponseData { get; private set; }

        public WebAuthenticationStatus ResponseStatus { get; private set; }

        public uint ResponseErrorDetail { get; private set; }

        public WebAuthenticationResult(string data, WebAuthenticationStatus status, uint error)
        {
            ResponseData = data;
            ResponseStatus = status;
            ResponseErrorDetail = error;
        }
    }

    /// <summary>
    /// This class mimics the functionality provided by WebAuthenticationBroker available in Win8.
    /// </summary>
    internal sealed class WebAuthenticationBroker
    {
        private static string responseData = "";
        private static uint responseErrorDetail = 0;
        private static WebAuthenticationStatus responseStatus = WebAuthenticationStatus.UserCancel;
        private static AutoResetEvent authenticateFinishedEvent = new AutoResetEvent(false);

        static public bool AuthenticationInProgress { get; private set; }
        static public Uri StartUri { get; private set; }
        static public Uri EndUri { get; private set; }
        static public bool SystemTrayIsVisible { get; set; }
        
        /// <summary>
        /// Mimics the WebAuthenticationBroker's AuthenticateAsync method.
        /// </summary>
        public static Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri startUri, Uri endUri)
        {
            if (options != WebAuthenticationOptions.None)
            {
                throw new NotImplementedException("This method does not support authentication options other than 'None'.");
            }

            bool delegateToUI = false;
            PhoneApplicationFrame rootFrame = null;

            // Trying to do this in current thread. If not possible, try do to on UI thread.
            try
            {
                rootFrame = Application.Current.RootVisual as PhoneApplicationFrame;

                if (rootFrame == null)
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                delegateToUI = true;
            }

            WebAuthenticationBroker.StartUri = startUri;
            WebAuthenticationBroker.EndUri = endUri;
            WebAuthenticationBroker.AuthenticationInProgress = true;

            // Navigate to the login page.
            if (!delegateToUI)
            {

                rootFrame.Navigate(new Uri("/Facebook.Client;component/loginpage.xaml?systemtray=" + SystemTrayIsVisible.ToString(), UriKind.Relative));
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    rootFrame = Application.Current.RootVisual as PhoneApplicationFrame;

                    if (rootFrame == null)
                    {
                        return;
                    }

                    rootFrame.Navigate(new Uri("/Facebook.Client;component/loginpage.xaml?systemtray=" + SystemTrayIsVisible.ToString(), UriKind.Relative));
                });
            }

            Task<WebAuthenticationResult> task = Task<WebAuthenticationResult>.Factory.StartNew(() =>
            {
                authenticateFinishedEvent.WaitOne();
                return new WebAuthenticationResult(responseData, responseStatus, responseErrorDetail);
            });

            return task;
        }

        public static void OnAuthenticationFinished(string data, WebAuthenticationStatus status, uint error)
        {
            WebAuthenticationBroker.responseData = data;
            WebAuthenticationBroker.responseStatus = status;
            WebAuthenticationBroker.responseErrorDetail = error;

            WebAuthenticationBroker.AuthenticationInProgress = false;

            // Signal the waiting task that the authentication operation has finished.
            authenticateFinishedEvent.Set();
        }
    }
}
