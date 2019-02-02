CREATE TYPE [dbo].[ExceptionHierarchyType] AS TABLE(
	[ExceptionHierarchyID] [uniqueidentifier] NULL,
	[HierarchyOrder] [int] NULL,
	[ExceptionMessage] [nvarchar](max) NULL,
	[StackTrace] [nvarchar](max) NULL
)