# TODO

Both:
- [ ] App to App comunication.
  - [x] Notification with link to default map application with the route.
    - Problems with the notification. Working with strings is problematic.
    - Cannot send it over the Network with WNS (paid service on Azure).
- [x] SQL Database.
- [x] SQL Querries.

Control:
- [x] Map Control and view.
- [x] Search in map.
  - [x] Search according to time-of-travel-to-destination.
  - [x] Search according to birdfly distance.
- [x] All donors without geoposition must be geopositioned
  - Either do a bing map service query and store geoposition
  - Or just use the address in map if geoposition is not present.


Mobile Client:
- [x] "2nd Launch Page".
- [x] App re-open opens the "2nd Launch Page".
- [ ] App configuration.
  - Useless as we can't send data.

## Help
- [Data Binding](https://code.msdn.microsoft.com/Data-Binding-in-UWP-b5c98114)
- [App 2 App communication](https://blogs.windows.com/buildingapps/2015/09/22/using-cross-app-communication-to-make-apps-work-together-10-by-10/)
- [App Services](https://msdn.microsoft.com/en-us/windows/uwp/launch-resume/how-to-create-and-consume-an-app-service)
- [Notifications](https://msdn.microsoft.com/en-us/windows/uwp/controls-and-patterns/tiles-badges-notifications)
- [Send Notifications](https://msdn.microsoft.com/en-us/library/windows/apps/mt187196.aspx)
- [How to connect to SQL server database from a Windows 10 UWP app](http://stackoverflow.com/questions/32885735/how-to-connect-to-sql-server-database-from-a-windows-10-uwp-app)
- [How to access data from SQL Server database in Windows Store app C++/C#](https://code.msdn.microsoft.com/How-to-access-data-from-5f2602ec#content)
  - No time to implement it.
- [How to access data from SQL Server database in Windows Store app C#](https://code.msdn.microsoft.com/How-to-access-data-from-f11ef8df)
  - Same.
- [Getting Started with Entity Framework on UWP](http://docs.efproject.net/en/latest/platforms/uwp/getting-started.html)
 - Doesn't work for some reason unknown.
- [Encrypt data](https://msdn.microsoft.com/en-us/library/windows/apps/windows.security.cryptography.core.cryptographicengine.encrypt.aspx)
## Articles (may be helpful)
[Two Apps into One - Box](https://blogs.windows.com/buildingapps/2015/11/11/combining-two-apps-into-one-at-box-with-the-universal-windows-platform/)
