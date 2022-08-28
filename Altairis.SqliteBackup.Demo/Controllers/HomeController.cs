using Microsoft.AspNetCore.Mvc;

namespace Altairis.SqliteBackup.Demo.Controllers;

public class HomeController : Controller {

    [Route("")]
    public ActionResult Index() => this.Ok("This web app does not do anything, just backs up the Sqlite database -- see console log.");


}
