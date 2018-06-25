using kCura.Relativity.DataReaderClient;
using kCura.Relativity.ImportAPI;
using Relativity.Test.Helpers.SharedTestHelpers;
using System;
using System.Data;
using System.Linq;

namespace Scripts.Tests.Integration.Helpers
{
	public class ImportAPIHelpers
	{
		private const string PARENT_OBJECT_ID_SOURCE_FIELD_NAME = "Folder Name";
		private const string control_Number = "Control Number";
		private const int _IDENTITY_FIELD_ARTIFACT_ID = 1003667;

		public static void ImportObjects(Int32 workspaceArtifactId, String objectTypeName, Int32 identityFieldArtifactId,
			DataTable objects, String parentObjectName)
		{
			var iapi = new ImportAPI(ConfigurationHelper.ADMIN_USERNAME, ConfigurationHelper.DEFAULT_PASSWORD,
				string.Format("https://{0}/Relativitywebapi/", ConfigurationHelper.RSAPI_SERVER_ADDRESS));

			// Pass the ArtifactID of the workspace. You add your RDOs to this workspace.
			var artifactTypeList = iapi.GetUploadableArtifactTypes(workspaceArtifactId);

			// Use this code to choose type of object that you want to add.
			var desiredArtifactType = artifactTypeList.Single(at => at.Name.Equals(objectTypeName));
			var importJob = iapi.NewObjectImportJob(desiredArtifactType.ID);

			// Use this setting for Name field of the object.
			importJob.Settings.SelectedIdentifierFieldName = "Name";

			// Specifies the ArtifactID of a document identifier field, such as a control number.
			importJob.Settings.IdentityFieldId = identityFieldArtifactId;

			importJob.Settings.CaseArtifactId = workspaceArtifactId;
			importJob.Settings.OverwriteMode = OverwriteModeEnum.AppendOverlay;
			importJob.SourceData.SourceData = objects.CreateDataReader();
			importJob.Execute();
		}

		public static void CreateDocuments(Int32 workspaceID, int folderArtifactID, DataTable sourceDataTable)
		{


			var iapi = new ImportAPI(ConfigurationHelper.ADMIN_USERNAME, ConfigurationHelper.DEFAULT_PASSWORD,
				string.Format("https://{0}/Relativitywebapi/", ConfigurationHelper.RSAPI_SERVER_ADDRESS));

			var importJob = iapi.NewNativeDocumentImportJob();

			importJob.OnMessage += ImportJobOnMessage;
			importJob.OnComplete += ImportJobOnComplete;
			importJob.OnFatalException += ImportJobOnFatalException;
			importJob.Settings.CaseArtifactId = workspaceID;
			importJob.Settings.ExtractedTextFieldContainsFilePath = false;

			importJob.Settings.NativeFilePathSourceFieldName = "Native File";
			importJob.Settings.NativeFileCopyMode = NativeFileCopyModeEnum.CopyFiles;
			importJob.Settings.OverwriteMode = OverwriteModeEnum.Append;

			importJob.Settings.IdentityFieldId = _IDENTITY_FIELD_ARTIFACT_ID;

			var documentDataTable = sourceDataTable;
			importJob.SourceData.SourceData = documentDataTable.CreateDataReader();
			importJob.Execute();
		}

		static void ImportJobOnMessage(Status status)
		{
			Console.WriteLine("Message: {0}", status.Message);
		}

		static void ImportJobOnFatalException(JobReport jobReport)
		{
			Console.WriteLine("Fatal Error: {0}", jobReport.FatalException);
		}

		static void ImportJobOnComplete(JobReport jobReport)
		{
			Console.WriteLine("Job Finished With {0} Errors: ", jobReport.ErrorRowCount);
		}
	}

}

