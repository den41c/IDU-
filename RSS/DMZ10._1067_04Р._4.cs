using ITnet2.Server.BusinessLogic.Core.Documents;
using  ITnet2.Server.UserBusinessLogic.Doc_Stagin;

/// <summary>
/// Бізнес-логіка вузла _4 (Стадія)
/// </summary>
public class BusinessLogic : BusinessLogicBase
{
	public  void SetDocAsCompleted()
	{
		DocStagin.SetDocAsCompleted(Cursor.GetRowObject<Document>());
	}
}