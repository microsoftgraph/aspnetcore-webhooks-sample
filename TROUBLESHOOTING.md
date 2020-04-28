# Troubleshooting

This document covers some of the common issues you may encounter when running this sample.

## You get a 403 Forbidden response when you attempt to create a subscription

Make sure that your app registration includes the required permission for Microsoft Graph (as described in the [Register the app](README.md#register-the-app) section). This permission must be set before you try to create a subscription. Otherwise you'll get an error. Then, make sure a tenant administrator has granted consent to the application.

## You do not receive notifications

If you're using ngrok, you can use the web interface [http://127.0.0.1:4040](http://127.0.0.1:4040) to see whether the notification is being received. If you're not using ngrok, monitor the network traffic using the tools your hosting service provides, or try using ngrok.

If Microsoft Graph is not sending notifications, please open a [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph) issue tagged `MicrosoftGraph`. Include the subscription ID and the time it was created.

Known issue: Occasionally the notification is received, and the retrieved message is sent to NotificationService, but the SignalR client in this sample does not update. When this happens, it's usually the first notification after the subscription is created.

## You get a "Subscription validation request timed out" response

This indicates that Microsoft Graph did not receive a validation response within the expected time frame (about 10 seconds).

- Make sure that you are not paused in the debugger when the validation request is received.
- If you're using ngrok, make sure that you used your project's HTTP port for the tunnel (not HTTPS).

## You get errors while installing packages

Make sure the local path where you placed the solution is not too long/deep. Moving the solution closer to the root drive resolves this issue.

## The app opens to a *Server Error in '/' Application.

**The resource cannot be found.* browser page.**

Make sure that a CSHTML view file isn't the active tab when you run the app from Visual Studio.

## Hosting the sample without a tunnel

Microsoft Graph (or any other webhook provider) needs a notification URL that it can reach to deliver notifications. The sample uses localhost as the development server.

Localhost just means this host. If any webhook provider would deliver a notification to localhost, it would be delivering it to itself. Not very useful.

Microsoft Graph can't deliver notifications to localhost. For this reason, we need a tunnel that can forward requests from a URL on the Internet to our localhost.

There are some alternatives that you can consider to try this sample without a tunnel.

### Host the sample on a cloud service

You can host the sample using a cloud service such as Microsoft Azure. Cloud services allow you to expose the notification URL to the Internet. Microsoft Graph can deliver notifications to the URL in the cloud.

Note that in some cases, you'll be able to deploy the sample to a website hosted in the cloud. In other cases, you'll need to set up a virtual machine and install a development environment with the prerequisites listed in the [ReadMe](./README.md#prerequisites).

See your cloud provider's documentation for details about how to host a web application or virtual machine using the cloud service.