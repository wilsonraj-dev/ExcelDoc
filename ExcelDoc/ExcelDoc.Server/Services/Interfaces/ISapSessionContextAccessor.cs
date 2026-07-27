using ExcelDoc.Server.Sap;

namespace ExcelDoc.Server.Services.Interfaces;

public interface ISapSessionContextAccessor
{
    SapSessionContext GetRequiredSession();

    string GetRequiredSessionKey();

    void SetSessionKey(string sessionKey);

    void SetJobSessionKey(string sessionKey);
}
