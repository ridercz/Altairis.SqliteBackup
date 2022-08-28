using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altairis.SqliteBackup;

public interface IBackupProcessor {

    void ProcessBackupFile(string backupFilePath);

}
