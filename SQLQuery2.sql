DECLARE @Email nvarchar(256) = N'test.user1@mail.com';

DECLARE @UserId nvarchar(450) = (
    SELECT Id
    FROM dbo.AspNetUsers
    WHERE NormalizedUserName = UPPER(@Email)
);

IF @UserId IS NULL
    THROW 50001, 'Test account not found in this database.', 1;

UPDATE dbo.AspNetUserClaims
SET ClaimValue = N'true'
WHERE UserId = @UserId AND ClaimType = N'HasForumAccess';

IF @@ROWCOUNT = 0
    INSERT INTO dbo.AspNetUserClaims (UserId, ClaimType, ClaimValue)
    VALUES (@UserId, N'HasForumAccess', N'true');