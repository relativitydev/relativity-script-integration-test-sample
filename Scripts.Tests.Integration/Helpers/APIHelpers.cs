using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Linq;

namespace Scripts.Tests.Integration.Helpers
{
	public class APIHelpers
	{

		public static Int32 GetRootFolderArtifactID(IRSAPIClient client, string WorkspaceName)
		{
			Query<Folder> query = new Query<Folder>();
			query.Condition = new TextCondition(FolderFieldNames.Name, TextConditionEnum.EqualTo, WorkspaceName);
			query.Fields = FieldValue.AllFields;
			return client.Repositories.Folder.Query(query).Results.FirstOrDefault().Artifact.ArtifactID;
		}

		public static Int32 Query_For_Saved_SearchID(string savedSearchName, IRSAPIClient _client)
		{

			int searchArtifactId = 0;

			var query = new Query
			{
				ArtifactTypeID = (Int32)ArtifactType.Search,
				Condition = new TextCondition("Name", TextConditionEnum.Like, savedSearchName)
			};
			QueryResult result = null;

			try
			{
				result = _client.Query(_client.APIOptions, query);
			}
			catch (Exception ex)
			{
				Console.WriteLine("An error occurred: {0}", ex.Message);
			}

			if (result != null)
			{
				searchArtifactId = result.QueryArtifacts[0].ArtifactID;
			}

			return searchArtifactId;
		}
	}
}
