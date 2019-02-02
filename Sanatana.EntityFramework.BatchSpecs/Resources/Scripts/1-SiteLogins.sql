CREATE TABLE [dbo].[SiteLogins] (
    [UID]                              UNIQUEIDENTIFIER NOT NULL,
    [Email]                            NVARCHAR (256)   NOT NULL,
    [Password]                         NVARCHAR (128)   NOT NULL,
    [DisplayName]                      NVARCHAR (64)    NOT NULL,
    [IsEmailVerified]                  BIT              CONSTRAINT [DF_SiteLogins_IsEmailVerified] DEFAULT ((0)) NOT NULL,
    [CreatedTime]                      DATETIME         CONSTRAINT [DF_SiteLogins_CreatedTime] DEFAULT (getdate()) NOT NULL,
    [LastLoginTime]                    DATETIME         CONSTRAINT [DF_SiteLogins_LastLoginTime] DEFAULT (getdate()) NOT NULL,
    [LastPasswordChangedDate]          DATETIME         CONSTRAINT [DF_SiteLogins_LastPasswordChangedDate] DEFAULT (getdate()) NOT NULL,
    [IsLockedOut]                      BIT              CONSTRAINT [DF_SiteLogins_IsLockedOut] DEFAULT ((0)) NOT NULL,
    [FailedPasswordAttemptCount]       INT              CONSTRAINT [DF_SiteLogins_FailedPasswordAttemptCount] DEFAULT ((0)) NOT NULL,
    [LastFailedPassword]               NVARCHAR (MAX)   CONSTRAINT [DF_SiteLogins_LastFailedPassword] DEFAULT ('') NOT NULL,
    [FailedPasswordAttemptWindowStart] DATETIME         CONSTRAINT [DF_SiteLogins_FailedPasswordAttemptWindowStart] DEFAULT (getdate()) NOT NULL,
    [LastLockoutDate]                  DATETIME         CONSTRAINT [DF_SiteLogins_LastLockoutDate] DEFAULT (getdate()) NOT NULL,
    [TotalLockouts]                    INT              CONSTRAINT [DF_SiteLogins_TotalLockouts] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_dbo.SiteLogins] PRIMARY KEY CLUSTERED ([UID] ASC),
	CONSTRAINT [IX_SiteLogins_Unique_DisplayName] UNIQUE NONCLUSTERED ([DisplayName] ASC),
	CONSTRAINT [IX_SiteLogins_Unique_Email] UNIQUE NONCLUSTERED ([Email] ASC)

);

