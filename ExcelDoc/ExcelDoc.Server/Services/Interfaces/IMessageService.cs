namespace ExcelDoc.Server.Services.Interfaces;

public interface IMessageService
{
    string Get(string key);

    string Get(string key, params object[] arguments);
}
