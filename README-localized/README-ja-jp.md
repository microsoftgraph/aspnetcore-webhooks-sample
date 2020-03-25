---
page_type: sample
description: "このサンプル ASP.NET Core Web アプリケーションでは、委任されたアクセス許可を使用して Webhook をサブスクライブする方法を示します。"
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
# ASP.NET 用 Microsoft Graph Webhook のサンプル

[Microsoft Graph Webhook](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) をサブスクライブすると、ユーザーのデータが変更された場合に通知を受け取ることができ、変更内容についてポーリングを行う必要がなくなります。

このサンプルの ASP.NET Core Web アプリケーションでは、委任されたアクセス許可を使用して Webhook をサブスクライブする方法を示します。開発者向け Microsoft ID プラットフォームを使用するログイン/ログアウトに OpenID Connect を使用し、[認証コード フロー](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)を使用してアクセス トークンを取得するために [Microsoft Authentication Library for .NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) (MSAL.NET) を使用し、Web アプリに正常にログインしたユーザーに代わって Microsoft Graphを呼び出すために [Microsoft Graph Client Library for .NET](https://github.com/microsoftgraph/msgraph-sdk-dotnet) (SDK) を使用します。これらの複雑さは、`Microsoft.Identity.Web` 再利用可能ライブラリ プロジェクトにカプセル化されています。 

>Microsoft Graph でサポートされている各リソースに許可されている[委任されたアクセス許可](https://docs.microsoft.com/en-us/graph/api/subscription-post-subscriptions?view=graph-rest-1.0)の一覧を参照してください。

サンプル アプリは Azure AD *adminconsent* エンドポイントにリダイレクトするため、テナント管理者は委任されたアクセス許可をアプリに直接付与できます。管理者が同意すると、テナントのユーザーはサブスクリプションを作成し、通知を監視できます。 

アプリケーションが Webhook のサブスクリプションを使用して実行する一般的なタスクを次に示します。

- ユーザーのリソースをサブスクライブするための同意を取得し、アクセス トークンを取得する。
- アクセス トークンを使用して、リソースへの[サブスクリプションを作成](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/subscription_post_subscriptions)する。
- 検証トークンを送り返して通知 URL を確認する。
- Microsoft Graph からの通知をリッスンし、状態コード 202 で応答する。
- 通知内のデータを使用して、変更されたリソースの詳細情報を要求する。

## Microsoft Graph Webhook のサンプルを使用する

以下のスクリーンショットは、アプリの開始ページを示しています。 
  
![ASP.NET Core 用の Microsoft Graph Webhook サンプルのスクリーンショット](readme-images/Page1.PNG)

ログインしているユーザーのためにアプリがサブスクリプションを作成した後は、ユーザーのサブスクライブしたリソースでイベントが発生すると、Microsoft Graph は登録済みエンドポイントに通知を送信します。これに対して、アプリがイベントに反応します。

このサンプル アプリでは、`created` 変更について、`users/{user-id}/mailFolders('Inbox')/messages` リソースをサブスクライブします。サブスクライブしているユーザーにメール メッセージを受信したことが通知されると、アプリはそのメッセージに関する情報でページを更新します。このページには、ログインしているユーザーに属するメッセージのみが表示されます。

### 前提条件

ASP.NET Core 用 Microsoft Graph Webhook サンプルを使用するには、以下が必要です。

- 開発用コンピューターにインストールされている Visual Studio 2017。 
- .NET Core 2.1 以降 (たとえば、Windows 版) がインストールされてること。「[.NET and C# - 10 分で使用を開始する](https://www.microsoft.com/net/core)」の手順に従います。Windows での開発に加えて、[Linux](https://www.microsoft.com/net/core#linuxredhat)、[Mac](https://www.microsoft.com/net/core#macos)、[Docker](https://www.microsoft.com/net/core#dockercmd) でも開発できます。
- [職場、学校または個人のアカウント](https://dev.office.com/devprogram)。アプリケーションのアクセス許可を付与するには、テナント管理者アカウントが必要です。 
- [Azure ポータルに登録](#register-the-app)するアプリケーションのアプリケーション ID とキー。 
- HTTP 要求を送受信するためのパブリック HTTPS エンドポイント。Microsoft Azure または別のサービスでエンドポイントをホストすることも、テスト中は [ngrok](#ngrok) または同様のツールを使用することもできます。

### アプリを作成する

#### アプリを作成するテナントを選択する

1. 職場または学校のアカウントを使用して、[Azure ポータル](https://portal.azure.com)にサインインします。
1. 複数の Azure AD テナントにアカウントが存在する場合:
   1. ページの右上隅にあるメニューからプロファイルを選択し、[**ディレクトリの切り替え**] を選択します。
   1. アプリケーションを作成する Azure AD テナントにセッションを変更します。

#### アプリを登録する

1. [[Azure ポータル]、[アプリの登録]](https://go.microsoft.com/fwlink/?linkid=2083908) の順に移動してアプリを登録します。
1. [**新規登録**] を選択します。
1. [**アプリケーションの登録ページ**] が表示されたら、以下のアプリの登録情報を入力します。
   1. [**名前**] セクションに、アプリのユーザーに表示されるわかりやすい名前を入力します。次に例を示します。`MyWebApp`
   1. [**サポートされているアカウントの種類**] セクションで、[**組織ディレクトリ内のアカウントと個人の Microsoft アカウント (例: Skype、Xbox、Outlook.com)**] を選択します。
      > リダイレクト URI が複数ある場合は、アプリが正常に作成された後で、[**認証**] タブからこれらを追加する必要があります。
1. [**登録**] を選択して、アプリを作成します。
1. アプリの [**概要**] ページで、[**Application (client) ID**] (アプリケーション (クライアント) ID) の値を確認し、後で使用するために記録します。この値は、このプロジェクトで Visual Studio 構成ファイルを設定するのに必要になります。
1. アプリのページの一覧から [**認証**] を選択します。
   1. [**リダイレクト URI**] セクションで、コンボ ボックスの [**Web**] を選択し、次のリダイレクト URI を入力します。
       - `https://localhost:44334/signin-oidc`
       - `https://localhost:44334/Account/GrantPermissions`
1. [**保存**] を選択します。
1. [**証明書とシークレット**] ページの [**クライアント シークレット**] セクションで、[**新しいクライアント シークレット**]を選択します。
   1. キーの説明を入力します (例: `アプリ シークレット`)。
   1. [**1 年**]、[**2 年**]、または [**有効期限なし**] からキーの期間を選択します。
   1. [**追加**] ボタンをクリックすると、キー値が表示されます。キー値をコピーして安全な場所に保存します。

      Visual Studio でプロジェクトを構成するには、このキーが必要になります。このキー値は二度と表示されず、他の方法で取得することもできませんので、Azure ポータルで表示されたらすぐに記録してください。

1. アプリのページの一覧から [**API のアクセス許可**] を選択します。
   1. [**アクセス許可の追加**] ボタンをクリックして、[**Microsoft API**] タブが選択されていることを確認します。
   1. [**一般的に使用される Microsoft API**] セクションで、[**Microsoft Graph**] を選択します。
   1. [**アプリケーションのアクセス許可**] セクションで、**Mail.Read.** アクセス許可が選択されていることを確認します。必要に応じて検索ボックスを使用します。
    > また、[**委任されたアクセス許可**] セクションで、Azure Active Directory の User.Read 委任されたアクセス許可を確認して、ユーザーがアプリにログインしてサブスクリプション プロセスを開始できるようにします。
   1. [**アクセス許可の追加**] ボタンを選択します。
   
<a name="ngrok"></a>
### ngrok プロキシをセットアップする (省略可) 
サブスクリプションを作成し、Microsoft Graph から通知を受信するには、パブリック HTTPS エンドポイントを公開する必要があります。テスト中は、ngrok を使用して Microsoft Graph からのメッセージをコンピューター上の *localhost* ポートにトンネリングすることを一時的に許可できます。 

ngrok Web インターフェイス (http://127.0.0.1:4040) を使用して、トンネルを通過する HTTP トラフィックを検査できます。ngrok の使用方法の詳細については、「[ngrok の Web サイト](https://ngrok.com/)」を参照してください。  

1. ソリューション エクスプローラーで、**GraphWebhooks-Core** プロジェクトを右クリックして、[**プロパティ**] を選択します。 

1. [**デバッグ**] タブで、**App URL** のポート番号をコピーします。 

	![[プロパティ] ウィンドウ内の URL ポート番号](readme-images/PortNumber.png)

1. Windows 用の [ngrok をダウンロード](https://ngrok.com/download)します。  

1. パッケージを展開し、ngrok.exe を実行します。

1. 次のコマンドの 2 つの *{port-number}* プレースホルダー値をコピーしたポート番号に置き換え、ngrok コンソールでコマンドを実行します。

   `ngrok http {port-number} -host-header=localhost:{port-number}`

	![ngrok コンソールで実行するコマンドの例](readme-images/ngrok1.PNG)

1. コンソールに表示される HTTPS URL をコピーします。このサンプルでは、これを使用して通知 URL を設定します。

	![ngrok コンソールに表示される転送用 HTTPS URL](readme-images/ngrok2.PNG)

テスト中はコンソールを開いたままにします。コンソールを閉じるとトンネルも閉じられるため、新しい URL を生成してサンプルを更新する必要があります。

>トンネルの使用に関する詳細については、「[Hosting without a tunnel (トンネルを使用しないでホストする)](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Hosting-the-sample-without-a-tunnel)」 および 「[Why do I have to use a tunnel? (トンネルを使用する理由)](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Why-do-I-have-to-use-a-tunnel)」 を参照してください。


## サンプルを構成して実行する

1. 以下の[手順](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-2.2)に従って、ASP.NET Core SignalR Javascript クライアント パッケージをアプリにインストールします。
1. 通知エンドポイントのパブリック HTTPS を公開します。Microsoft Azure などのサービスで HTTPS を実行することも、[ngrok](#ngrok) または同様のツールを使用してプロキシ Web サーバーを作成することもできます。

1. Visual Studio 2017 で **GraphWebhooks-Core.sln** のサンプルを開きます。

1. ソリューション エクスプローラーで、プロジェクトのルート ディレクトリにある **appsettings.json** ファイルを開きます。  
 
   - **NotificationUrl** キーは、 *ENTER_YOUR_URL* を HTTPS URL で置き換えます。*/notification/listen* の部分は残します。 

   ngrok を使用している場合は、コピーした HTTPS URL を使用します。値は次のようになります。

   `"NotificationUrl": "https://2885f9c5.ngrok.io/notification/listen",`

   これは、上記のプロキシ サーバー設定 (このサンプルでは ngrok) を介して、Graph からサブスクリプション検証コールバックと通知イベントを受け取る URL エンドポイントです。

1. ソリューション エクスプローラー内で、プロジェクト名を右クリックし、[**ユーザー シークレットの管理**] を選択します。このアプリは、機密アプリのデータ (ClientId および ClientSecret) の保存に [Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2) 構成を使用します。
    
    - 開く **secret.json** ウィンドウに以下のコードを貼り付けます。
        
        `"AzureAd": {
        "ClientId": "ENTER_YOUR_APP_ID",
        "ClientSecret": "ENTER_YOUR_SECRET"
  }`

    - **ClientId** キーは、*ENTER_YOUR_APP_ID* を登録済み Azure アプリケーションのアプリケーション ID で置き換えます。  
    - **ClientSecret** キーは、*ENTER_YOUR_SECRET* を登録済み Azure アプリケーションのキーで置き換えます。運用アプリではアプリケーション シークレットとして常に証明書を使用するべきですが、このサンプルでは、簡単な共有シークレット パスワードを使用している点にご留意ください。

1. ngrok コンソールがまだ実行中であることを確認し、F5 キーを押してデバッグ モードでソリューションをビルドして実行します。 

   >パッケージのインストール中にエラーが発生した場合は、ソリューションを保存したローカル パスが長すぎたり深すぎたりしていないかご確認ください。この問題は、ドライブのルート近くにソリューションを移動すると解決します。

### アプリを使用してサブスクリプションを作成する
 

1. 右上隅の [**ログイン**] を選択し、職場または学校のアカウントでログインします。

1. **基本プロフィールの表示**とアクセス許可**としてログインする**に同意します。 

1. サンプルのホーム ページで、[**管理者の同意を付与する**] を選択します。*adminconsent* ページにリダイレクトされます。

1. テナント管理者としてログインして、**すべてのメールボックスのメールを読み取る**および**ログインとユーザー プロフィールの読み取り**アクセス許可に同意します。サンプルのホーム ページにリダイレクトされます。 

   この時点で、テナント内のすべてのユーザーがログインしてサブスクリプションを作成できます。最初に管理者アクセス許可を付与しないと、*未認証*エラーが表示されます。このサンプルは初期トークンをキャッシュするため、新しいブラウザー セッションでサンプルを開く必要があります。
    
1. [**サブスクリプションの作成**] を選択します。[**サブスクリプション**] ページに、サブスクリプションに関する情報が表示されます。

   >このサンプルでは、テスト用にサブスクリプションの有効期限を 15 分に設定します。

	![新しいサブスクリプションのプロパティを表示するアプリのページ](readme-images/Page2.PNG)
	
1. [**Watch for notifications (通知を監視する)**] ボタンを選択します。 

1. ユーザー アカウントにメールを送信します。[**通知**] ページには、メッセージ プロパティが表示されます。ページの更新に数秒かかることがあります。

1. 必要に応じて、[**Delete subscription (サブスクリプションの削除)**] ボタンを選択します。 


## サンプルの主要なコンポーネント 
次のファイルには、Microsoft Graph への接続、サブスクリプションの作成、および通知の処理信に関連するコードが含まれています。

- [`appsettings.json`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/appsettings.json) 認証、承認、およびエンドポイント URL に使用される値が含まれます。 
- secrets.json 認証と承認に使用される ClientId と ClientSecret が含まれます。これらがプロジェクトに構成されているかどうかを確認するには、.csproj ファイルが存在するディレクトリから次のコマンドを実行します。
`dotnet user-secrets list`
- [`Startup.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Startup.cs) 認証を含む、使用するアプリとサービスの構成を行います。

### コントローラー  
- [`AccountController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/AccountController.cs) 管理者の同意を処理します。  
- [`NotificationController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/NotificationController.cs) 通知を受信します。  
- [`SubscriptionContoller.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/SubscriptionController.cs) サブスクリプションを作成および削除します。

### モデル
- [`Notification.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/Notification.cs) 変更通知を表します。
- [`MessageViewModel.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/MessageViewModel.cs) Notification (通知) ビューに表示されるデータを表す **MessageViewModel** を定義します。

### ヘルパー
- [`GraphSdkHelper.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/GraphServiceClientFactory.cs) Microsoft Graph の操作に使用される SDK クライアントを開始します。
- [`SubscriptionStore.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/SubscriptionStore.cs) 格納されたサブスクリプション情報のアクセス層です。サンプルでは、この情報を一時的に HttpRuntime.Cache に格納します。運用アプリでは通常、永続的なストレージのための何かしらの方法が使用されます。

### Microsoft.Identity.Web
- 以下を支援するのに役立つ一連の再利用可能クラスを含むヘルパー ライブラリ:
    
    - OpenId Connect ミドルウェアと MSAL.NET を使用して、Microsoft ID プラットフォーム v2.0 (AAD v2.0) で職場k、学校、または Microsoft 個人アカウントを使用してユーザーを認証およびログインします。 
    - ログアウトを処理し、MSAL.NET キャッシュからアカウントを削除します。
    - ログインしているユーザーの代わりにトークンを取得します。
    - いくつかのメソッドを呼び出すだけで、アプリケーションの `Startup.cs` ファイルから Web リソースをブートストラップします。



## トラブルシューティング 

| 問題 | 解決策 |
|:------|:------|
| サブスクリプションを作成しようとすると、403 禁止の応答が表示されます。 | アプリの登録に、Microsoft Graph の **Mail.Read** アプリケーションのアクセス許可 (「[アプリの登録](#register-the-app)」セクションで説明する通り) が含まれていること、テナント管理者がアプリに同意していることを確認します。 |
| 通知を受け取りません。 | ngrok を使用している場合、Web インターフェイス (http://127.0.0.1:4040) を使用して、通知が受信されているかどうかを確認できます。Ngrok を使用していない場合は、ホスティング サービスが提供するツールを使用してネットワーク トラフィックを監視するか、ngrok を使用してみてください。<br />Microsoft Graph で通知が送信されていない場合は、[MicrosoftGraph] タグの付いた[スタックオーバーフロー](https://stackoverflow.com/questions/tagged/MicrosoftGraph)問題を開いてください。サブスクリプション ID、作成時刻、応答からの要求 ID (所有している場合) を含めます。<br /><br />既知の問題:通知が受信され、取得されたメッセージが NotificationService に送信されることがありますが、このサンプルの SignalR クライアントは更新されません。これが発生するとき、これは通常、サブスクリプションが作成された後の最初の通知です。 |
| [*サブスクリプション検証リクエストがタイムアウトしました*] 応答が表示されました。 | これは、Microsoft Graph が 10 秒以内に検証応答を受信しなかったことを示します。<br /><br />ngrok を使用している場合は、エンドポイントにアクセスできることと、トンネル用にプロジェクトの HTTP ポート (HTTPS ではない) を指定したことを確認します。 |
| パッケージのインストール中にエラーが発生します。 | ソリューションを保存したローカル パスが長すぎたり深すぎたりしていないかご確認します。この問題は、ドライブのルート近くにソリューションを移動すると解決します。 |
| Microsoft.AspNetCore.SignalR.Serve rに関連するビルド エラーが発生しまる | パッケージ マネージャー コンソールに次のコマンドを入力します:'Install-Package Microsoft.AspNetCore.SignalR.Server -Version 0.2.0-rtm-22752 -Source https://dotnet.myget.org/F/aspnetcore-master/api/v3/index.json' |
| アプリは、'/'  アプリケーションで*サーバー エラーを開きます。リソースが見つかりません。* ブラウザー ページ。 | Visual Studio からアプリを実行するときは、CSHTML ビュー ファイルがアクティブなタブではないことを確認してください。 |

## 投稿

このサンプルに投稿する場合は、[CONTRIBUTING.MD](/CONTRIBUTING.md) を参照してください。

このプロジェクトでは、[Microsoft Open Source Code of Conduct (Microsoft オープン ソース倫理規定)](https://opensource.microsoft.com/codeofconduct/) が採用されています。詳細については、「[Code of Conduct の FAQ (倫理規定の FAQ)](https://opensource.microsoft.com/codeofconduct/faq/)」を参照してください。また、その他の質問やコメントがあれば、[opencode@microsoft.com](mailto:opencode@microsoft.com) までお問い合わせください。

## 質問とコメント

ASP.NET Core用 Microsoft Graph Webhook のサンプルに関するフィードバックをお寄せください。質問や提案につきましては、このリポジトリの「[問題](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/issues)」セクションで送信できます。

Microsoft Graph 全般の質問については、「[Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph)」に投稿してください。質問やコメントには、必ず "*MicrosoftGraph*" とタグを付けてください。

Microsoft Graph に関する変更の提案は、[UserVoice](https://officespdev.uservoice.com/) で行うことができます。

## その他のリソース

- [ASP.NET 4.6 用 Microsoft Graph Webhook のサンプル](https://github.com/microsoftgraph/aspnet-webhooks-rest-sample) (委任されたアクセス許可)
- [Node.js 用 Microsoft Graph Webhook のサンプル](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample) (委任されたアクセス許可)
- [Microsoft Graph の Webhook での作業](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
- [サブスクリプション リソース](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
- [Microsoft Graph ドキュメント](https://developer.microsoft.com/graph)

## 著作権
Copyright (c) 2019 Microsoft.All rights reserved.
