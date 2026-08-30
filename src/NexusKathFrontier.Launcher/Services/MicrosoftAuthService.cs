using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using XboxAuthNet.Game.Msal;
using XboxAuthNet.Game.Msal.OAuth;

namespace NexusKathFrontier.Launcher.Services;

public sealed class MicrosoftAuthService(string clientId)
{
    public static bool IsConfigured(string clientId) =>
        Guid.TryParse(clientId, out _) &&
        !clientId.Equals("YOUR-AZURE-APP-CLIENT-ID", StringComparison.OrdinalIgnoreCase);

    public async Task<MSession> AuthenticateAsync()
    {
        if (!IsConfigured(clientId))
            throw new InvalidOperationException(
                "Falta configurar microsoftClientId en appsettings.json. " +
                "Debes registrar la aplicación antes de iniciar sesión.");

        var app = await MsalClientHelper.BuildApplicationWithCache(clientId);
        var loginHandler = new JELoginHandlerBuilder()
            .WithOAuthProvider(new MsalCodeFlowProvider(app))
            .Build();

        try
        {
            return await loginHandler.AuthenticateSilently();
        }
        catch
        {
            return await loginHandler.AuthenticateInteractively();
        }
    }
}
