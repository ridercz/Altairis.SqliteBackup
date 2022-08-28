using System.ComponentModel.DataAnnotations;

namespace Altairis.SqliteBackup.Demo.Data;

public class StartupTime {

    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Time { get; set; }

}
