//Опишите свой класс и его методы ниже. Данная сборка будет доступна в namespace ITnet2.Server.UserBusinessLogic.Dog_Stagin.

using System;
using System.Collections.Generic;
using System.Linq;
using ITnet2.Common.Data;
using ITnet2.Server.BusinessLogic.Core.Documents;
using ITnet2.Server.BusinessLogic.Core.Documents.DataLayer;
using ITnet2.Server.Data;
using ITnet2.Server.Data.Tables;
using ITnet2.Server.Dialogs;

public static class DogStagin
{
	public static bool CheckDocument(DogTable.Record dogRecord)
	{
		var undog = dogRecord.Undog;
		var dgt = dogRecord.Kdgt;
		var dogstatus = dogRecord.Kdgd;

		return SqlClient.Main.RecordsExist("DMZ",
			new SqlCmdText(
				//"UNDOG = @undog and KDMT = (select KDMT from DOGC_ where KDGT = @KDGT and KDGD = @KDGD) and DOSTATUS = @STATUS",
				"UNDOG = @undog and KDMT in ('04_03_UKR','PRHOXD','PROFNIOKRS') and DOSTATUS = @STATUS",
				new SqlParam("undog", undog),
				new SqlParam("KDGT", dgt),
				new SqlParam("KDGD", dogstatus),
				new SqlParam("STATUS", DocStatus.Active)));
	}

	public static int CreateDocument(DogTable.Record dogRecord)
	{
		var undog = dogRecord.Undog;
		//var dgt = Cursor.GetFieldValue<string>("KDGT");
		var dgt = (dogRecord.Undog_pr == 0) ? dogRecord.Kdgt : DogTable.GetRecord(dogRecord.Undog_pr, new String[] { "KDGT" }).Kdgt;
		//var dogstatus = Cursor.GetFieldValue<string>("KDGD");

		//var docType = SqlClient.Main.CreateCommand("select KDMT from DOGC_ where KDGT = @KDGT and KDGD = @KDGD",
		//    new SqlParam("KDGT", dgt),
		//    new SqlParam("KDGD", dogstatus)).ExecScalar<string>();

		//var docType = SqlClient.Main.CreateCommand(@"select PROTTYPE_ from dgt wher");
		var docType = DgtTable.GetRecord(dgt, new[] { "PROTTYPE_" }).GetFieldValue<string>("PROTTYPE_").Split(',').FirstOrDefault();

		//Формирование документа
		var document = new Document();
		document.FillDocConfig(docType);
		document.ContractorCode = dogRecord.Org;
		document.ContractCode = undog;
		document.Status = DocStatus.Project;
		//Добавление документа в SQL
		var docRepo = new HeadersRepository();
		docRepo.Add(document);

		//Установка документа на 1ю стадию
		var dmz = new DataEditor.StartInfo("DMZ10")
		{
			TemplateId = "DMZ",
			StartMode = new DataEditor.StartInfo.DataEditorStartMode(
				new DataEditor.StartInfo.WorkflowStartMode(WorkflowProcessMode.SetRoute)
				{
					ExitAfterCall = true
				}),
			PrimaryKeyFilter = new Dictionary<string, object>()
			{
				{"UNDOC", document.Undoc}
			},
			Editable = true
		};
		var dc = new DocumentCondition(ConditionDbType.Dmz);
		dmz.Cursors["DMZ"].CustomProperties.Add(DocumentCaller.DocumentConditionPropertyName, dc);
		DataEditor.Call(dmz);

		return document.Undoc;

	}
}