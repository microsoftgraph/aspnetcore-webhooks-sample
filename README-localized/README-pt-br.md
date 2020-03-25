---
page_type: sample
description: "Este exemplo de aplicativo Web ASP.NET mostra como se inscrever em webhooks usando permissões delegadas."
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
# Exemplo de webhooks do Microsoft Graph para ASP.NET Core

Inscreva-se no [Webhooks do Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) para ser notificado quando os dados do usuário mudarem, para que você não precise fazer enquetes para mudanças.

Este exemplo de aplicativo Web ASP.NET Core mostra como assinar o webhook usando as permissões delegadas. Ele usa o OpenID Connect para entrar/sair usando a plataforma de identidade da Microsoft para desenvolvedores, a [Biblioteca de autenticação da Microsoft para o .NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) (MSAL.NET) para obter um token de acesso usando o [fluxo de código de autenticação](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow) e a [Biblioteca de Clientes do Microsoft Graph para .NET](https://github.com/microsoftgraph/msgraph-sdk-dotnet) (SDK) para chamar o Microsoft Graph em nome de um usuário que entrou com êxito no aplicativo Web. Essas complexidades foram encapsuladas no projeto de biblioteca reutilizável `Microsoft.Identity.Web`. 

>Confira a lista de [permissões delegadas](https://docs.microsoft.com/en-us/graph/api/subscription-post-subscriptions?view=graph-rest-1.0) permitidas para cada recurso compatível no Microsoft Graph.

O aplicativo de exemplo redireciona para o ponto de extremidade *adminconsent* do Azure AD, para que um administrador de locatários possa conceder permissões delegadas diretamente para o aplicativo. Após a concessão do administrador, os usuários no locatário podem criar uma assinatura e visualizar as notificações. 

A seguir, são apresentadas tarefas comuns que um aplicativo executa com assinaturas do webhooks:

- Obtenha consentimento para inscrever-se nos recursos de usuários e receber um token de acesso.
- Use o token de acesso para [criar uma assinatura](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/subscription_post_subscriptions) para um recurso.
- Devolva um token de validação para confirmar a URL da notificação.
- Ouça as notificações do Microsoft Graph e responda com o código de status 202.
- Solicite mais informações sobre os recursos alterados usando os dados da notificação.

## Usar o exemplo de webhooks do Microsoft Graph

A captura de tela abaixo mostra a página inicial do aplicativo. 
  
![Captura de tela do exemplo de webhook do Microsoft Graph para ASP.NET Core](readme-images/Page1.PNG)

Depois que o aplicativo cria uma assinatura para o usuário conectado, o Microsoft Graph envia uma notificação para o ponto de extremidade registrado quando acontecem eventos no recurso assinado do usuário. Em seguida, o aplicativo reage ao evento.

Esse aplicativo de exemplo se inscreve no recurso `users/{user-id}/mailFolders('Inbox')/messages` para as alterações `criadas`. Quando notificado que os usuários inscritos recebem uma mensagem de email, o aplicativo atualiza uma página com informações sobre a mensagem. A página exibe somente as mensagens pertencentes ao usuário conectado.

### Pré-requisitos

Para usar o exemplo de webhook do Microsoft Graph para ASP.NET Core, você precisa do seguinte:

- Visual Studio 2017 instalado no computador de desenvolvimento. 
- .NET Core 2.1 ou posterior (por exemplo, para Windows) instalado. Você pode seguir as instruções em [.NET e C# – Introdução em 10 minutos](https://www.microsoft.com/net/core). Além de desenvolver no Windows, você pode desenvolver no [Linux](https://www.microsoft.com/net/core#linuxredhat), [Mac](https://www.microsoft.com/net/core#macos) ou [Docker](https://www.microsoft.com/net/core#dockercmd).
- Uma [conta corporativa, escolar ou pessoal](https://dev.office.com/devprogram). É necessário ter uma conta de administrador de locatário para conceder as permissões de aplicativo. 
- A ID do aplicativo e a chave do aplicativo que você [registra no Portal do Azure](#register-the-app). 
- Um ponto de extremidade de HTTPS público para receber e enviar solicitações HTTP. Você pode hospedar isso no Microsoft Azure ou em outro serviço, ou pode [usar o ngrok](#ngrok) ou uma ferramenta semelhante durante o teste.

### Criar seu aplicativo

#### Escolher o locatário para o qual você deseja criar seu aplicativo

1. Entre no [portal do Azure](https://portal.azure.com) usando uma conta corporativa ou de estudante.
1. Se a sua conta estiver presente em mais de um locatário do Azure AD:
   1. Selecione seu perfil no menu no canto superior direito da página e **Alterne o diretório**.
   1. Altere a sessão no locatário do Azure AD em que você deseja criar o aplicativo.

#### Registrar o aplicativo

1. Navegue até o [Portal do Azure > Registros de aplicativo](https://go.microsoft.com/fwlink/?linkid=2083908) para registrar seu aplicativo.
1. Selecione **Novo registro**.
1. Quando a página **Registrar um aplicativo** for exibida, insira as informações de registro do aplicativo:
   1. Na seção **Nome**, insira um nome relevante que será exibido aos usuários do aplicativo. Por exemplo: `MyWebApp`
   1. Na seção **Tipos de conta com suporte**, selecione **Contas em qualquer diretório organizacional e contas pessoais da Microsoft (por exemplo, Skype, Xbox, Outlook.com)**.
      > Se houver mais de um URI de redirecionamento, será necessário adicioná-los posteriormente na guia **Autenticação**, depois que o aplicativo for criado com êxito.
1. Selecione **Registrar** para criar o aplicativo.
1. Na página **Visão geral** do aplicativo, encontre o valor de **ID do aplicativo (cliente)** e registre-o para usar mais tarde. Este valor será necessário para configurar o arquivo de configuração do Visual Studio para este projeto.
1. Na lista de páginas para o aplicativo, selecione **Autenticação**.
   1. Na seção **Redirecionar URIs**, selecione **Web** na caixa de combinação e digite os seguintes URIs de redirecionamento:
       - `https://localhost:44334/signin-oidc`
       - `https://localhost:44334/Account/GrantPermissions`
1. Selecione **Salvar**.
1. Na página **Certificados e segredos**, na seção **Segredos do cliente**, escolha **Novo segredo do cliente**.
   1. Insira uma descrição da chave (por exemplo, `segredo do aplicativo`).
   1. Selecione uma duração de chave de **1 ano**, **2 anos** ou **Nunca Expirará**.
   1. Ao clicar no botão **Adicionar**, o valor da chave será exibido. Copie o valor da chave e salve-o em um local seguro.

      Você precisará dessa chave mais tarde para configurar o projeto no Visual Studio. Esse valor da chave não será exibido novamente, nem será recuperável por nenhum outro meio, portanto, grave-o assim que estiver visível no portal do Azure.

1. Na lista de páginas do aplicativo, selecione **Permissões de API**.
   1. Clique no botão **Adicionar uma permissão** e verifique se a guia **APIs da Microsoft** está selecionada.
   1. Na seção **APIs mais usadas da Microsoft**, selecione **Microsoft Graph**.
   1. Na seção **Permissões do aplicativo**, certifique-se de que a permissão **Mail.Read.** está marcada. Use a caixa de pesquisa, se necessário.
    > Além disso, na seção **Permissões delegadas**, marque a permissão delegada User.Read para o Azure Active Directory, para que os usuários possam entrar no aplicativo para iniciar o processo de assinatura.
   1. Selecione o botão **Adicionar permissão**.
   
<a name="ngrok"></a>
### Configurar o proxy ngrok (opcional) 
Você deve expor um ponto de extremidade HTTPS público para criar uma assinatura e receber as notificações do Microsoft Graph. Ao testar o, você pode usar o ngrok para permitir temporariamente que as mensagens do Microsoft Graph sejam encapsuladas para uma porta *localhost* em seu computador. 

Você pode usar a interface da Web do ngrok (http://127.0.0.1:4040) para inspecionar o tráfego HTTP que passa pelo encapsulamento. Para saber mais sobre como usar o ngrok, confira o [site do ngrok](https://ngrok.com/).  

1. Em Explorador de Projetos, clique com o botão direito do mouse no projeto **GraphWebhooks-Core** e escolha **Propriedades**. 

1. Na guia **Debug**, copie o número da porta da **URL do aplicativo**. 

	![O número da porta da URL na janela Propriedades](readme-images/PortNumber.png)

1. [Baixar ngrok](https://ngrok.com/download) para Windows.  

1. Descompacte o pacote e execute ngrok.exe.

1. Substitua os dois valores de espaço reservado *{port-number}* no comando abaixo com o número da porta que você copiou e execute o comando no console ngrok.

   `ngrok http {port-number} -host-header=localhost:{port-number}`

	![Exemplo do comando para executar no console ngrok](readme-images/ngrok1.PNG)

1. Copie a URL HTTPS exibida no console. Você usará essa configuração para definir a URL de notificação no exemplo.

	![A URL HTTPS de encaminhamento no console ngrok](readme-images/ngrok2.PNG)

Mantenha o console aberto durante o teste. Caso feche-o, o encapsulamento também é fechado, e você precisará gerar uma nova URL e atualizar o exemplo.

>Confira [Hospedagem sem um encapsulamento](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Hosting-the-sample-without-a-tunnel) e [Por que eu preciso usar um encapsulamento?](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Why-do-I-have-to-use-a-tunnel) para saber mais sobre como usar encapsulamentos.


## Configurar e executar o exemplo

1. Siga estas [instruções](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-2.2) para instalar o pacote de cliente javascript SignalR do ASP.NET Core no aplicativo.
1. Exponha um ponto de extremidade de notificação de HTTPS público. Ele pode ser executado em um serviço como o Microsoft Azure ou você pode criar um servidor Web proxy [usando o ngrok](#ngrok) ou uma ferramenta semelhante.

1. Abra o arquivo de exemplo **GraphWebhooks-Core.sln** no Visual Studio 2017.

1. No Gerenciador de Soluções, abra o arquivo **appsettings.json** na pasta raiz do projeto.  
 
   - Na chave **NotificationUrl**, substitua *ENTER_YOUR_URL* pela URL HTTPS. Mantenha a parte */notification/listen*. 

   Se você estiver usando o ngrok, use a URL HTTPS que você copiou. O valor será parecido com isto:

   `"NotificationUrl": "https://2885f9c5.ngrok.io/notification/listen",`

   Esse é o ponto de extremidade da URL que receberá chamadas de retorno de validação da assinatura e eventos de notificação do Graph, por meio do servidor proxy configurado acima (neste exemplo o ngrok).

1. No Gerenciador de soluções, clique com botão direito no nome do projeto e selecione **Gerenciar Segredos do Usuário**. Esse aplicativo usa a configuração do [Gerenciador de Segredos](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2) no armazenamento de dados confidenciais do aplicativo, ClientId e ClientSecret.
    
    - Na janela **secret.json** que é aberta, cole o código abaixo.
        
        `"AzureAd": {
        "ClientId": "ENTER_YOUR_APP_ID",
        "ClientSecret": "ENTER_YOUR_SECRET"
  }`

    - Substitua *ENTER_YOUR_APP_ID* na chave **ClientId** com a ID do aplicativo registrada no Azure.  
    - Substitua *ENTER_YOUR_SECRET* na chave **ClientSecret** com a chave do aplicativo registrado no Azure. Observe que nos aplicativos de produção você sempre deve usar os certificados como segredos do seu aplicativo, mas, neste exemplo, usaremos uma senha de secreta simples compartilhada.

1. Verifique se o console ngrok ainda está em execução e pressione F5 para criar e executar a solução no modo de depuração. 

   >Caso receba mensagens de erro durante a instalação de pacotes, verifique se o caminho para o local onde você colocou a solução não é muito longo ou extenso. Para resolver esse problema, coloque a solução junto à raiz da unidade.

### Usar o aplicativo para criar uma assinatura
 

1. Escolha **Entrar** no canto superior direito e entre com uma conta corporativa ou de estudante.

1. Consentimento para as permissões **Exibir seu perfil básico** e **Entrar como você**. 

1. Na página inicial do exemplo, escolha **Conceder consentimento de administrador**. Você será redirecionado para a página *adminconsent*.

1. Entre como um administrador de locatários e dê o consentimento para as permissões **Ler emails em todas as caixas de correio** e **Entrar e ler o perfil do usuário**. Você será redirecionado para a página inicial do exemplo. 

   Neste momento, qualquer usuário em seu locatário pode entrar e criar uma assinatura. Caso você não conceda primeiro as permissões de administrador, receberá um erro de *Não autorizado*. Será preciso abrir o exemplo em uma nova sessão do navegador, pois esse exemplo armazena em cache o token inicial.
    
1. Escolha **Criar assinatura**. A página **Assinatura** é carregada com as informações dessa assinatura.

   >Para fins de teste, este exemplo define a expiração da assinatura como 15 minutos.

	![Página do aplicativo mostrando as propriedades da nova assinatura](readme-images/Page2.PNG)
	
1. Escolha o botão **Ver as notificações**. 

1. Envie um email para sua conta de usuário. A página de **Notificação** exibe as propriedades da mensagem. Pode levar alguns segundos para que a página seja atualizada.

1. Opcionalmente, escolha o botão **Excluir assinatura**. 


## Componentes principais do exemplo 
Os seguintes arquivos contêm códigos relacionados à conexão com o Microsoft Graph, à criação de assinaturas e ao tratamento de notificações.

- [`appsettings.json`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/appsettings.json) contém valores usados para autenticação, autorização e URLs de ponto de extremidade. 
- secrets.json contém o ClientId e o ClientSecret usados para autenticação e autorização. Para verificar se eles foram configurados para o projeto, execute o seguinte comando no diretório no qual está o arquivo .csproj:
`dotnet user-secrets list`
- [`Startup.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Startup.cs) configura o aplicativo e os serviços que ele usa, incluindo autenticação.

### Controladores  
- [`AccountController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/AccountController.cs) lida com o consentimento do administrador.  
- [`NotificationController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/NotificationController.cs) recebe as notificações.  
- [`SubscriptionContoller.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/SubscriptionController.cs) cria e exclui as assinaturas.

### Modelos
- [`Notification.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/Notification.cs) representa uma notificação de alteração.
- [`MessageViewModel.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/MessageViewModel.cs) define o **MessageViewModel** que representa os dados exibidos no Modo de exibição de notificação.

### Auxiliares
- [`GraphServiceClientFactory.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/GraphServiceClientFactory.cs) inicia o cliente SDK usado para interagir com o Microsoft Graph.
- [`SubscriptionStore.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/SubscriptionStore.cs) camada de acesso para informações de assinatura armazenadas. O exemplo armazena temporariamente as informações em HttpRuntime.Cache. Os aplicativos de produção geralmente usam alguns métodos de armazenamento persistente.

### Microsoft.Identity.Web
- A biblioteca auxiliar contém um conjunto de classes reutilizáveis que são úteis para ajudar com o seguinte:
    
    - Autenticação e entrada de usuários com contas pessoais da Microsoft, corporativas ou de estudante na plataforma de identidade da Microsoft v2.0 (AAD v2.0) usando o middleware de conexão OpenId e MSAL.NET. 
    - Manipulação de saída e remoção da conta do cache do MSAL.NET.
    - Aquisição de token em nome do usuário conectado.
    - Inicializando o recurso da Web a partir do arquivo `Startup.cs` no aplicativo chamando apenas alguns métodos.



## Solução de problemas 

| Problema | Resolução |
|:------|:------|
| Você recebe uma resposta de 403 Proibido ao tentar criar uma assinatura. | Certifique-se de que o registro do aplicativo inclui a permissão **Mail.Read** do aplicativo para o Microsoft Graph (como descrito na seção [Registrar o aplicativo](#register-the-app)) e se um administrador de locatários concedeu consentimento para o aplicativo. |  
| Você não recebe notificações. | Se estiver usando o ngrok, é possível usar a interface da Web (http://127.0.0.1:4040) para ver se a notificação é recebida. Se você não estiver usando o ngrok, monitore o tráfego de rede usando as ferramentas fornecidas pelo serviço de hospedagem ou tente usar o ngrok.<br />Se o Microsoft Graph não estiver enviando notificações, abra um problema no [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph) marcado com *[MicrosoftGraph]*. Inclua a ID da assinatura, a hora em que foi criada e a ID da solicitação da resposta (se você tiver).<br /><br />Problemas conhecidos: Às vezes, a notificação é recebida e a mensagem recuperada é enviada ao NotificationService, mas o cliente SignalR neste exemplo não é atualizado. Quando isso acontece, costuma ser a primeira notificação após a criação da assinatura. |  
| Você recebe uma resposta *Solicitação de validação de assinatura expirou*. | Isso indica que o Microsoft Graph não recebeu uma resposta de validação no prazo de 10 segundos.<br /><br />Se você estiver usando o ngrok, certifique-se de que o ponto de extremidade está acessível e se você especificou a porta HTTP do seu projeto para o encapsulamento (não HTTPS). |  
| Caso receba mensagens de erro durante a instalação de pacotes. | Verifique se o caminho para o local onde você colocou a solução não é muito longo ou extenso. Para resolver esse problema, coloque a solução junto à raiz da unidade. |
| Você recebe erros de compilação relacionados ao Microsoft.AspNetCore.SignalR.Server | Digite esse comando no Console do Gerenciador de Pacotes: 'Install-Package Microsoft.AspNetCore.SignalR.Server -Version 0.2.0-rtm-22752 -Source https://dotnet.myget.org/F/aspnetcore-master/api/v3/index.json' |
| O aplicativo é aberto na página do navegador com *Erro de servidor no aplicativo '/'. Não é possível encontrar o recurso.*. | Verifique se o arquivo de modo de exibição CSHTML não é a guia ativa ao executar o aplicativo a partir do Visual Studio. |

## Colaboração

Se quiser contribuir para esse exemplo, confira [CONTRIBUTING.MD](/CONTRIBUTING.md).

Este projeto adotou o [Código de Conduta de Código Aberto da Microsoft](https://opensource.microsoft.com/codeofconduct/).  Para saber mais, confira as [Perguntas frequentes sobre o Código de Conduta](https://opensource.microsoft.com/codeofconduct/faq/) ou entre em contato pelo [opencode@microsoft.com](mailto:opencode@microsoft.com) se tiver outras dúvidas ou comentários.

## Perguntas e comentários

Adoraríamos receber os seus comentários sobre o exemplo de webhooks do Microsoft Graph para ASP.NET Core. Você pode enviar perguntas e sugestões na seção [Problemas](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/issues) deste repositório.

As perguntas sobre o Microsoft Graph em geral devem ser postadas no [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Não deixe de marcar as perguntas ou comentários com *[MicrosoftGraph]*.

Você pode sugerir alterações no Microsoft Graph em [UserVoice](https://officespdev.uservoice.com/).

## Recursos adicionais

- [Exemplo de Webhooks do Microsoft Graph para ASP.NET 4.6](https://github.com/microsoftgraph/aspnet-webhooks-rest-sample) (permissões delegadas)
- [Exemplo de Webhooks do Microsoft Graph para Node.js](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample) (permissões delegadas)
- [Trabalhando com webhooks no Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
- [Recurso de assinatura](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
- [Documentação do Microsoft Graph](https://developer.microsoft.com/graph)

## Direitos autorais
Copyright (c) 2019 Microsoft. Todos os direitos reservados.
