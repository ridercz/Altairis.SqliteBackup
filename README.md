# Altairis Sqlite Backup

[![NuGet Status](https://img.shields.io/nuget/v/Altairis.SqliteBackup.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Altairis.SqliteBackup/)


Sqlite is well-known embedded database, usable also in ASP.NET Core. It's "good enough" for many applications, but of course has its drawbacks compared to traditional databases like Microsoft SQL Server. One of them is the absence of proper automated backup.

Embedded database can be backed up by copying its database file, but does not contain any mechanism to do it automatically and while the application is running.

This library takes care of that. When set-up, the application will regularly perform backup of its data file and optionally upload it to external or cloud storage.

**For documentation and samples see [wiki](https://github.com/ridercz/Altairis.SqliteBackup/wiki).**

## Contributor Code of Conduct

This project adheres to No Code of Conduct. We are all adults. We accept anyone's contributions. Nothing else matters.

For more information please visit the [No Code of Conduct](https://github.com/domgetter/NCoC) homepage.