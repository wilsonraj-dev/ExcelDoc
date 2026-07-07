using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Localization;

namespace ExcelDoc.Server.Services;

public class MessageService : IMessageService
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MessageService(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Get(string key)
    {
        return _localizer[key];
    }

    public string Get(string key, params object[] arguments)
    {
        return _localizer[key, arguments];
    }
}
