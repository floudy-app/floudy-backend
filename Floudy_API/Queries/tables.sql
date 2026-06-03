CREATE TABLE [Files] 
(
    [ID] BIGINT PRIMARY KEY NOT NULL,
    [Name] NVARCHAR(256) NOT NULL,
    [Type] NVARCHAR(100) NOT NULL,
    [UploadDate] DATETIME2 NOT NULL,
    [Content] VARBINARY(MAX) NOT NULL,
    [UserId] BIGINT NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users]([ID]) ON DELETE CASCADE
);

CREATE TABLE [Roles]
(
    [ID] BIGINT PRIMARY KEY NOT NULL,
    [Name] NVARCHAR(50) NOT NULL
);

CREATE TABLE [Users]
(
    [ID] BIGINT PRIMARY KEY NOT NULL,
    [Username] NVARCHAR(100) NOT NULL UNIQUE,
    [Email] NVARCHAR(256) NOT NULL UNIQUE,
    [Password] NVARCHAR(256) NOT NULL,
    [RoleId] BIGINT NOT NULL,
    [IsBlocked] BIT NOT NULL DEFAULT 0,
    FOREIGN KEY ([RoleId]) REFERENCES [Roles]([ID])
);

INSERT INTO [Roles] ([ID], [Name]) VALUES (1, 'Admin'), (2, 'User');
INSERT INTO [Users] ([ID], [Username], [Email], [Password], [RoleId], [IsBlocked]) VALUES (1, 'admin', 'prooms.email@gmail.com', 'admin', 1, 0);
