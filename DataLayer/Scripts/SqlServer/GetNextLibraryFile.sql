DROP PROCEDURE IF EXISTS GetNextLibraryFile;
GO

CREATE PROCEDURE GetNextLibraryFile
    @NodeUid varchar(36), 
	@WorkerUid varchar(36), 
	@IntervalIndex int, 
	@StartDate varchar(50)
AS

declare @nodeName as varchar(255);
	declare @tData varchar(max);
	declare @tUid varchar(36);
    declare @allLibraries int;
    declare @jsonLibraries varchar(max);
    
    set @nodeName = '';
    set @tData = '';
    set @tUid = '';
    set @allLibraries = 0;
    set @jsonLibraries = '';

    -- get node name, only if in schedule    	
select top 1 @nodeName=[Name] from DbObject
where Uid = @NodeUid and Type = 'FileFlows.Shared.Models.ProcessingNode'
  and json_value([Data], '$.Enabled') = 1
  and json_value([Data], '$.Schedule') = '' or substring(json_value([Data], '$.Schedule'), @IntervalIndex, 1) = '1'

    if @nodeName is NULL OR @nodeName = ''
begin
        return
end
        
    -- get supported libraries 
drop table if exists #tempLibraries;
select @allLibraries=json_value([Data], '$.AllLibraries') from DbObject where Uid = @NodeUid
declare @libraries as varchar(max)
select @libraries=json_query([Data], '$.Libraries') from DbObject where Uid = @NodeUid
select @jsonLibraries=STRING_AGG([Uid],',') from OPENJSON(@libraries)
    WITH (
    Name	VARCHAR(200)	'$.Name',
    Uid     varchar(36)		'$.Uid',
    Type	varchar(50)		'$.Type'
    )

create table #tempLibraries (
                                Uid varchar(36),
                                Name varchar(255),
                                Priority int
)

    insert into #tempLibraries
select Uid, Name, json_value([Data], '$.Priority') as Priority from DbObject
where Type = 'FileFlows.Shared.Models.Library'
  and json_value([Data], '$.Enabled') = 1
  and (json_value([Data], '$.Schedule') = '' or substring(json_value([Data], '$.Schedule'), @IntervalIndex, 1) = '1')
  and (@allLibraries = 1 OR (@allLibraries = 0 and charindex(cast(Uid as varchar(36)), @jsonLibraries) > 0)  OR (@allLibraries = 2 and charindex(cast(Uid as varchar(36)), @jsonLibraries) < 1))

    set @tUid = '';
set @tData = '';

select top 1 @tUid=DbObject.Uid
from DbObject inner join #tempLibraries
                         on json_value(DbObject.Data, '$.Library.Uid') = #tempLibraries.Uid
where Type = 'FileFlows.Shared.Models.LibraryFile'
  and json_value(DbObject.Data, '$.Status') = 0
order by
    case
        when json_value(DbObject.Data, '$.Order') > 0 then json_value([Data], '$.Order')
        else 10000 * -#tempLibraries.Priority
        end asc,
    #tempLibraries.Priority desc,
    DbObject.DateCreated asc


drop table if exists #tempLibraries;

if @tUid is NULL OR @tUid = ''
begin
            return
end
        
	set @tData = (select Data from DbObject where Uid = @tUid);
    
	SET @tData=JSON_MODIFY(@tData,'$.Status',2)
	SET @tData=JSON_MODIFY(@tData,'$.ProcessingStarted',@StartDate)
	SET @tData=JSON_MODIFY(@tData,'$.WorkerUid',@WorkerUid)
	SET @tData=JSON_MODIFY(@tData,'$.Node.NodeUid', @NodeUid)
	SET @tData=JSON_MODIFY(@tData,'$.Node.Name', @nodeName)

update DbObject set Data = @tData where Uid = @tUid;

select * from DbObject where Uid = @tUid;

GO