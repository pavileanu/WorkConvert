USE [iQuote2_UAT]
GO
/****** Object:  StoredProcedure [dbo].[sp_AddNewPQWSServiceLevel]    Script Date: 05/10/2015 12:29:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[sp_AddNewPQWSServiceLevel]
(

 @serviceLevel  int,
 @mfrCode		nchar(3)

)
as 
begin

	set nocount on

	if not exists (select * from NewServiceLevel where MfrCode = @mfrCode and ServiceLevel = @serviceLevel)
	begin

		insert into NewServiceLevel (ServiceLevel, MfrCode) values (@serviceLevel, @mfrCode)

	end

end
