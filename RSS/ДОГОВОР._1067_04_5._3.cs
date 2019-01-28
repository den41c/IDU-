using ITnet2.Server.Data;
using ITnet2.Server.Data.Tables;
using ITnet2.Server.UserBusinessLogic.Dog_Stagin;

/// <summary>
/// Бізнес-логіка вузла _3 (Стадія)
/// </summary>
public class BusinessLogic : BusinessLogicBase
{
	public bool Next()
	{
		return DogStagin.CheckDocument(DogTable.GetRecord(Cursor.GetFieldValue<int>("UNDOG")));
	}

	public void OnStage()
	{
		SqlClient.Main.CreateCommand("update DOG set KDGD = @kdgd where undog = @undog",
			new SqlParam("kdgd", "C"),
			new SqlParam("undog", Cursor.GetFieldValue<int>("UNDOG"))).ExecNonQuery();
		var dogRec = DogTable.GetRecord(Cursor.GetFieldValue<int>("UNDOG"));

		if (!DogStagin.CheckDocument(dogRec))
		{
			DogStagin.CreateDocument(dogRec);
		}
	}
}