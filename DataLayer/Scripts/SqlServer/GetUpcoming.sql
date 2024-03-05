DROP PROCEDURE IF EXISTS GetUpcoming;
GO

CREATE PROCEDURE GetUpcoming 
	@IntervalIndex int,
    @MaxItems int
AS

create table #tempLibraries (
                                Uid varchar(36),
                                Name varchar(255),
                                Priority int
)

    insert into #tempLibraries
select Uid, Name, json_value([Data], '$.Priority') as Priority from DbObject
where Type = 'FileFlows.Shared.Models.Library'
  and json_value([Data], '$.Enabled') = 1
  and substring(json_value([Data], '$.Schedule'), @IntervalIndex, 1) <> '0'

select top 10 dblf.* from DbObject dblf
                                  inner join #tempLibraries dbLib on json_value(dblf.[Data], '$.Library.Uid') = dbLib.Uid
where dblf.Type = 'FileFlows.Shared.Models.LibraryFile' and json_value(dblf.[Data], '$.Status') = 0
order by case
             when json_value(dblf.[Data], '$.Order') > 0 then json_value(dblf.[Data], '$.Order')
             else 10000 * -dbLib.Priority
             end asc

GO