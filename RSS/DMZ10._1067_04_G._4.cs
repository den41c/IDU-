using ITnet2.Server.BusinessLogic.Core.Documents;
using ITnet2.Server.BusinessLogic.Core.Documents.DataLayer;

/// <summary>
/// ������-����� ����� _4 (�����)
/// </summary>
public class BusinessLogic : BusinessLogicBase
{
	public void SetDocAsCompleted()
	{
		var doc = Cursor.GetRowObject<Document>();

		doc.Status = DocStatus.Completed;
		new HeadersRepository().Modify(doc, new []{ "DOSTATUS" });
	}
}