DROP PROCEDURE IF EXISTS GetRecentlyFinished;
GO

CREATE PROCEDURE GetRecentlyFinished 
    @MaxItems int
AS

select top 10 dblf.*, datefinished=cast(substring('2022-05-31T15:44:09.6709897+12:00', 0, 24) as datetime) from DbObject dblf
where dblf.Type = 'FileFlows.Shared.Models.LibraryFile' and json_value(dblf.[Data], '$.Status') = 1
order by datefinished desc

GO