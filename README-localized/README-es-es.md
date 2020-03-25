---
page_type: sample
description: "En este ejemplo de la aplicación web ASP.NET Core se muestra cómo suscribirse a webhooks mediante permisos delegados."
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
# Ejemplo de Webhooks de Microsoft Graph para ASP.NET Core

Suscríbase a [webhooks de Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) para recibir una notificación cuando se produzcan cambios en los datos del usuario, de modo que no tenga que realizar un sondeo de los cambios.

Este ejemplo de la aplicación web de ASP.NET Core muestra cómo suscribirse a webhooks mediante permisos delegados. Se usa OpenID Connect para iniciar o cerrar sesión con la Plataforma de identidad de Microsoft para desarrolladores, la [Biblioteca de autenticación de Microsoft para .NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) (MSAL.NET) para obtener un token de acceso con el [flujo de código de autorización](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow), y la [Biblioteca cliente de Microsoft Graph para .NET](https://github.com/microsoftgraph/msgraph-sdk-dotnet) (SDK) para llamar a Microsoft Graph en nombre de un usuario que ha iniciado sesión correctamente en la aplicación web. Estas complejidades han sido encapsuladas en el proyecto de biblioteca reutilizable `Microsoft.Identity.Web`. 

>Vea la lista de [permisos delegados](https://docs.microsoft.com/en-us/graph/api/subscription-post-subscriptions?view=graph-rest-1.0) permitidos para cada recurso compatible en Microsoft Graph. 

La aplicación de muestra redirige al extremo de Azure AD *adminconsent* para que un administrador de inquilinos pueda conceder permisos delegados directamente a la aplicación. Después de que el administrador lo autorice, los usuarios del inquilino pueden crear una suscripción y ver las notificaciones. 

Las tareas comunes que una aplicación realiza con las suscripciones de webhooks son las siguientes:

- Obtener consentimiento para suscribirse a los recursos de los usuarios y después, obtener un token de acceso.
- Usar el token de acceso para [crear una suscripción](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/subscription_post_subscriptions) a un recurso.
- Devolver un token de validación para confirmar la dirección URL de notificación.
- Escuchar las notificaciones de Microsoft Graph y responder con un código de estado 202.
- Solicitar más información sobre los recursos modificados utilizando los datos en la notificación.

## Usando el Ejemplo de Webhooks de Microsoft Graph

La siguiente captura de pantalla muestra la página de inicio de la aplicación. 
  
![Captura de pantalla del Ejemplo de Webhooks de Microsoft Graph para ASP.NET Core](readme-images/Page1.PNG)

Después de que la aplicación crea una suscripción para el usuario que ha iniciado sesión, Microsoft Graph envía una notificación al extremo registrado cuando se produce un evento en el recurso del usuario suscrito. Entonces, la aplicación reaccionará al evento.

Esta aplicación de ejemplo se subscribe al recurso `usuarios/{User-ID}/mailFolders('Inbox')/messages` para los cambios`creados`. Cuando se le notifica que los usuarios suscritos reciben un mensaje de correo, la aplicación actualiza una página con información sobre el mensaje. La página solo muestra mensajes que pertenecen al usuario que ha iniciado sesión.

### Requisitos previos

Para usar el Ejemplo de Webhook de Microsoft Graph para ASP.NET Core, necesita lo siguiente:

- Visual Studio 2017 instalado en el equipo de desarrollo. 
- .NET Core 2.1 o posterior (por ejemplo, para Windows) instalado. Puede seguir las instrucciones que se indican en [.NET y C# : empiece en 10 minutos](https://www.microsoft.com/net/core). Además de desarrollar en Windows, también puede desarrollar en [Linux](https://www.microsoft.com/net/core#linuxredhat), [Mac](https://www.microsoft.com/net/core#macos)o [Docker](https://www.microsoft.com/net/core#dockercmd).
- Una cuenta de [trabajo, escuela o personal](https://dev.office.com/devprogram). Se necesita una cuenta de administrador de inquilinos para conceder los permisos de aplicación. 
- El Id. de la aplicación y la clave que [registró en el Portal de Azure](#register-the-app). 
- Un extremo HTTPS público para recibir y enviar solicitudes HTTP. Puede hospedar esto en Microsoft Azure u otro servicio, o bien puede [usar ngrok](#ngrok) o una herramienta similar mientras realiza las pruebas.

### Crear su aplicación

#### Elija el inquilino en el que desea crear su aplicación

1. Inicie sesión en el [Portal de Azure](https://portal.azure.com) con una cuenta profesional o educativa.
1. Si su cuenta se encuentra en más de un inquilino de Azure AD:
   1. Seleccione su perfil en el menú de la esquina superior derecha de la página y después, elija **Cambiar directorio**.
   1. Cambie su sesión al inquilino de Azure AD donde quiera crear su aplicación.

#### Registrar la aplicación

1. Vaya al [Portal de Azure > Registro de aplicaciones](https://go.microsoft.com/fwlink/?linkid=2083908) para registrar su aplicación.
1. Seleccione **Nuevo registro**.
1. Cuando aparezca la **página Registrar una aplicación**, introduzca la información de registro de su aplicación:
   1. En la sección **Nombre**, escriba un nombre significativo que se mostrará a los usuarios de la aplicación. Por ejemplo: `MyWebApp`
   1. En la sección **Tipos de cuentas admitidas**, seleccione **Cuentas en cualquier directorio organizacional y cuentas personales de Microsoft (por ejemplo, Skype, Xbox, Outlook.com)**.
      > Si hay más de un URI de redirección, tendrá que agregarlos desde la pestaña **Autenticación** más tarde, cuando la aplicación se haya creado correctamente.
1. Seleccione **Registrar** para crear la aplicación.
1. En la página **Información general** de la aplicación, busque el valor **Id. de la aplicación (cliente)** y guárdelo para más tarde. Necesitará este valor para configurar el archivo de configuración de Visual Studio para este proyecto.
1. En la lista de páginas de la aplicación, seleccione **Autenticación**.
   1. En la sección **URI de redirección**, seleccione **Web** en el cuadro combinado y escriba los siguientes URI de redirección:
       - `https://localhost:44334/signin-oidc`
       - `https://localhost:44334/Account/GrantPermissions`
1. Seleccione **Guardar**.
1. En la página **Certificados y secretos**, en la sección **Secretos de cliente**, elija **Nuevo secreto de cliente**.
   1. Escriba una descripción de clave (de la instancia `app secret`).
   1. Seleccione una duración de clave entre **En 1 año**, **En 2 años** o **Nunca expira**.
   1. Cuando haga clic en el botón **Agregar**, se mostrará el valor de clave. Copie el valor de clave y guárdelo en una ubicación segura.

      Necesitará esta clave más tarde para configurar el proyecto en Visual Studio. Este valor de clave no se volverá a mostrar, ni se podrá recuperar por cualquier otro medio, por lo que deberá registrarlo tan pronto como sea visible desde el portal de Azure.

1. En la lista de páginas de la aplicación, seleccione **Permisos de API**.
   1. Haga clic en el botón **Agregar un permiso** y después, asegúrese de que la pestaña **API de Microsoft** esté seleccionada.
   1. En la sección **API de Microsoft más usadas**, seleccione **Microsoft Graph**.
   1. En la sección **Permisos de aplicación**, asegúrese de que el permiso **Mail.Read.** está activado. Si es necesario, use el cuadro de búsqueda.
    > Asimismo, en la sección **Permisos delegados**, marque el permiso delegado User.Read para Azure Active Directory, de modo que los usuarios puedan iniciar sesión en la aplicación para iniciar el proceso de suscripción.
   1. Seleccione el botón **Agregar permisos**.
   
<a name="ngrok"></a>
### Configurar el proxy ngrok (opcional) 
Debe exponer un extremo HTTPS público para crear una suscripción y recibir notificaciones de Microsoft Graph. Mientras realiza las pruebas, puede usar ngrok para permitir temporalmente que los mensajes de Microsoft Graph hagan un túnel a un puerto *localhost* en su equipo. 

Puede usar la interfaz web de ngrok (http://127.0.0.1:4040) para inspeccionar el tráfico HTTP que pasa por el túnel. Para obtener más información sobre el uso de ngrok, visite el [sitio web de ngrok](https://ngrok.com/).  

1. En el Explorador de soluciones, haga clic en el botón derecho en el proyecto **GraphWebhooks-Core** y elija **Propiedades**. 

1. En la pestaña **Depurar**, copie el número de puerto de la **Dirección URL de la aplicación**. 

	![El número de puerto de la dirección URL en la ventana Propiedades](readme-images/PortNumber.png)

1. [Descargar ngrok](https://ngrok.com/download) para Windows.  

1. Descomprima el paquete y ejecute ngrok.exe.

1. Reemplace los dos valores de marcador de posición *{port-number}* en el siguiente comando con el número de puerto que copió, y después ejecute el comando en la consola de ngrok.

   `ngrok http {port-number} -host-header=localhost:{port-number}`

	![Comando de ejemplo para ejecutar en la consola de ngrok](readme-images/ngrok1.PNG)

1. Copie la dirección URL HTTPS que se muestra en la consola. Esto se usará para configurar su dirección URL de notificación en el ejemplo.

	![La dirección URL HTTPS de reenvío en la consola de ngrok](readme-images/ngrok2.PNG)

Mantenga la consola abierta mientras realiza las pruebas. Si la cierra, el túnel también se cerrará y tendrá que generar una nueva dirección URL y actualizar el ejemplo.

>Para obtener más información sobre el uso de túneles, consulte [Hospedaje sin un túnel](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Hosting-the-sample-without-a-tunnel) y [¿Por qué tengo que usar un túnel?](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample/wiki/Why-do-I-have-to-use-a-tunnel).


## Configurar y ejecutar el ejemplo

1. Siga estas [instrucciones](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-2.2) para instalar el paquete cliente de JavaScript para ASP.NET Core SignalR en la aplicación.
1. Exponga un extremo de notificación HTTPS público. Este puede ejecutarse en un servicio como Microsoft Azure, o puede crear un servidor web proxy [mediante ngrok](#ngrok) o una herramienta similar.

1. Abra el archivo de ejemplo **GraphWebhooks-Core.sln** en Visual Studio 2017.

1. En el Explorador de soluciones, abra el archivo **appsettings.json** en el directorio raíz del proyecto.  
 
   - En la clave **NotificationUrl**, reemplace *ENTER_YOUR_URL* por la dirección URL HTTPS. Mantenga la porción */notification/listen*. 

   Si está usando ngrok, utilice la dirección URL HTTPS que ha copiado. El valor será similar al siguiente:

   `"NotificationUrl": "https://2885f9c5.ngrok.io/notification/listen",`

   Este es el extremo de la dirección URL que recibirá las devoluciones de llamada de validación de suscripción y los eventos de notificación de Graph, mediante el servidor proxy configurado anteriormente (ngrok, para este ejemplo).

1. Aún en el Explorador de soluciones, haga clic con el botón derecho en el nombre del proyecto y seleccione **Administrar secretos de usuario**. Esta aplicación usa la configuración del [Administrador de secretos](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2) para almacenar datos confidenciales de la aplicación: ClientId y ClientSecret.
    
    - En la ventana **secret.json** que se abre, pegue el código que se muestra a continuación.
        
        `"AzureAd": {
        "ClientId": "ENTER_YOUR_APP_ID",
        "ClientSecret": "ENTER_YOUR_SECRET"
  }`

    - Para la clave **ClientId**, reemplace *ENTER_YOUR_APP_ID* con el Id. de aplicación de la aplicación de Azure registrada.  
    - Para la clave **ClientSecret**, reemplace *ENTER_YOUR_SECRET* con la clave de la aplicación de Azure registrada. Tenga en cuenta que en las aplicaciones de producción siempre debe usar certificados como secretos de aplicación, pero para este ejemplo usaremos una simple contraseña secreta compartida.

1. Asegúrese de que la consola de ngrok aún se esté ejecutando y después, presione F5 para crear y ejecutar la solución en el modo de depuración. 

   >Si recibe errores durante la instalación de los paquetes, asegúrese de que la ruta de acceso local donde colocó la solución no es demasiado larga o profunda. Para resolver este problema, mueva la solución más cerca de la unidad raíz.

### Usar la aplicación para crear una suscripción
 

1. Elija **Iniciar sesión** en la esquina superior derecha e inicie sesión con una cuenta profesional o educativa.

1. Consentimiento para **Ver su perfil básico** y permisos para **Iniciar sesión en su nombre**. 

1. En la página de inicio de ejemplo, elija **Conceder consentimiento de administrador**. Será redirigido a la página *adminconsent*.

1. Inicie sesión como administrador de inquilinos y conceda los permisos **Leer correo en todos los buzones** e **Iniciar sesión y leer el perfil de usuario**. Será redirigido de vuelta a la página de inicio de ejemplo. 

   En este momento, cualquier usuario de su inquilino pueden iniciar sesión y crear una suscripción. Si no concede los permisos de administrador en primer lugar, recibirá un error de *No autorizado*. Tendrá que abrir el ejemplo en una nueva sesión del explorador, ya que este ejemplo almacena en caché el token inicial.
    
1. Elija **Crear suscripción**. La página de **Suscripción** se carga con información sobre la suscripción.

   >Este ejemplo establece la expiración de la suscripción en 15 minutos para propósitos de prueba.

	![Página de la aplicación que muestra las propiedades de la nueva suscripción](readme-images/Page2.PNG)
	
1. Seleccione el botón **Ver las notificaciones**. 

1. Envíe un correo electrónico a su cuenta de usuario. La página de**Notificación** muestra las propiedades del mensaje. La actualización de la página puede tardar unos segundos.

1. Si lo desea, elija el botón **Eliminar suscripción**. 


## Componentes clave del ejemplo 
Los siguientes archivos contienen código relacionado con la conexión a Microsoft Graph, la creación de suscripciones y el control de las notificaciones.

- [`appsettings.json`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/appsettings.json) Contiene los valores utilizados para las direcciones URL de autenticación, autorización y extremo. 
- secrets.json Contiene el ClientId y ClientSecret usado para la autenticación y la autorización. Para comprobar si se han configurado para el proyecto, ejecute el siguiente comando desde el directorio en el que se encuentra el archivo .csproj:
`dotnet user-secrets list`
- [`Startup.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Startup.cs) Configura la aplicación y los servicios que usa, incluida la autenticación.

### Controladores  
- [`AccountController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/AccountController.cs) Controla el consentimiento de administrador.  
- [`NotificationController.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/NotificationController.cs) Recibe notificaciones.  
- [`SubscriptionContoller.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Controllers/SubscriptionController.cs) Crea y elimina suscripciones.

### Modelos
- [`Notification.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/Notification.cs) Representa una notificación de cambios.
- [`MessageViewModel.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Models/MessageViewModel.cs) Define el **MessageViewModel** que representa los datos que se muestran en la vista Notificación.

### Aplicaciones auxiliares
- [`GraphServiceClientFactory.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/GraphServiceClientFactory.cs) Inicia el cliente SDK que se utilizó para interactuar con Microsoft Graph.
- [`SubscriptionStore.cs`](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/blob/master/src/GraphWebhooks-Core/Helpers/SubscriptionStore.cs) Capa de acceso para la información de la suscripción almacenada. El ejemplo almacena temporalmente la información en HttpRuntime.Cache. Las aplicaciones de producción suelen usar algún método de almacenamiento persistente.

### Microsoft.Identity.Web
- Biblioteca auxiliar que contiene un conjunto de clases reutilizables que son útiles para ayudar con lo siguiente:
    
    - Autenticación e inicio de sesión de los usuarios con cualquier cuenta Profesional, Educativa o Personal de Microsoft en la Plataforma de identidad de Microsoft v2.0 (AAD v 2.0) con el middleware OpenId connect y MSAL.NET. 
    - Control del cierre de sesión y eliminación de la cuenta de la cache MSAL.NET.
    - Obtención de tokens en nombre del usuario que ha iniciado sesión.
    - Arranque del recurso web desde el archivo `Startup.cs` en la aplicación, con solo llamar a unos pocos métodos.



## Solución de problemas 

| Problema | Resolución |
|:------|:------|
| Recibe una respuesta 403 Prohibido al intentar crear una suscripción. | Asegúrese de que el registro de su aplicación incluya el permiso de la aplicación **Mail.Read** para Microsoft Graph (como se describe en la sección [Registrar la aplicación](#register-the-app)) y que un administrador de inquilinos haya concedido el permiso a la aplicación. |
| No recibe ninguna notificación. | Si usa ngrok, puede usar la interfaz web (http://127.0.0.1:4040) para ver si la notificación se está recibiendo. Si no está usando ngrok, supervise el tráfico de red con las herramientas que proporcione su servicio de hospedaje o pruebe a usar ngrok.<br />Si Microsoft Graph no está enviando las notificaciones, abra una incidencia con la etiqueta[Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph) en *[MicrosoftGraph]*. Incluya el Id. de suscripción, la hora en que se creó y el Id. de solicitud de la respuesta (si lo tiene).<br /><br />Problema conocido: En ocasiones, la notificación se recibe y el mensaje recuperado se envía a NotificationService, pero el cliente SignalR en este ejemplo no se actualiza. Cuando esto ocurre, suele ser con la primera notificación después de que se crea la suscripción. |
| Recibe una respuesta de *Se ha agotado el tiempo de espera de la solicitud de validación de suscripción*. | Esto indica que Microsoft Graph no recibió una respuesta de validación en 10 segundos.<br /><br />Si usa ngrok, asegúrese de que el extremo es accesible y de que especificó el puerto HTTP del proyecto para el túnel (no HTTPS). |
| Recibe errores durante la instalación de los paquetes | Asegúrese de que la ruta de acceso local donde colocó la solución no es demasiado larga o profunda. Para solucionar este problema, mueva la solución más cerca de la unidad raíz. |
| Recibe errores de compilación relacionados con Microsoft.AspNetCore.SignalR.Server | Escriba este comando en la Consola del Administrador de paquetes: 'Install-Package Microsoft.AspNetCore.SignalR.Server -Version 0.2.0-rtm-22752 -Source https://dotnet.myget.org/F/aspnetcore-master/api/v3/index.json' |
| La aplicación se abre en una página del explorador *Error del servidor en la aplicación '/'. No se puede encontrar el recurso.* | Asegúrese de que un archivo de vista CSHTML no sea la pestaña activa cuando ejecute la aplicación desde Visual Studio. |

## Colaboradores

Si quiere hacer su aportación a este ejemplo, vea [CONTRIBUTING.MD](/CONTRIBUTING.md).

Este proyecto ha adoptado el [Código de conducta de código abierto de Microsoft](https://opensource.microsoft.com/codeofconduct/). Para obtener más información, vea [Preguntas frecuentes sobre el código de conducta](https://opensource.microsoft.com/codeofconduct/faq/) o póngase en contacto con [opencode@microsoft.com](mailto:opencode@microsoft.com) si tiene otras preguntas o comentarios.

## Preguntas y comentarios

Nos encantaría recibir sus comentarios sobre Webhooks de Microsoft Graph para ASP.NET Core. Puede enviarnos sus preguntas y sugerencias a través de la sección [Problemas](https://github.com/microsoftgraph/aspnetcore-apponlytoken-webhooks-sample/issues) de este repositorio.

Las preguntas sobre Microsoft Graph en general deben publicarse en [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Asegúrese de que sus preguntas o comentarios estén etiquetados con *[MicrosoftGraph]*.

Puede sugerir cambios para Microsoft Graph en [UserVoice](https://officespdev.uservoice.com/).

## Recursos adicionales

- [Ejemplo de Webhooks de Microsoft Graph para ASP.NET 4.6](https://github.com/microsoftgraph/aspnet-webhooks-rest-sample) (Permisos delegados)
- [Ejemplo de Webhooks de Microsoft Graph para Node.js](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample) (Permisos delegados)
- [Trabajar con Webhooks en Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
- [Recurso de suscripción](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
- [Documentación de Microsoft Graph](https://developer.microsoft.com/graph)

## Derechos de autor
Copyright (c) 2019 Microsoft. Todos los derechos reservados.
