---
page_type: sample
description: "此 ASP.NET Core Web 应用程序示例显示了如何使用委托权限订阅 Webhook。"
products:
- office-outlook
- office-365
- ms-graph
languages:
- csharp
extensions:
  contentType: samples
  technologies:
  - Microsoft Graph 
  - Microsoft identity platform
  services:
  - Outlook
  - Office 365
  - Microsoft identity platform
  createdDate: 3/3/2017 8:55:02 AM
---
# 适用于 ASP.NET Core 的 Microsoft Graph Webhook 示例

通过订阅 [Microsoft Graph Webhook](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)，即可在用户数据更改时获得通知，因此无需轮询更改。

此 ASP.NET Core Web 应用程序示例显示了如何使用委托的权限订阅 Webhook。此示例使用 OpenID Connect 进行登录/注销，对开发者使用 Microsoft 标识平台，通过[适用于 .NET 的 Microsoft 身份验证库](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) (MSAL.NET) 使用[身份验证代码流](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)获取访问令牌，并通过[适用于 .NET 的 Microsoft Graph 客户端库](https://github.com/microsoftgraph/msgraph-sdk-dotnet) (SDK) 代表已成功登录到 Web 应用的用户调用 Microsoft Graph。这些复杂性已封装到 `Microsoft.Identity.Web` 可重用库项目中。 

>请参阅 Microsoft Graph 中每个受支持资源所允许的[委托的权限](https://docs.microsoft.com/en-us/graph/api/subscription-post-subscriptions?view=graph-rest-1.0)列表。

此示例应用重定向到 Azure AD *adminconsent* 终结点，所以租户管理员可以直接向应用授予委托的权限。管理员同意后，租户中的用户可创建订阅并查收通知。 

下面是应用程序可通过 Webhook 订阅执行的常见任务：

- 获得订阅用户资源的许可，然后获取访问令牌。
- 使用访问令牌为资源[创建订阅](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/subscription_post_subscriptions)。
- 回发验证令牌以确认通知 URL。
- 收听来自 Microsoft Graph 的通知并使用 202 状态代码进行响应。
- 请求与使用通知中的数据更改的资源相关的更多信息。

## 使用 Microsoft Graph Webhook 示例

下面的屏幕截图显示了该应用的起始页。 
  
![适用于 ASP.NET Core 的 Microsoft Graph Webhook 示例屏幕截图](readme-images/Page1.PNG)

应用为已登录的用户创建订阅之后，当用户订阅的资源中发生事件时，Microsoft Graph 将向注册的终结点发送通知。应用随后会对事件作出回应。

此示例应用为`已创建的`更改订阅了 `users/{user-id}/mailFolders('Inbox')/messages` 资源。当被告知已订阅的用户收到邮件后，该应用将使用有关该邮件的信息来更新页面。页面中仅显示属于已登录用户的邮件。

### 先决条件

若要使用适用于 ASP.NET Core 的 Microsoft Graph Webhook 示例，需满足以下条件：

- 在开发计算机上安装 Visual Studio 2017。 
- 安装了 .NET Core 2.1 或更高版本（例如针对 Windows）。可按照 [.NET 和 C# - 10 分钟入门](https://www.microsoft.com/net/core)中的说明操作。除在 Windows 上进行开发之外，还可在 [Linux](https://www.microsoft.com/net/core#linuxredhat)、[Mac](https://www.microsoft.com/net/core#macos) 或 [Docker](https://www.microsoft.com/net/core#dockercmd) 上进行开发。
- 具有一个[工作、学校或个人帐户](https://dev.office.com/devprogram)。需要使用租户管理员帐户授予应用程序权限。 
- [在 Azure 门户中注册](#register-the-app)了应用程序的 ID 和密钥。 
- 具有用于接收和发送 HTTP 请求的公共 HTTPS 终结点。你可以将此终结点托管到 Microsoft Azure 或其他服务上，或者你可以在测试时[使用 ngrok](#ngrok) 或类似工具。

### 创建应用

#### 选择要在其中创建应用的租户

1. 使用工作/学校帐户登录 [Azure 门户](https://portal.azure.com)。
1. 如果你的帐户存在于多个 Azure AD 租户中：
   1. 请从页面右上角的菜单中选择你的个人资料，然后选择“**切换目录**”。
   1. 将会话更改为要在其中创建应用程序的 Azure AD 租户。

#### 注册应用

1. 导航到 [Azure 门户 >“应用注册”](https://go.microsoft.com/fwlink/?linkid=2083908)以注册应用。
1. 选择“**新注册**”。
1. 出现“**注册应用程序**”页面后，输入应用的注册信息：
   1. 在“**名称**”部分输入一个有意义的名称，该名称将显示给应用用户。例如：`MyWebApp`
   1. 在“**支持的帐户类型**”部分，选择“**任何组织目录中的帐户和个人 Microsoft 帐户(例如 Skype、Xbox、Outlook.com)**”。
      > 如果有多个重定向 URI，则需要稍后在成功创建应用后从“**身份验证**”选项卡中添加这些 URI。
1. 选择“**注册**”以创建应用。
1. 在应用的“**概述**”页面上，查找“**应用程序(客户端) ID**”值，记下它供稍后使用。你将需要此值来为此项目配置 Visual Studio 配置文件。
1. 在应用的页面列表中，选择“**身份验证**”。
   1. 在“**重定向 URI**”部分，选择组合框中的“**Web**”，然后输入以下重定向 URI：
       - `https://localhost:44334/signin-oidc`
       - `https://localhost:44334/Account/GrantPermissions`
1. 选择“**保存**”。
1. 在“**证书和密钥**”页面的“**客户端密码**”部分中，选择“**新建客户端密码**”。
   1. 键入密钥说明（例如`应用实例`）。
   1. 选择密钥持续时间：“**1 年内**”、“**2 年内**”或“**永不过期**”。
   1. 单击“**添加**”按钮时，将显示密钥值。复制密钥值并将其存储在安全的位置。

      稍后将需要此密钥来配置 Visual Studio 中的项目。此密钥值将不再显示，也不可用其他任何方式进行检索，因此请在 Azure 门户中看到此值时立即进行记录。

1. 在应用的页面列表中，选择“**API 权限**”。
   1. 单击“**添加权限**”按钮，然后确保选中“**Microsoft API**”选项卡。
   1. 在“**常用 Microsoft API**”部分，选择“**Microsoft Graph**”。
   1. 在“**应用程序权限**”部分，确保已勾选“**Mail.Read**”权限。必要时请使用搜索框。
    > 此外，在“**委托的权限**”部分，为 Azure Active Directory 选中“User.Read”委托权限，以便用户可以登录到应用以启动订阅过程。
   1. 选择“**添加权限**”按钮。
   
<a name="ngrok"></a>
### 设置 ngrok 代理（可选） 
你必须公开一个公共的 HTTPS 终结点才能创建订阅并接收来自 Microsoft Graph 的通知。测试时，你可以使用 ngrok 临时允许消息从 Microsoft Graph 经隧道传输至计算机上的 *localhost* 端口。 

你可以使用 ngrok Web 界面 (http://127.0.0.1:4040) 检查流经隧道的 HTTP 流量。若要了解与使用 ngrok 相关的详细信息，请参阅 [ngrok 网站](https://ngrok.com/)。  

1. 在解决方案资源管理器中，右键单击“**GraphWebhooks-Core**”项目，然后选择“**属性**”。 

1. 在“**调试**”选项卡上，复制“**应用 URL**”的端口号。 

	![“属性”窗口中的 URL 端口号](readme-images/PortNumber.png)

1. [下载 Windows 版 ngrok](https://ngrok.com/download)。  

1. 解压包并运行 ngrok.exe。

1. 将以下命令中的两个 *{port-number}* 占位符值替换为所复制的端口号，然后在 ngrok 控制台中运行以下命令。

   `ngrok http {port-number} -host-header=localhost:{port-number}`

	![在 ngrok 控制台中运行的示例命令](readme-images/ngrok1.PNG)

1. 复制控制台中显示的 HTTPS URL。你将使用它来配置示例中的通知 URL。

	![ngrok 控制台中的转发 HTTPS URL](readme-images/ngrok2.PNG)

测试时，请保持控制台处于打开状态。如果关闭，则隧道也会关闭，并且你需要生成新的 URL 并更新示例。

>有关使用隧道的详细信息，请参阅[不使用隧道托管](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Hosting-the-sample-without-a-tunnel)和[为什么必须使用隧道？](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Why-do-I-have-to-use-a-tunnel)。


## 配置并运行示例

1. 按照这些[说明](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-2.2)将 ASP.NET Core SignalR Javascript 客户端包安装到应用中。
1. 公开公共 HTTPS 通知终结点。它可以在 Microsoft Azure 等服务上运行，或者你可以通过[使用 ngrok](#ngrok) 或类似工具创建代理 Web 服务器。

1. 在 Visual Studio 2017 中打开 **GraphWebhooks-Core.sln** 示例文件。

1. 在解决方案资源管理器中，打开项目根目录下的 **appsettings.json** 文件。  
 
   - 对于 **NotificationUrl** 密钥，请将 *ENTER_YOUR_URL* 替换为 HTTPS URL。保留 */notification/listen* 部分。 

   如果使用的是 ngrok，请使用复制的 HTTPS URL。值与以下类似：

   `"NotificationUrl": "https://2885f9c5.ngrok.io/notification/listen",`

   这是 URL 终结点，将通过上述所设置的代理服务器（此示例中为 ngrok）从 Graph 接收订阅验证回调和通知事件。

1. 在解决方案资源管理器中，右键单击项目名称，然后选择“**管理用户机密**”。此应用使用[密码管理器](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2)配置来存储敏感的应用数据（ClientId 和 ClientSecret）。
    
    - 在打开的 **secret.json** 窗口中，粘贴以下代码。
        
        `"AzureAd": {
        "ClientId": "ENTER_YOUR_APP_ID",
        "ClientSecret": "ENTER_YOUR_SECRET"
  }`

    - 对于 **ClientId** 键，请将 *ENTER_YOUR_APP_ID* 替换为已注册的 Azure 应用程序的应用程序 ID。  
    - 对于 **ClientSecret** 键，请将 *ENTER_YOUR_SECRET* 替换为已注册的 Azure 应用程序的密钥。请注意，在生产应用中，应始终将证书用作应用程序密码，但对于此示例，我们将使用简单的共享密码。

1. 确保 ngrok 控制台仍在运行，然后按 F5 在调试模式下生成和运行解决方案。 

   >如果在安装包时出现任何错误，请确保你放置该解决方案的本地路径并未太长/太深。若要解决此问题，可以将解决方案移到更接近根驱动器的位置。

### 使用应用创建订阅
 

1. 选择右上角的“**登录**”，然后使用工作或学校帐户登录。

1. 同意“**查看你的基本个人资料**”和“**以你的身份登录**”权限。 

1. 在示例主页上，选择“**获得管理员同意**”。系统会将你重定向到 *adminconsent* 页面。

1. 以租户管理员身份登录，并同意“**读取所有邮箱中的邮件**”和“**登录并读取用户个人资料**”权限。系统会将你重定向回示例的主页。 

   此时，你租户中的任何用户都可以登录并创建订阅。如果先不授予管理员权限，则会收到*未经授权*错误。你将需要在新的浏览器会话中打开此示例，因为此示例会缓存初始令牌。
    
1. 选择“**创建订阅**”。“**订阅**”页面中将会加载与订阅相关的信息。

   >此示例将订阅过期时间设为 15 分钟，以供测试使用。

	![显示新订阅的属性的应用页面](readme-images/Page2.PNG)
	
1. 选择“**监视通知**”按钮。 

1. 向你的用户帐户发送一封电子邮件。“**通知**”页面将显示邮件属性。页面更新可能需要几秒钟。

1. （可选）选择“**删除订阅**”按钮。 


## 示例的主要组件 
以下文件包含与连接到 Microsoft Graph、创建订阅和处理通知相关的代码。

- [`appsettings.json`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/appsettings.json) 包含用于身份验证、授权和终结点 URL 的值。 
- secrets.json 包含用于身份验证和授权的 ClientId 和 ClientSecret。若要检查是否已为项目配置了这些设置，请从 .csproj 文件所在的目录运行以下命令：
`dotnet user-secrets list`
- [`Startup.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Startup.cs) 配置应用及其使用的服务，包括身份验证。

### 控制器  
- [`AccountController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/AccountController.cs) 处理管理员许可。  
- [`NotificationController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/NotificationController.cs) 接收通知。  
- [`SubscriptionContoller.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/SubscriptionController.cs) 创建和删除订阅。

### 模型
- [`Notification.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/Notification.cs) 表示更改通知。
- [`MessageViewModel.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/MessageViewModel.cs) 定义表示“通知”视图中显示的数据的 **MessageViewModel**。

### 帮助程序
- [`GraphServiceClientFactory.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/GraphServiceClientFactory.cs) 启动用于与 Microsoft Graph 进行交互的 SDK 客户端。
- [`SubscriptionStore.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/SubscriptionStore.cs) 表示已存储订阅信息的访问层。此示例会将信息临时存储于 HttpRuntime.Cache 中。通常情况下，生产应用将使用一些持久性存储方法。

### Microsoft.Identity.Web
- 帮助程序库包含一组可重用的类，这些类可用于帮助执行以下操作：
    
    - 借助 OpenID Connect 中间件和 MSAL.NET 在 Microsoft 标识平台 v2.0 (AAD v2.0) 上使用任何工作、学校或 Microsoft 个人帐户对用户进行身份验证和登录。 
    - 处理注销并从 MSAL.NET 缓存中删除帐户。
    - 代表已登录的用户获取令牌。
    - 从应用程序的 `Startup.cs` 文件中通过只调用几种方法来引导 Web 资源。



## 疑难解答 

| 问题 | 解决方案 |
|:------|:------|
| 你在尝试创建订阅时收到 403 禁用响应。| 请确保你的应用注册包含对 Microsoft Graph 的 **Mail.Read** 应用程序权限（如[注册应用](#register-the-app)部分中所述），并且租户管理员已授权该应用。|  
| 你不能收到通知。| 如果你使用了 ngrok，则可以使用 Web 界面 (http://127.0.0.1:4040) 查看是否收到了通知。如果未使用 ngrok，请使用托管服务提供的工具来监视网络流量，或者尝试使用 ngrok。<br />如果 Microsoft Graph 没有发送通知，请开立一个带有 *[MicrosoftGraph]* 标记的 [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph) 问题。请提供订阅 ID、创建时间以及响应中的请求 ID（如果有）。<br /><br />已知问题：偶尔会收到通知，并且已将检索到的邮件发送到 NotificationService，但此示例中的 SignalR 客户端不更新。发生这种情况时，这通常是在创建订阅后的第一个通知。|  
| 你收到*订阅验证请求超时*响应。| 这表示 Microsoft Graph 未在 10 秒内收到验证响应。<br /><br />如果你使用了 ngrok，请确保你的终结点可访问，并且你为隧道指定了项目的 HTTP 端口（而非 HTTPS）。|  
| 在安装包时出错。| 请确保你放置该解决方案的本地路径并未太长/太深。若要解决此问题，可以将解决方案移到更接近根驱动器的位置。|
| 你收到与 Microsoft.AspNetCore.SignalR.Server 相关的生成错误。| 在包管理器控制台中键入以下命令：'Install-Package Microsoft.AspNetCore.SignalR.Server -Version 0.2.0-rtm-22752 -Source https://dotnet.myget.org/F/aspnetcore-master/api/v3/index.json' |
| 应用打开一个*“/”应用程序中的服务器错误。找不到资源。*浏览器页面。| 在 Visual Studio 中运行应用时，请确保使用的是 CSHTML 视图文件而不是活动选项卡。|

## 参与

如果想要参与本示例，请参阅 [CONTRIBUTING.MD](/CONTRIBUTING.md)。

此项目已采用 [Microsoft 开放源代码行为准则](https://opensource.microsoft.com/codeofconduct/)。有关详细信息，请参阅[行为准则 FAQ](https://opensource.microsoft.com/codeofconduct/faq/)。如有其他任何问题或意见，也可联系 [opencode@microsoft.com](mailto:opencode@microsoft.com)。

## 问题和意见

我们乐意倾听你有关适用于 ASP.NET Core 的 Microsoft Graph Webhook 示例的反馈。你可以在该存储库中的[问题](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/issues)部分将问题和建议发送给我们。

与 Microsoft Graph 相关的一般问题应发布到 [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph)。请确保你的问题或意见标记有 *[MicrosoftGraph]*。

可在 [UserVoice](https://officespdev.uservoice.com/)上提供有关 Microsoft Graph 的更改意见。

## 其他资源

- [适用于 ASP.NET 4.6 的 Microsoft Graph Webhook 示例](https://github.com/microsoftgraph/aspnet-webhooks-rest-sample)（委托的权限）
- [适用于 Node.js 的 Microsoft Graph Webhook 示例](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)（委托的权限）
- [在 Microsoft Graph 中使用 Webhook](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
- [订阅资源](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
- [Microsoft Graph 文档](https://developer.microsoft.com/graph)

## 版权信息
版权所有 (c) 2019 Microsoft。保留所有权利。
