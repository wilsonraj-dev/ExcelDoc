using System.Security.Claims;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;
using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Services;

public sealed class SapSessionContextAccessor : ISapSessionContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISapSessionStore _sessionStore;
    private string? _explicitSessionKey;
    private bool _useJobLease;

    public SapSessionContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        ISapSessionStore sessionStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionStore = sessionStore;
    }

    public SapSessionContext GetRequiredSession()
    {
        var sessionKey = GetRequiredSessionKey();
        var found = _useJobLease
            ? _sessionStore.TryGetForJob(sessionKey, out var session)
            : _sessionStore.TryGet(sessionKey, out session);

        if (found && session is not null)
        {
            return session;
        }

        throw new SapSessionExpiredException(
            "A sessão do SAP Business One expirou. Entre novamente no sistema.");
    }

    public string GetRequiredSessionKey()
    {
        var sessionKey = _explicitSessionKey
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(
                CustomClaimTypes.SapSessionKey);

        return !string.IsNullOrWhiteSpace(sessionKey)
            ? sessionKey
            : throw new SapSessionExpiredException(
                "Sessão do SAP Business One não encontrada.");
    }

    public void SetSessionKey(string sessionKey)
    {
        _useJobLease = false;
        _explicitSessionKey = !string.IsNullOrWhiteSpace(sessionKey)
            ? sessionKey
            : throw new ArgumentException(
                "A chave da sessão SAP é obrigatória.",
                nameof(sessionKey));
    }

    public void SetJobSessionKey(string sessionKey)
    {
        SetSessionKey(sessionKey);
        _useJobLease = true;
    }
}
