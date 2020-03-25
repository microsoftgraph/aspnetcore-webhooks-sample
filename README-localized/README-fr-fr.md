---
page_type: sample
description: "Cet exemple d’application web principale ASP.NET explique comment s’abonner aux webhooks à l’aide des autorisations déléguées."
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
# Exemple de Webhooks Microsoft Graph pour ASP.NET Core

Abonnez-vous à des [webhooks Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) pour être averti lorsque les données de vos utilisateurs changent, de sorte que vous n’avez pas besoin d’interroger les modifications.

Cet exemple d’application web ASP.NET Core explique comment s’abonner à des webhooks à l’aide des autorisations déléguées. Il utilise OpenID Connect pour la connexion/déconnexion à l’aide de la Plateforme d’identités Microsoft pour développeurs, [la bibliothèque d’authentification Microsoft pour .NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) (MSAL.NET) pour obtenir un jeton d’accès à l’aide du [flux de code d’authentification ](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)et la [Bibliothèque de client Microsoft Graph pour .NET](https://github.com/microsoftgraph/msgraph-sdk-dotnet) (SDK) pour appeler Microsoft Graph pour le compte d’un utilisateur qui s'est connecté avec succès à l'application web. Ces complexités ont été encapsulées dans le projet de bibliothèque réutilisable `Microsoft.Identity.Web`. 

>Consultez la liste des [autorisations déléguées](https://docs.microsoft.com/en-us/graph/api/subscription-post-subscriptions?view=graph-rest-1.0) permis pour chaque ressource prise en charge dans Microsoft Graph.

L’exemple d’application redirige vers le point de terminaison Azure AD *adminconsent* pour qu’un administrateur client puisse octroyer des autorisations déléguées directement à l’application. Une fois que l'administrateur a donné son accord, les utilisateurs du locataire peuvent créer un abonnement et surveiller les notifications. 

Voici les tâches courantes qu’une application effectue avec des abonnements webhook :

- Obtenez le consentement de vous abonner aux ressources des utilisateurs, puis obtenez un jeton d’accès.
- Utilisez le jeton d'accès pour [créer un abonnement](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/subscription_post_subscriptions) à une ressource.
- Renvoyez un jeton de validation pour confirmer l'URL de notification.
- Écoutez les notifications de Microsoft Graph et répondez avec un code d’État 202.
- Demandez plus d’informations sur les ressources modifiées à l’aide des données de la notification.

## Utilisation de l’exemple de Webhooks Microsoft Graph 

La capture d’écran ci-dessous montre la page de démarrage de l’application. 
  
![Exemple de Webhook Microsoft Graph pour la capture d’écran ASP.NET Core](readme-images/Page1.PNG)

Une fois que l'application crée un abonnement pour l'utilisateur connecté, Microsoft Graph envoie une notification au terminal enregistré lorsque des événements se produisent dans la ressource abonnée de l'utilisateur. L’application réagit ensuite à l’événement.

Cet exemple d’application s’abonne à la ressource `users/{user-id}/mailFolders('Inbox')/messages` pour les modifications `créées`. Lorsque les utilisateurs abonnés reçoivent un message électronique, l’application met à jour une page contenant des informations sur le message. La page affiche uniquement les messages appartenant à l’utilisateur connecté.

### Conditions préalables

Pour utiliser l’exemple Webhook Microsoft Graph pour ASP.NET Core, vous avez besoin des éléments suivants :

- Visual Studio 2017 installé sur votre ordinateur de développement. 
- .NET Core 2.1 ou version ultérieure (par exemple, pour Windows) est installé. Vous pouvez suivre les instructions de [.NET et C# - Prise en main dans 10 minutes](https://www.microsoft.com/net/core). En plus de développer sur Windows, vous pouvez développer sur [Linux](https://www.microsoft.com/net/core#linuxredhat), [Mac](https://www.microsoft.com/net/core#macos)ou [Docker](https://www.microsoft.com/net/core#dockercmd).
- Un [compte professionnel, scolaire ou personnel](https://dev.office.com/devprogram). Un compte d’administrateur de locataire est nécessaire pour consentir des autorisations d’application. 
- ID de l’application et clé de l’application que vous [inscrivez sur le portail Azure](#register-the-app). 
- Un point de terminaison public HTTPS pour recevoir et envoyer des demandes HTTP. Vous pouvez héberger celui-ci sur Microsoft Azure ou un autre service, ou vous pouvez [utiliser ngrok](#ngrok) ou un outil similaire pendant le test.

### Créer votre application

#### Sélectionnez le locataire dans lequel vous voulez créer votre application

1. Connectez-vous au [Portail Microsoft Azure](https://portal.azure.com) à l’aide d’un compte professionnel ou scolaire.
1. Si votre compte est présent dans plusieurs locataires Azure AD :
   1. Sélectionnez votre profil dans le menu situé dans le coin supérieur droit de la page, puis **basculer entre les répertoires**.
   1. Sélectionnez le locataire Azure AD dans lequel vous souhaitez créer votre application.

#### Inscription de l’application

1. Accédez au [Portail Microsoft Azure > enregistrement des applications](https://go.microsoft.com/fwlink/?linkid=2083908) pour enregistrer votre application.
1. Sélectionnez **Nouvelle inscription**.
1. Lorsque la **page Inscrire une application** s’affiche, saisissez les informations d’inscription de votre application :
   1. Dans la section **Nom**, saisissez un nom explicite qui s’affichera pour les utilisateurs de l’application. Par exemple : `MyWebApp`
   1. Dans la section **Types de comptes pris en charge**, sélectionnez **Comptes dans un annuaire organisationnel et comptes personnels Microsoft (par ex. Skype, Xbox, Outlook.com)**.
      > S’il existe plusieurs URI de redirection, vous devez les ajouter ultérieurement à partir de l’onglet **Authentification** une fois l’application créée.
1. Sélectionnez **S’inscrire** pour créer l’application.
1. Sur la page **Vue d’ensemble** de l’application, notez la valeur **ID d’application (client)** et conservez-la pour plus tard. Vous aurez besoin de cette valeur pour paramétrer le fichier de configuration de Visual Studio pour ce projet.
1. Dans la liste des pages de l’application, sélectionnez **Authentification**.
   1. Dans la section **URI de redirection**, sélectionnez **Web** dans la zone de liste déroulante et entrez les URI de redirection suivants :
       - `https://localhost:44334/signin-oidc`
       - `https://localhost:44334/Account/GrantPermissions`
1. Sélectionnez **Enregistrer**.
1. Dans la page **Certificats et clés secrètes**, dans la section **Clés secrètes de clients**, sélectionnez **Nouvelle clé secrète client**.
   1. Entrez une description de clé (par exemple `clé secrète de l’application`),
   1. Sélectionnez une durée de clé : **Dans 1 an**, **Dans 2 ans** ou **N’expire jamais**.
   1. Lorsque vous cliquez sur le bouton **Ajouter**, la valeur de la clé s’affiche. Copiez la clé et enregistrez-le dans un endroit sûr.

      Vous aurez besoin de cette clé ultérieurement pour configurer le projet dans Visual Studio. Cette valeur de clé ne sera plus affichée, ni récupérée par d’autres moyens. Par conséquent, enregistrez-la dès qu’elle est visible depuis le Portail Microsoft Azure.

1. Dans la liste des pages de l’application, sélectionnez **Permissions API**.
   1. Cliquez sur le bouton **Ajouter une autorisation**, puis assurez-vous que l’onglet **Microsoft APIs** est sélectionné.
   1. Dans la section **API Microsoft couramment utilisées**, sélectionnez **Microsoft Graph**.
   1. Dans la section **Autorisations applicatives**, assurez-vous que l’autorisation **Mail.Read.** est activée. Utilisez la zone de recherche, le cas échéant.
    > De plus, dans la section **Autorisations déléguées**, vérifiez l’autorisation déléguée User.Read d’Azure Active Directory, les utilisateurs peuvent se connecter à l’application pour initier le processus d’abonnement.
   1. Cliquez sur le bouton **Ajouter des autorisations**.
   
<a name="ngrok"></a>
### Configurer le proxy ngrok (facultatif) 
Vous devez exposer un point de terminaison public HTTPS pour créer un abonnement et recevoir des notifications de Microsoft Graph. Pendant le test, vous pouvez utiliser ngrok pour autoriser temporairement les messages de Microsoft Graph à passer par un tunnel vers une *localhost* port sur votre ordinateur. 

Vous pouvez utiliser l’interface web ngrok (http://127.0.0.1:4040) pour inspecter le trafic HTTP qui traverse le tunnel. Pour en savoir plus sur l’utilisation ngrok, consultez le [site web ngrok](https://ngrok.com/).  

1. Dans l’Explorateur de solutions, cliquez avec le bouton droit sur le projet **GraphWebhooks-Core**, puis sélectionnez **Propriétés**. 

1. Sous l’onglet **Debug**, copiez le numéro de port de l’**URL d’application **. 

	![Numéro de port de l’URL dans la fenêtre Propriétés](readme-images/PortNumber.png)

1. [Télécharger ngrok](https://ngrok.com/download) pour Windows.  

1. Décompressez le package et exécutez ngrok.exe.

1. Remplacez les deux valeurs d’espace réservé *{port-number}* dans la commande suivante par le numéro de port que vous avez copié, puis exécutez la commande dans la console ngrok.

   `ngrok http {port-number}-Host-Header = localhost : {port-number}`

	![Exemple de commande à exécuter dans la console ngrok](readme-images/ngrok1.PNG)

1. Copiez l’URL HTTPS affichée dans la console. Vous l’utiliserez pour configurer l’URL de notification dans l’exemple.

	![L’URL HTTPS de transfert dans la console ngrok](readme-images/ngrok2.PNG)

Gardez la console ouverte pendant le test. Si vous la fermez, le tunnel se ferme également et vous devrez générer une nouvelle URL et mettre à jour l’exemple.

>Consultez [Hébergement sans tunnel](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Hosting-the-sample-without-a-tunnel) et [pourquoi dois-je utiliser un tunnel ?](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Why-do-I-have-to-use-a-tunnel) pour plus d’informations sur l’utilisation de tunnels,.


## Configurez et exécutez l’exemple

1. Suivez ces [instructions](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-2.2) pour installer le package de client JavaScript ASP.NET Core SignalR dans l’application.
1. Expose un point de terminaison de notification HTTPS public. Il peut être exécuté sur un service tel que Microsoft Azure, ou vous pouvez créer un serveur proxy web en [utilisant ngrok](#ngrok) ou un outil similaire.

1. Ouvrez le fichier d’exemple **GraphWebhooks-Core.sln** dans Visual Studio 2017.

1. Dans l’Explorateur de solutions, ouvrez le fichier **appsettings.json** dans le répertoire racine du projet.  
 
   - Pour la clé **NotificationUrl**, remplacez *ENTER_YOUR_URL* par l’URL HTTPS. Conservez la partie */notification/Listen*. 

   Si vous utilisez ngrok, utilisez l’URL HTTPS que vous avez copiée. La valeur se présente comme suit :

   `"NotificationUrl": "https://2885f9c5.ngrok.io/notification/listen",`

   Il s’agit du point de terminaison d’URL qui reçoit les rappels de validation d’abonnement et les événements de notification provenant de Graph, via le serveur proxy configuré ci-dessus (ngrok, pour cet exemple).

1. Dans l’Explorateur de solutions, cliquez avec le bouton droit sur le nom du projet et sélectionnez **Gérer les secrets d’utilisateurs**. Cette application utilise la configuration[Gestionnaire de secret](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2) dans le stockage des données d’application sensibles : ClientId et ClientSecret.
    
    - Dans la fenêtre **secret.json** qui s’ouvre, collez le code ci-dessous.
        
        `"AzureAd": {
        "ClientId": "ENTER_YOUR_APP_ID",
        "ClientSecret": "ENTER_YOUR_SECRET"
  }`

    - Pour la clé **ClientId**, remplacez *ENTER_YOUR_APP_ID* par l’ID d’application de votre application Azure inscrite.  
    - Pour la clé**ClientSecret**, remplacez *ENTER_YOUR_SECRET* par la clé de votre application Azure enregistrée. Notez que dans les applications de production, vous devez toujours utiliser des certificats comme secrets d’application. pour cet exemple, nous allons utiliser un mot de passe secret partagé simple.

1. Assurez-vous que la console ngrok est encore en cours d’exécution, puis appuyez sur F5 pour générer et exécuter la solution en mode débogage. 

   >Si vous recevez des erreurs pendant l’installation des packages, vérifiez que le chemin d’accès local où vous avez sauvegardé la solution n’est pas trop long/profond. Pour résoudre ce problème, il vous suffit de déplacer la solution dans un dossier plus près du répertoire racine de lecteur.

### Utiliser l’application pour créer un abonnement
 

1. Sélectionnez **Connexion** dans le coin supérieur droit, puis connectez-vous à l’aide d’un compte professionnel ou scolaire.

1. Consentez les autorisations **Afficher votre profil de base** et **Se connecter en tant que vous**. 

1. Sur la page d’accueil de l’exemple, sélectionnez **Consentir l’autorisation d’administrateur**. Vous serez redirigé vers la page *adminconsent*.

1. Connectez-vous en tant qu’administrateur du locataire et consentez les autorisations **Lire le courrier dans toutes les boîtes aux lettres** et **Se connecter et lire le profil utilisateur**. Vous serez redirigé vers la page d’accueil de l’exemple. 

   À ce stade, les utilisateurs de votre locataire peuvent se connecter et créer un abonnement. Si vous ne consentez pas d’autorisations d’administrateur en premier, vous recevrez une erreur *Non autorisée*. Vous devez ouvrir l’exemple dans une nouvelle session de navigateur, car cet exemple met en cache le jeton initial.
    
1. Sélectionnez **Créer un abonnement**. La page **Abonnement** se charge avec les informations relatives à l’abonnement.

   >Cet exemple règle l’expiration de l’abonnement à 15 minutes à des fins de test.

	![Page d’application montrant les propriétés du nouvel abonnement](readme-images/Page2.PNG)
	
1. Sélectionnez le bouton **Surveiller les notifications**. 

1. Envoyez un courrier électronique à votre compte d’utilisateur. La page **Notification** affiche les propriétés du message. La mise à jour de la page peut prendre plusieurs secondes.

1. Si vous le souhaitez, vous pouvez également sélectionner le bouton **Supprimer un abonnement**. 


## Composants clés de l’exemple 
Les fichiers suivants contiennent du code lié à la connexion à Microsoft Graph, à la création d’abonnements et à la gestion des notifications.

- [`appsettings.json`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/appsettings.json) contient des valeurs utilisées pour l’authentification, l’autorisation et les URL de point de terminaison. 
- secrets.json contient les identifiants ClientId et ClientSecret utilisés pour l’authentification et l’autorisation. Pour vérifier si ceux-ci ont été configurés pour le projet, exécutez la commande suivante à partir du répertoire dans lequel se trouve le fichier .csproj :
`dotnet user-secrets list`
- [`Startup.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Startup.cs) configure l’application et les services qu’elle utilise, y compris l’authentification.

### Contrôleurs  
- [`AccountController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/AccountController.cs) gère le consentement de l’administrateur.  
- [`NotificationController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/NotificationController.cs) reçoit des notifications.  
- [`SubscriptionContoller.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/SubscriptionController.cs) crée et supprime les abonnements.

### Modèles
- [`Notification.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/Notification.cs) représente une notification de modification.
- [`MessageViewModel.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/MessageViewModel.cs) définit le **MessageViewModel** qui représente les données affichées dans l’affichage des notifications.

### Assistants
- [`GraphServiceClientFactory.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/GraphServiceClientFactory.cs) initialise le client du kit de développement logiciel (SDK) utilisé pour interagir avec Microsoft Graph.
- [`SubscriptionStore.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/SubscriptionStore.cs) couche d’accès pour les informations de l’abonnement stocké. L’exemple stocke temporairement les informations dans HttpRuntime.Cache. En général, les applications de production utilisent une méthode de stockage persistant.

### Microsoft.Identity.Web
- Bibliothèque d’assistance contenant un groupe de classes réutilisables utiles dans les rubriques suivantes :
    
    - Authentification et connexion des utilisateurs avec n’importe quel compte professionnel, scolaire ou personnel Microsoft sur la plateforme d’identité Microsoft v 2.0 (AAD v 2.0) à l’aide de l’intergiciel OpenId Connect et MSAL.NET. 
    - Gestion de la déconnexion et suppression du compte du cache MSAL.NET.
    - Acquisition de jetons au nom de l’utilisateur connecté.
    - Démarrage de la ressource web à partir du fichier `Startup.cs` dans l’application en appelant simplement quelques méthodes.



## Résolution des problèmes 

| Problème | Résolution |
| :------| :------|
| Vous obtenez une réponse 403 interdite lorsque vous essayez de créer un abonnement. | Assurez-vous que l’inscription de l’application inclut l’autorisation d’application **Mail.Read** pour Microsoft Graph (comme décrit dans la section [Inscrire l’application](#register-the-app)) et qu’un administrateur locataire a accordé l’autorisation à l’application. |  
| Vous ne recevez pas de notifications. | Si vous utilisez ngrok, vous pouvez utiliser l’interface web (http://127.0.0.1:4040) pour vérifier si la notification est reçue. Si vous n’utilisez pas ngrok, surveillez le trafic réseau à l’aide des outils fournis par votre service d’hébergement, ou essayez d’utiliser ngrok.<br />Si Microsoft Graph n’envoie pas de notifications, veuillez ouvrir un problème de [dépassement de capacité de la pile](https://stackoverflow.com/questions/tagged/MicrosoftGraph) marqué*[MicrosoftGraph]*. Incluez l’ID de l’abonnement, l’heure à laquelle il a été créé et l’ID de demande de la réponse (si vous l’avez).<br /><br />Problème connu : La notification est parfois reçue et le message extrait est envoyé à NotificationService, mais le client SignalR dans cet exemple ne se met pas à jour. Dans ce cas, il s’agit généralement de la première notification après la création de l’abonnement. |  
| Vous recevez une réponse *La demande de validation d’abonnement a expiré*. | Cela indique que Microsoft Graph n’a pas reçu de réponse de validation dans un délai de 10 secondes.<br /><br />Si vous utilisez ngrok, assurez-vous que votre point de terminaison est accessible et que vous avez spécifié le port HTTP de votre projet pour le tunnel (et non HTTPS). |  
| Vous recevez des erreurs pendant l’installation des packages. | vérifiez que le chemin d’accès local où vous avez sauvegardé la solution n’est pas trop long/profond. Le déplacement de la solution plus près du lecteur racine permet de résoudre ce problème. |
| Vous recevez des erreurs de build liées à Microsoft.AspNetCore.SignalR.Server | Tapez la commande suivante dans la console Gestionnaire de package : 'Install-Package Microsoft.AspNetCore.SignalR.Server -Version 0.2.0-rtm-22752 -Source https://dotnet.myget.org/F/aspnetcore-master/api/v3/index.json' |
| L’application ouvre sur une erreur serveur*dans '/' Application. La ressource est introuvable.* page du navigateur. | Assurez-vous qu’un fichier d’affichage CSHTML n’est pas l’onglet actif lorsque vous exécutez l’application à partir de Visual Studio. |

## Contribution

Si vous souhaitez contribuer à cet exemple, voir [CONTRIBUTING.MD](/CONTRIBUTING.md).

Ce projet a adopté le [code de conduite Open Source de Microsoft](https://opensource.microsoft.com/codeofconduct/). Pour en savoir plus, reportez-vous à la [FAQ relative au code de conduite](https://opensource.microsoft.com/codeofconduct/faq/) ou contactez [opencode@microsoft.com](mailto:opencode@microsoft.com) pour toute question ou tout commentaire.

## Questions et commentaires

Nous serions ravis de connaître votre opinion sur l’exemple de webhooks Microsoft Graph pour ASP.NET Core. Vous pouvez nous faire part de vos questions et suggestions dans la rubrique [Problèmes](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/issues) de ce référentiel.

Les questions générales sur Microsoft Graph doivent être publiées sur le [Dépassement de capacité de la pile](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Veillez à poser vos questions ou à rédiger vos commentaires en utilisant les tags *[MicrosoftGraph]*.

Vous pouvez suggérer des modifications pour Microsoft Graph sur [UserVoice](https://officespdev.uservoice.com/).

## Ressources supplémentaires

- [Exemple de Webhooks Microsoft Graph pour ASP.NET 4.6](https://github.com/microsoftgraph/aspnet-webhooks-rest-sample) (Autorisations déléguées)
- [Exemple de Webhooks Microsoft Graph pour Node.js](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample) (Autorisations déléguées)
- [Utiliser des Webhooks dans Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
- [Ressource abonnement](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
- [Documentation Microsoft Graph](https://developer.microsoft.com/graph)

## Copyright
Copyright (c) 2019 Microsoft. Tous droits réservés.
