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

	--Select tables list that should be updated
	DECLARE tables_cursor CURSOR
		FOR SELECT name
		FROM sys.Tables
		WHERE name LIKE 'Fact_%' OR name LIKE 'Segment%'
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
			SELECT @sql = 'UPDATE ' + @table_name + ' SET Date = DATEADD(day,'+@daysToAdd+', Date) '
			EXEC sp_executesql @sql
		END
	END	
	
	UPDATE Trees SET StartDate = DATEADD(day,@dayspan, StartDate), EndDate = DATEADD(day,@dayspan, EndDate)
END


