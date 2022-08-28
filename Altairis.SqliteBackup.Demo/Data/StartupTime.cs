using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altairis.SqliteBackup.Demo.Data;

public class StartupTime {

    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Time { get; set; }

}
