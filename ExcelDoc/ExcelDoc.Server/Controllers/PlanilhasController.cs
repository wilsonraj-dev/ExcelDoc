using ExcelDoc.Server.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelDoc.Server.Controllers;

[ApiController]
[Route("api/planilhas")]
[Authorize(Roles = AuthRoles.All)]
public class PlanilhasController(IWebHostEnvironment environment) : ControllerBase
{
    private const string ExampleSpreadsheetFileName = "Planilha Exemplo.xlsx";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("exemplo")]
    public IActionResult DownloadExample()
    {
        var filePath = Path.Combine(environment.ContentRootPath, "Planilha", ExampleSpreadsheetFileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, ExcelContentType, ExampleSpreadsheetFileName);
    }
}
