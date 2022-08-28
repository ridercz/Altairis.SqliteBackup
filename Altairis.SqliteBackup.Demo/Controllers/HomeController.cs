using Microsoft.AspNetCore.Mvc;

namespace Altairis.SqliteBackup.Demo.Controllers;

public class HomeController : Controller {
    private const string FileStorePath = "App_Data/PostedFiles/received-backup_{0:yyMMddHHmmss_ffff}.bak";
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger) {
        this.logger = logger;
    }

    [Route("")]
    public ActionResult Index() => this.Ok("This web app does not do anything, just backs up the Sqlite database -- see console log.");

    [Route("receive-file")]
    public async Task<ActionResult> ReceivePostedFile() {
        // Validate and get the posted file
        if (this.Request.Form.Files.Count != 1) return this.BadRequest();
        var postedFile = this.Request.Form.Files[0];

        // Create folder to store files if needed
        var fileName = string.Format(FileStorePath, DateTime.Now);
        this.logger.LogInformation("Saving received file as {fileName}", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fileName) ?? string.Empty);

        // Save posted file
        using var localFile = System.IO.File.Create(fileName);
        await postedFile.CopyToAsync(localFile);
        return this.NoContent();
    }

}
