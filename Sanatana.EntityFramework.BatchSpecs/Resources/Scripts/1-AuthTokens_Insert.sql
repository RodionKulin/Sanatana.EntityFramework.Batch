CREATE PROCEDURE [dbo].[AuthTokens_Insert]
	@accountUid UNIQUEIDENTIFIER,
	@token NVARCHAR(100)
AS

DELETE AuthTokens
WHERE Token = @token

MERGE AuthTokens AS target
USING (SELECT @accountUid, @token) AS source (AccountUid, Token)
ON (target.AccountUID = source.AccountUid)
WHEN MATCHED THEN 
    UPDATE SET Token = source.Token,
	CreateDateUtc = GETUTCDATE()
WHEN NOT MATCHED THEN	
	INSERT (AccountUid, Token, CreateDateUtc)
	VALUES (source.AccountUid, source.Token, GETUTCDATE());