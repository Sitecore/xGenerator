CREATE PROCEDURE [dbo].[sp_sc_Refresh_Analytics]
	@lastUpdate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @today DATETIME = GETDATE()

	DECLARE @dayspan INT = DATEDIFF(DAY, @lastUpdate, @today)

	DECLARE @table_name nvarchar(80)
	DECLARE @sql NVARCHAR(4000)
	DECLARE @exist INT
	DECLARE @retval INT   
	DECLARE @ParmDefinition nvarchar(500);

	DECLARE @daysToAdd nvarchar(10);
	SELECT @daysToAdd = CONVERT(nvarchar(10), @dayspan)  

	DECLARE @d datetime
	DECLARE @siteNameId int
	DECLARE @itemId uniqueidentifier
	DECLARE @languageId int
	DECLARE @devicenameid int
	DECLARE @duplicate int
	DECLARE @views bigint
	DECLARE @visits bigint
	DECLARE @duration bigint
	DECLARE @value bigint
	
	DECLARE fact_pvbylanguage CURSOR FOR
	SELECT date, SiteNameId, ItemId, LanguageId, DeviceNameId,Views, Visits, Duration, Value from dbo.Fact_PageViewsByLanguage where date <= @lastUpdate


	OPEN fact_pvbylanguage
	FETCH NEXT FROM fact_pvbylanguage
	INTO @d, @siteNameId, @itemId, @languageId, @devicenameid, @views, @visits, @duration, @value

	WHILE @@FETCH_STATUS =0
	BEGIN

		UPDATE Fact_PageViewsByLanguage set Views = Views + @views, Visits = Visits + @visits, Duration = Duration +@duration, Value = Value + @value
		WHERE Date = DATEADD(day, @dayspan, @d) AND SiteNameId = @siteNameId AND ItemId = @itemId AND LanguageId = @languageId AND DeviceNameId = @devicenameid AND Date > @lastUpdate

		DELETE dbo.Fact_PageViewsByLanguage WHERE CURRENT OF fact_pvbylanguage 

		FETCH NEXT FROM fact_pvbylanguage
		INTO @d, @siteNameId, @itemId, @languageId, @devicenameid, @views, @visits, @duration, @value
	END

	CLOSE fact_pvbylanguage
	DEALLOCATE fact_pvbylanguage

	--Select tables list that should be updated
	DECLARE tables_cursor CURSOR
		FOR SELECT name
		FROM sys.Tables
		WHERE (name LIKE 'Fact_%' OR name LIKE 'Segment%') AND name<>'Fact_PageViewsByLanguage'
	OPEN tables_cursor
	FETCH NEXT FROM tables_cursor INTO @table_name

	--Go through all tables and update dates
	WHILE @@FETCH_STATUS = 0
	BEGIN
		FETCH NEXT FROM tables_cursor INTO @table_name
		SET @sql = 'SELECT * from ' + @table_name
		
		SELECT @sql = N'SELECT @retvalOUT = count(*) FROM syscolumns WHERE name=''Date'' AND id=OBJECT_ID(''' + @table_name + ''')'
		SET @ParmDefinition = N'@retvalOUT int OUTPUT';
		EXEC sp_executesql @sql, @ParmDefinition, @retvalOUT=@exist OUTPUT;
		
		if(@exist = 1)
		BEGIN
			select @sql = 'Delete FROM ' + @table_name + ' WHERE Date > '''+CONVERT(varchar(10), @lastUpdate, 20)+''' AND Date <= ''' + CONVERT(varchar(10), @today, 20) + ''' 
			AND (Select count(1) from '+ @table_name + ' WHERE DATEADD(day,-'+@daysToAdd+', Date) <='''+CONVERT(varchar(10), @lastUpdate, 20)+''')>0'
			EXEC sp_executesql @sql

			SELECT @sql = 'UPDATE ' + @table_name + ' SET Date = DATEADD(day,'+@daysToAdd+', Date) WHERE Date <='''+CONVERT(varchar(10), @lastUpdate, 20)+''''
			EXEC sp_executesql @sql
		END
	END	
	
	UPDATE Trees SET StartDate = DATEADD(day,@dayspan, StartDate), EndDate = DATEADD(day,@dayspan, EndDate)
END


