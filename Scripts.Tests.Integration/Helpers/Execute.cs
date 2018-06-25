using kCura.Relativity.Client;
using Relativity.API;
using System;
using System.Collections.Generic;

namespace Scripts.Tests.Integration.Helpers
{
	public class Execute
	{
		public static List<String> GetRelativityScriptInput(IRSAPIClient client, String scriptName, Int32 workspaceArtifactID, IDBContext dbContext)
		{

			var returnval = new List<string>();
			List<RelativityScriptInputDetails> scriptInputList = null;

			int artifactid = GetScriptArtifactId(scriptName, workspaceArtifactID, client);

			// STEP 1: Using ArtifactID, set the script you want to run.
			kCura.Relativity.Client.DTOs.RelativityScript script = new kCura.Relativity.Client.DTOs.RelativityScript(artifactid);

			// STEP 2: Call GetRelativityScriptInputs.
			try
			{
				scriptInputList = client.Repositories.RelativityScript.GetRelativityScriptInputs(script);
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("An error occurred: {0}", ex.Message));
				return returnval;
			}


			// STEP 3: Each RelativityScriptInputDetails object can be used to generate a RelativityScriptInput object, 
			// but this example only displays information about each input.
			foreach (RelativityScriptInputDetails relativityScriptInputDetails in scriptInputList)
			{
				// ACB: Removed because it's only necessary for debugging
				//Console.WriteLine("Input Name: {0}\n ", //Input Id:  {1}\nInput Type: ",
				//    relativityScriptInputDetails.Name);
				////  relativityScriptInputDetails.Id);


				returnval.Add(relativityScriptInputDetails.Name);
			}
			return returnval;
		}

		public static Int32 GetScriptArtifactId(String scriptName, Int32 workspaceID, IRSAPIClient _client)
		{
			int ScriptArtifactId = 0;
			var query = new Query
			{
				ArtifactTypeID = (Int32)ArtifactType.RelativityScript,
				Condition = new TextCondition("Name", TextConditionEnum.Like, scriptName)
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
				ScriptArtifactId = result.QueryArtifacts[0].ArtifactID;
			}

			return ScriptArtifactId;
		}

	}
}
