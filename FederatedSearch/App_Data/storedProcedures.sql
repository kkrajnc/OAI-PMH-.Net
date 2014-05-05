/*     This file is part of OAI-PMH .Net.
*  
*      OAI-PMH .Net is free software: you can redistribute it and/or modify
*      it under the terms of the GNU General Public License as published by
*      the Free Software Foundation, either version 3 of the License, or
*      (at your option) any later version.
*  
*      OAI-PMH .Net is distributed in the hope that it will be useful,
*      but WITHOUT ANY WARRANTY; without even the implied warranty of
*      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*      GNU General Public License for more details.
*  
*      You should have received a copy of the GNU General Public License
*      along with OAI-PMH .Net.  If not, see <http://www.gnu.org/licenses/>.
*----------------------------------------------------------------------------*/

USE [OaiPmhDb]
GO

/****** Object: SqlProcedure [dbo].[DeDuplicateSkip] Script Date: 30.4.2014 17:01:14 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[DeDuplicateSkip]
	@idList RecIdList READONLY,
	@objectType TINYINT,
	@metadataType TINYINT,
	@provenanceNum TINYINT
AS
	
	SELECT h.OAI_Identifier
	FROM [dbo].Header h, @idList l
	WHERE h.OAI_Identifier = l.Item

	UNION ALL

	SELECT		m.Identifier
	FROM		[dbo].Header h INNER JOIN
				[dbo].ObjectMetadata o ON h.HeaderId = o.ObjectId INNER JOIN
				[dbo].Metadata m ON o.MetadataId = m.MetadataId INNER JOIN
				@idList l ON m.Identifier = l.Item
	WHERE		o.ObjectType = @objectType
	AND			o.MetadataType = @metadataType
	AND			(m.MdFormat & @provenanceNum) != 0


RETURN 0


GO

/****** Object: SqlProcedure [dbo].[DeleteMetadata] Script Date: 30.4.2014 17:02:18 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[DeleteMetadata]
	@dataProviderId INT,
	@fullDelete BIT
AS
	BEGIN TRAN DeleteMetadata
		BEGIN TRY
			/* delete metadata */
			DELETE M FROM [dbo].[Metadata] M
			INNER JOIN [dbo].[ObjectMetadata] O ON O.MetadataId = M.MetadataId
			INNER JOIN [dbo].[Header] H ON H.HeaderId = O.ObjectId
			WHERE H.OAIDataProviderId = @dataProviderId

			/* delete linktable */
			DELETE O FROM [dbo].[ObjectMetadata] O
			INNER JOIN [dbo].[Header] H ON H.HeaderId = O.ObjectId
			WHERE H.OAIDataProviderId = @dataProviderId

			IF @fullDelete = 1
				BEGIN
					DELETE FROM [dbo].[Header]
					WHERE [dbo].[Header].[OAIDataProviderId] = @dataProviderId
				END
			ELSE
				BEGIN
					UPDATE [dbo].[Header]
					SET [dbo].[Header].[Deleted] = 1
					WHERE [dbo].[Header].[OAIDataProviderId] = @dataProviderId
				END

		END TRY
		BEGIN CATCH
			ROLLBACK TRAN DeleteMetadata
			RETURN 0
		END CATCH

		COMMIT TRAN DeleteMetadata
		RETURN 1


GO

/****** Object: SqlProcedure [dbo].[MetaFts] Script Date: 30.4.2014 17:03:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[MetaFts]
	@searchStr nvarchar(4000),
	@objectType tinyint,
	@metaType tinyint,
	@metaFormat tinyint,
	@skip int,
	@take int,
	@resultCount int OUTPUT
AS
	SET NOCOUNT ON;

	WITH TempResult AS(
		SELECT		[dbo].Header.HeaderId, 
					[dbo].Header.OAI_Identifier, 
					[dbo].Header.Datestamp, 
					[dbo].Metadata.Title, 
					[dbo].Metadata.Creator, 
					[dbo].Metadata.MetadataId, 
					[dbo].Metadata.Subject, 
					[dbo].Metadata.Date,
					KeyTable.RANK
		FROM		[dbo].Header INNER JOIN
					[dbo].ObjectMetadata ON [dbo].Header.HeaderId = [dbo].ObjectMetadata.ObjectId INNER JOIN
					[dbo].Metadata ON [dbo].ObjectMetadata.MetadataId = [dbo].Metadata.MetadataId INNER JOIN
					CONTAINSTABLE([dbo].Metadata, *, @searchStr) AS KeyTable ON [dbo].Metadata.MetadataId = KeyTable.[KEY]
		WHERE		[dbo].ObjectMetadata.ObjectType = @objectType
		AND			[dbo].ObjectMetadata.MetadataType = @metaType
		AND			([dbo].Metadata.MdFormat & @metaFormat) != 0
	), TempCount AS (
		SELECT COUNT(*) AS HeaderId, NULL AS OAI_Identifier, NULL AS Datestamp, NULL AS Title, NULL AS Creator,
		0 AS MetadataId, NULL AS Subject, NULL AS Date, 0 AS RANK
		FROM TempResult
	)
	SELECT * FROM
	(SELECT *
	FROM TempResult
	ORDER BY TempResult.RANK
    OFFSET (@skip) ROWS
    FETCH NEXT (@take) ROWS ONLY) AS TempSelect
	UNION ALL
	SELECT * FROM TempCount


RETURN
