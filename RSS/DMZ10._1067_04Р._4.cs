using ITnet2.Server.BusinessLogic.Core.Documents;
using  ITnet2.Server.UserBusinessLogic.Doc_Stagin;

/// <summary>
/// ������-����� ����� _4 (�����)
/// </summary>
public class BusinessLogic : BusinessLogicBase
{
	public  void SetDocAsCompleted()
	{
		DocStagin.SetDocAsCompleted(Cursor.GetRowObject<Document>());
	}
}