
ALTER TABLE dbo.ProductValidations ADD
	ValidationMessageType varchar(20) NULL
GO
ALTER TABLE dbo.ProductValidations ADD CONSTRAINT
	DF_ProductValidations_ValidationMessageType DEFAULT 'Validation' FOR ValidationMessageType
GO
UPDATE ProductValidations set ValidationMessageType='Validation' where ValidationMessageType is null