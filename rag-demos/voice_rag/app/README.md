## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- An Calling-enabled telephone number. [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=platform-azp).
- Azure Dev Tunnels CLI or another method to tunnel back to your local computer. For details, see  [Enable dev tunnel](https://docs.tunnels.api.visualstudio.com/cli)
- An Azure OpenAI Resource and Deployed Realtime Model. See [instructions](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/how-to/create-resource?pivots=web-portal).


## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

##### 1. Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 5165
devtunnel host
```
##### 2. Add the required API Keys and endpoints
Open the Properties\launchSettings.json file to configure the following settings:

    - `VS_TUNNEL_URL`: your dev tunnel endpoint
    - `applicationUrl`: the localhost port that your local app is running on

Open the `appsettings.json` or add an `appsettings.Development.json` file to configure the following settings:

    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `AzureOpenAIServiceKey`: Open AI's Service Key
    - `AzureOpenAIServiceEndpoint`: Open AI's Service Endpoint
    - `AzureOpenAIDeploymentModelName`: Open AI's Model name
    - `SystemPrompt`: Optionally override the AI default system prompt


## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the sample application
3. Register an EventGrid Webhook for the IncomingCall Event that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).
