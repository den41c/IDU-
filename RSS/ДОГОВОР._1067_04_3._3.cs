using System.Security.Cryptography.X509Certificates;
using ITnet2.Server.BusinessLogic.ContextHelp.Dog;
using ITnet2.Server.BusinessLogic.Core.DocumentConfig;
using ITnet2.Server.BusinessLogic.Core.Documents;
using ITnet2.Server.BusinessLogic.Core.Documents.DataLayer;
using ITnet2.Server.BusinessLogic.Core.Documents.Forming;
using ITnet2.Server.Data;
using ITnet2.Server.Data.Tables;
using ITnet2.Server.Dialogs;
using ITnet2.Server.Session;
using System;
using System.Linq;
using ITnet2.Server.UserBusinessLogic.Dog_Stagin;

/// <summary>
/// Бизнес-логика узла _4 (Стадия)
/// </summary>
public class BusinessLogic : BusinessLogicBase
{
    public bool Next()
    {
	    return DogStagin.CheckDocument(DogTable.GetRecord(Cursor.GetFieldValue<int>("UNDOC")));
    }

	public void OnStage()
	{
		SqlClient.Main.CreateCommand("update DOG set KDGD = @kdgd where undog = @undog",
			new SqlParam("kdgd", "A"),
			new SqlParam("undog", Cursor.GetFieldValue<int>("UNDOG"))).ExecNonQuery();
		var dogRec = DogTable.GetRecord(Cursor.GetFieldValue<int>("UNDOC"));

		if (!DogStagin.CheckDocument(dogRec))
		{
			DogStagin.CreateDocument(dogRec);
		}

	}
}