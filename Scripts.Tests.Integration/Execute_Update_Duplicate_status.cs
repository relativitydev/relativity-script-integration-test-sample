using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using NUnit.Framework;
using Relativity.API;
using Relativity.Test.Helpers;
using Relativity.Test.Helpers.ServiceFactory.Extentions;
using Relativity.Test.Helpers.SharedTestHelpers;
using Relativity.Test.Helpers.WorkspaceHelpers;
using Scripts.Tests.Integration.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scripts.Tests.Integration
{
	[TestFixture]
	public class Execute_Update_Duplicate_Status
	{
		/// <summary>
		/// For the Import Object and Import native to work, replace the oi folder with the relativity specific oi folder files
		/// </summary>

		#region Variables

		private const String _SCRIPT_NAME = "Update Duplicate Status";
		private const String _PARENT_OBJECT_ID_SOURCE_FIELD_NAME = "Folder Name";
		private const String _CONTROL_NUMBER_FIELD_NAME = "Control Number";
		private const String _MD5_HASH_FIELD_NAME = "MD5 Hash";
		private const String _MD5_HASH_FIELD_COLUMN_NAME = "MD5Hash";
		private const String _DUPLICATE_STATUS_1_FIELD_NAME = "Duplicate Status 1";
		private const String _CUSTODIAN_FIELD_NAME = "Custodian";
		private const String _FULL_NAME_FIELD_NAME = "Full Name";
		private const String _FIRST_NAME_FIELD_NAME = "First Name";
		private const String _LAST_NAME_FIELD_NAME = "Last Name";
		private const String _SORT_ORDER_FIELD_NAME = "Sort Order";
		private const String _SORT_ORDER_FIELD_COLUMN_NAME = "SortOrder";
		private const String _MASTER_CHOICE_NAME = "Master";
		private const String _DUPLICATE_CHOICE_NAME = "Duplicate";
		private const String _UNIQUE_CHOICE_NAME = "Unique";
		private const String _SAVED_SEARCH_NAME = "All Documents";
		private const String _SAVED_SEARCH_NAME_ONE_GROUP = "MD5 Hash = 111111";
		private readonly Guid _strIdentifierFieldGuid = new Guid("57928EF5-F29D-4137-A215-3A9ABF3E3F82");
		private readonly Guid _duplicateStatusFieldGuid = new Guid("4240E1CE-E9E3-4EDD-B004-A4A2609DFE6D");
		private IRSAPIClient _client;
		private int _workspaceId;
		private Int32 _rootFolderArtifactId;
		private int _rootFolderArtifactID;
		private readonly string _workspaceName = "IntTest_" + Guid.NewGuid();
		private const ExecutionIdentity EXECUTION_IDENTITY = ExecutionIdentity.CurrentUser;
		private IDBContext dbContext;
		private IServicesMgr servicesManager;
		private IDBContext _eddsDbContext;
		//todo: Update location of the setup application file
		public String FilepathData = @"S:\SourceCode\Github\Scripts.Tests.Integration\RA_Update_Duplicate_Status_Setup.rap";

		#endregion

		#region Setup and Teardown

		[TestFixtureSetUp]
		public void Execute_TestFixtureSetup()
		{
			//Setup for testing
			//Setup for testing		
			TestHelper helper = new TestHelper();
			servicesManager = helper.GetServicesManager();

			// implement_IHelper
			//create client
			_client = helper.GetServicesManager().GetProxy<IRSAPIClient>(ConfigurationHelper.ADMIN_USERNAME, ConfigurationHelper.DEFAULT_PASSWORD);
			_eddsDbContext = helper.GetDBContext(-1);

			//Create workspace
			_workspaceId = CreateWorkspace.CreateWorkspaceAsync(_workspaceName, ConfigurationHelper.TEST_WORKSPACE_TEMPLATE_NAME, servicesManager, ConfigurationHelper.ADMIN_USERNAME, ConfigurationHelper.DEFAULT_PASSWORD).Result;
			dbContext = helper.GetDBContext(_workspaceId);
			_client.APIOptions.WorkspaceID = _workspaceId;

			_rootFolderArtifactId = APIHelpers.GetRootFolderArtifactID(_client, _workspaceName);

			//Import Application containing script, fields, and choices  
			Relativity.Test.Helpers.Application.ApplicationHelpers.ImportApplication(_client, _workspaceId, true, FilepathData);

			//Import custodians
			var custodians = GetCustodiansDatatable();

			var identityFieldArtifactId = GetArtifactIDOfCustodianField("Full Name", _workspaceId, _client);
			ImportAPIHelpers.ImportObjects(_workspaceId, "Custodian", identityFieldArtifactId, custodians, String.Empty);

			//Import Documents
			var documents = GetDocumentDataTable(_rootFolderArtifactId, _workspaceId);
			ImportAPIHelpers.CreateDocuments(_workspaceId, _rootFolderArtifactId, documents);
		}

		[TestFixtureTearDown]
		public void Execute_TestFixtureTeardown()
		{
			_client.APIOptions.WorkspaceID = -1;
			Relativity.Test.Helpers.WorkspaceHelpers.DeleteWorkspace.Delete(_client, _workspaceId);
		}

		[SetUp]
		public void Execute_TestSetup()
		{
			//Reset all documents in the duplicate status field
			Reset_Duplicate_Status_Field();
		}

		#endregion

		#region Golden Flow

		[Test, Description("Golden Flow")]
		public void Update_Duplicate_StatusGolden_Flow()
		{
			//Act
			var scriptResults = ExecuteScript_Update_Duplicate_status(_SCRIPT_NAME, _workspaceId, _SAVED_SEARCH_NAME, _duplicateStatusFieldGuid, _MD5_HASH_FIELD_COLUMN_NAME, _SORT_ORDER_FIELD_COLUMN_NAME);

			//Assert
			Assert.AreEqual("Update Complete", scriptResults.Artifacts[0].Fields[0].Value.ToString());
			Assert.AreEqual(scriptResults.Success, true);
		}


		[Test, Description("Golden Flow")]
		public void Update_Duplicate_StatusGolden_flow_without_Sort()
		{
			//Act
			var scriptResults = ExecuteScript_Update_Duplicate_status(_SCRIPT_NAME, _workspaceId, _SAVED_SEARCH_NAME, _duplicateStatusFieldGuid, _MD5_HASH_FIELD_COLUMN_NAME, "");

			//Assert
			Assert.AreEqual("Update Complete", scriptResults.Artifacts[0].Fields[0].Value.ToString());
			Assert.AreEqual(scriptResults.Success, true);
		}

		[TestCase("AS000006", _SORT_ORDER_FIELD_COLUMN_NAME, _UNIQUE_CHOICE_NAME, _SAVED_SEARCH_NAME)]
		[TestCase("AS000001;AS000008", "", _MASTER_CHOICE_NAME, _SAVED_SEARCH_NAME)]
		[TestCase("AS000002;AS000003;AS000004;AS000005;AS000009;AS000010;AS000011", "", _DUPLICATE_CHOICE_NAME, _SAVED_SEARCH_NAME)]
		[TestCase("AS000001;AS000010", _SORT_ORDER_FIELD_COLUMN_NAME, _MASTER_CHOICE_NAME, _SAVED_SEARCH_NAME)]
		[TestCase("AS000002;AS000003;AS000004;AS000005;AS000008;AS000009;AS000011", _SORT_ORDER_FIELD_COLUMN_NAME, _DUPLICATE_CHOICE_NAME, _SAVED_SEARCH_NAME)]
		[TestCase("AS000001", "", _MASTER_CHOICE_NAME, _SAVED_SEARCH_NAME_ONE_GROUP)]
		[TestCase("AS000002;AS000003;AS000004;AS000005", "", _DUPLICATE_CHOICE_NAME, _SAVED_SEARCH_NAME_ONE_GROUP)]
		[Test, Description("Ensure that documents are set to the correct duplicate status value.")]
		public void Verify_Duplicate_Status_Field(String documentIdentifiersList, String sortOrderFieldInput, String expectedDuplicateStatus, String savedSearchName)
		{
			//Arrange
			char[] delimiterChars = { ';' };
			var documentIdentifiers = documentIdentifiersList.Split(delimiterChars);

			//Act
			var results = Execute_Script_And_Query_Documents(documentIdentifiers, sortOrderFieldInput, savedSearchName);

			//Assert
			foreach (var result in results.Results)
			{
				Assert.AreEqual(expectedDuplicateStatus, result.Artifact.Fields[0].ValueAsSingleChoice.Name);
			}
		}

		[TestCase("AS000006;AS000007;AS000008;AS000009;AS000010;AS000011", "", _SAVED_SEARCH_NAME_ONE_GROUP)]
		[Test, Description("Ensure that documents outside the saved search are not set in the duplicate status field.")]
		public void Verify_Duplicate_Status_Field_Saved_Search(String documentIdentifiersList, String sortOrderFieldInput, String savedSearchName)
		{
			//Arrange

			char[] delimiterChars = { ';' };
			var documentIdentifiers = documentIdentifiersList.Split(delimiterChars);

			//Act
			var results = Execute_Script_And_Query_Documents(documentIdentifiers, sortOrderFieldInput, savedSearchName);

			//Assert
			foreach (var result in results.Results)
			{
				Assert.IsNull(result.Artifact.Fields[0].ValueAsSingleChoice);
			}
		}

		[Test, Description("Ensure that documents with no relational value are not set in the duplicate status field.")]
		public void Duplicate_Status_Not_Set()
		{
			//Arrange
			var documentIdentifiers = new[] { "AS000007" };

			//Act
			var results = Execute_Script_And_Query_Documents(documentIdentifiers, _SORT_ORDER_FIELD_COLUMN_NAME, _SAVED_SEARCH_NAME);

			//Assert
			foreach (var result in results.Results)
			{
				Assert.IsNull(result.Artifact.Fields[0].ValueAsSingleChoice);
			}
		}

		[Test, Description("Ensure that documents outside the selected saved search do not lose their duplicate status field value.")]
		public void Duplicate_Status_Does_Not_Overwrite()
		{
			//Arrange
			// Execute golden flow to populate all documents
			ExecuteScript_Update_Duplicate_status(_SCRIPT_NAME, _workspaceId, _SAVED_SEARCH_NAME, _duplicateStatusFieldGuid, _MD5_HASH_FIELD_COLUMN_NAME, _SORT_ORDER_FIELD_COLUMN_NAME);

			// document that's in the 'All Documents' but not 'MD5 Hash = 11111' Saved Search
			var documentIdentifiers = new[] { "AS000010" };

			//Act
			var results = Execute_Script_And_Query_Documents(documentIdentifiers, _SORT_ORDER_FIELD_COLUMN_NAME, _SAVED_SEARCH_NAME_ONE_GROUP);

			//Assert
			Assert.IsTrue(results.Success, "Should return results");
			Assert.Greater(results.TotalCount, 0, "Should be at least one result");
			Assert.IsNotNull(results.Results.First().Artifact.Fields.Get(_duplicateStatusFieldGuid).ValueAsSingleChoice, "Should not have cleared the duplicate status of a document outside the saved search");
		}

		#endregion

		#region Helper for execute Relativity script

		public RelativityScriptResult ExecuteScript_Update_Duplicate_status(String scriptName, Int32 workspaceArtifactId, String savedSearchName, Guid duplicateStatusFieldGuid,
			String relationalFieldColumnName, String sortOrderFieldColumnName)
		{
			//Retrieve script by name
			Query<RelativityScript> relScriptQuery = new Query<RelativityScript>
			{
				Condition = new TextCondition(RelativityScriptFieldNames.Name, TextConditionEnum.EqualTo, scriptName),
				Fields = FieldValue.AllFields
			};

			QueryResultSet<RelativityScript> relScriptQueryResults = _client.Repositories.RelativityScript.Query(relScriptQuery);
			if (!relScriptQueryResults.Success)
			{
				throw new Exception(String.Format("An error occurred finding the script: {0}", relScriptQueryResults.Message));
			}

			if (!relScriptQueryResults.Results.Any())
			{
				throw new Exception(String.Format("No results returned: {0}", relScriptQueryResults.Message));
			}

			//Retrieve script inputs
			var script = relScriptQueryResults.Results[0].Artifact;
			var inputnames = Execute.GetRelativityScriptInput(_client, scriptName, workspaceArtifactId, dbContext);
			int savedsearchartifactid = APIHelpers.Query_For_Saved_SearchID(savedSearchName, _client);
			Int32 duplicateStatusFieldCodeTypeId = GetFieldCodeTypeId(_client, workspaceArtifactId);

			//Set inputs for script
			RelativityScriptInput input = new RelativityScriptInput(inputnames[0], savedsearchartifactid.ToString());
			RelativityScriptInput input2 = new RelativityScriptInput(inputnames[1], duplicateStatusFieldCodeTypeId.ToString());
			RelativityScriptInput input3 = new RelativityScriptInput(inputnames[2], relationalFieldColumnName);
			RelativityScriptInput input4 = new RelativityScriptInput(inputnames[3], sortOrderFieldColumnName);

			//Execute the script
			List<RelativityScriptInput> inputList = new List<RelativityScriptInput> { input, input2, input3, input4 };
			RelativityScriptResult scriptResult = _client.Repositories.RelativityScript.ExecuteRelativityScript(script, inputList);

			return scriptResult;
		}

		private QueryResultSet<Document> Execute_Script_And_Query_Documents(String[] documentIdentifiers, String sortOrderColumnName, String savedSearchName)
		{
			//Execute the script
			ExecuteScript_Update_Duplicate_status(_SCRIPT_NAME, _workspaceId, savedSearchName, _duplicateStatusFieldGuid, _MD5_HASH_FIELD_COLUMN_NAME, sortOrderColumnName);

			//Get the duplicate status field for documents after the script is executed
			var identifierFieldArtifactId = GetFieldArtifactID(_CONTROL_NUMBER_FIELD_NAME, _workspaceId, _client);
			var query = new Query<Document>
			{
				Condition = new TextCondition(identifierFieldArtifactId, TextConditionEnum.In, documentIdentifiers)
			};
			query.Fields.Add(new FieldValue(_DUPLICATE_STATUS_1_FIELD_NAME));

			return _client.Repositories.Document.Query(query);
		}

		private void Reset_Duplicate_Status_Field()
		{
			var query = new Query<Document>();
			var documents = _client.Repositories.Document.Query(query);
			var documentsToUpdate = new List<Document>();

			foreach (var document in documents.Results)
			{
				var docToUpdate = new Document(document.Artifact.ArtifactID);
				docToUpdate.Fields.Add(new FieldValue(_duplicateStatusFieldGuid));
				documentsToUpdate.Add(docToUpdate);
			}

			_client.Repositories.Document.Update(documentsToUpdate);
		}

		#endregion

		#region Helpers

		private DataTable GetDocumentDataTable(int folderArtifactId, Int32 workspaceId)
		{
			var table = new DataTable();
			var folderName = GetFolderName(folderArtifactId, workspaceId, _client);

			table.Columns.Add(_CONTROL_NUMBER_FIELD_NAME, typeof(string));
			table.Columns.Add(_MD5_HASH_FIELD_NAME, typeof(string));
			table.Columns.Add(_DUPLICATE_STATUS_1_FIELD_NAME, typeof(string));
			table.Columns.Add(_CUSTODIAN_FIELD_NAME, typeof(string));
			table.Columns.Add(_PARENT_OBJECT_ID_SOURCE_FIELD_NAME, typeof(string));
			table.Columns.Add("Native File", typeof(string));

			//todo: Update location of Sample Text file
			var nativeName = @"S:\SourceCode\Github\Scripts.Tests.Integration\Scripts.Tests.Integration\Natives\SampleTextFile.txt";


			table.Rows.Add("AS000001", "111111", "", "", folderName, nativeName);
			table.Rows.Add("AS000002", "111111", "", "", folderName, nativeName);
			table.Rows.Add("AS000003", "111111", "", "", folderName, nativeName);
			table.Rows.Add("AS000004", "111111", "", "", folderName, nativeName);
			table.Rows.Add("AS000005", "111111", "", "", folderName, nativeName);
			table.Rows.Add("AS000006", "22222", "", "", folderName, nativeName);
			table.Rows.Add("AS000007", "", "", "", folderName, nativeName);
			table.Rows.Add("AS000008", "33333", "", "doe, Kayla", folderName, nativeName);
			table.Rows.Add("AS000009", "33333", "", "doe, Kayla", folderName, nativeName);
			table.Rows.Add("AS000010", "33333", "", "Doe, Jane", folderName, nativeName);
			table.Rows.Add("AS000011", "33333", "", "Smith, John", folderName, nativeName);

			return table;
		}

		private DataTable GetCustodiansDatatable()
		{
			var table = new DataTable();

			table.Columns.Add(_FULL_NAME_FIELD_NAME, typeof(string));
			table.Columns.Add(_FIRST_NAME_FIELD_NAME, typeof(string));
			table.Columns.Add(_LAST_NAME_FIELD_NAME, typeof(string));
			table.Columns.Add(_SORT_ORDER_FIELD_NAME, typeof(string));

			table.Rows.Add("Doe, Jane", "Jane", "Doe", "1");
			table.Rows.Add("Smith, John", "John", "Smith", "2");
			table.Rows.Add("doe, Kayla", "Kayla", "doe", "3");

			return table;
		}


		public static Int32 GetArtifactIDOfCustodianField(String fieldname, Int32 workspaceID, IRSAPIClient _client)
		{
			int fieldArtifactId = 0;
			var query = new Query
			{
				ArtifactTypeID = (Int32)ArtifactType.Field,
				Condition = new TextCondition("Name", TextConditionEnum.Like, fieldname)
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
				fieldArtifactId = result.QueryArtifacts[0].ArtifactID;
			}

			return fieldArtifactId;
		}


		public static Int32 GetFieldCodeTypeId(IRSAPIClient proxy, Int32 workspaceID)
		{
			proxy.APIOptions.WorkspaceID = workspaceID;

			// STEP 1: Call the Read() method on the Field repository, passing a Field DTO.
			ResultSet<kCura.Relativity.Client.DTOs.Field> results;

			//todo: Get the duplicate status field artifact id dynamically
			results = proxy.Repositories.Field.Read(new kCura.Relativity.Client.DTOs.Field(1038856) { Fields = FieldValue.AllFields });

			// STEP 2: Get the Field artifact from the read results.
			kCura.Relativity.Client.DTOs.Field fieldArtifact = results.Results.FirstOrDefault().Artifact;

			Console.WriteLine("Field Name: " + fieldArtifact.Name);
			Console.WriteLine("Field Type: " + fieldArtifact.FieldTypeID);
			Console.WriteLine("Object Type: " + fieldArtifact.ObjectType.DescriptorArtifactTypeID);
			Int32 CodetypeId = Convert.ToInt32(fieldArtifact.ChoiceTypeID);
			return CodetypeId;
		}


		public static Int32 GetFieldArtifactID(String fieldname, Int32 workspaceID, IRSAPIClient _client)
		{
			int fieldArtifactId = 0;
			var query = new Query
			{
				ArtifactTypeID = (Int32)ArtifactType.Field,
				Condition = new TextCondition("Name", TextConditionEnum.Like, fieldname)
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
				fieldArtifactId = result.QueryArtifacts[0].ArtifactID;
			}

			return fieldArtifactId;
		}

		public static String GetFolderName(Int32 folderArtifactID, Int32 workspaceID, IRSAPIClient _client)
		{
			Folder requestArtifact = new Folder(folderArtifactID);
			requestArtifact.Fields.Add(new FieldValue(FolderFieldNames.Name));

			ResultSet<Folder> readResult1 = null;

			try
			{
				readResult1 = _client.Repositories.Folder.Read(requestArtifact);
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("An error occurred while reading the folder: {0}", ex.Message));
			}

			Folder readArtifact = readResult1.Results.FirstOrDefault().Artifact;

			return readArtifact.Name;

		}


		#endregion
	}
}
