//Опишите свой класс и его методы ниже. Данная сборка будет доступна в namespace ITnet2.Server.UserBusinessLogic.Doc_Stagin.

using ITnet2.Server.BusinessLogic.Core.Documents;
using ITnet2.Server.BusinessLogic.Core.Documents.DataLayer;

public static class DocStagin
{
	public static void SetDocAsCompleted(Document doc)
	{
		doc.Status = DocStatus.Completed;
		new HeadersRepository().Modify(doc, new[] { "DOSTATUS" });
	}
}