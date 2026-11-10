DECLARE @Email nvarchar(256) = N'test.user1@mail.com';
DECLARE @Hours nvarchar(20) = N'100';

DECLARE @UserId nvarchar(450) = (
    SELECT Id
    FROM dbo.AspNetUsers
    WHERE NormalizedUserName = UPPER(@Email)
);

IF @UserId IS NULL
    THROW 50001, 'Test account not found in this database.', 1;

UPDATE dbo.AspNetUserClaims
SET ClaimValue = @Hours
WHERE UserId = @UserId AND ClaimType = N'WorkingHours';

IF @@ROWCOUNT = 0
    INSERT INTO dbo.AspNetUserClaims (UserId, ClaimType, ClaimValue)
    VALUES (@UserId, N'WorkingHours', @Hours);