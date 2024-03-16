// using FileFlows.ServerShared.Workers;
//
// namespace FileFlows.Server.Workers;
//
// /// <summary>
// /// Workers that refreshes the cached data in the library file service
// /// </summary>
// public class LibraryFileServiceUpdater:Worker
// {
//     /// <summary>
//     /// Creates a new instance of the LibraryFileService Updater 
//     /// </summary>
//     public LibraryFileServiceUpdater() : base(ScheduleType.Hourly, 3)
//     {
//     }
//
//     /// <summary>
//     /// Executes the worker
//     /// </summary>
//     protected override void Execute()
//         =>  Services.LibraryFileService.Refresh();
// }