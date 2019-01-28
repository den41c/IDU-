//Опишите свой класс и его методы ниже. Данная сборка будет доступна в namespace ITnet2.Server.UserBusinessLogic.Importcontractz.
//UserLib.Importcontractz.TestImport.Exec()

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ITnet2.Common.Tools;
using ITnet2.Server.BusinessLogic.ContextHelp.Undoc10_St;
using ITnet2.Server.BusinessLogic.Core;
using ITnet2.Server.BusinessLogic.Core.Documents;
using ITnet2.Server.BusinessLogic.Core.Documents.DataLayer;
using ITnet2.Server.BusinessLogic.Core.Entity;
using ITnet2.Server.BusinessLogic.Core.Events;
using ITnet2.Server.BusinessLogic.Core.Organizations;
using ITnet2.Server.BusinessLogic.Core.Tools;
using ITnet2.Server.BusinessLogic.LP.Contracts;
using ITnet2.Server.BusinessLogic.MP.ARCT;
using ITnet2.Server.BusinessLogic.MP.TDM.Documents;
using ITnet2.Server.Data;
using ITnet2.Server.Data.TableInfo;
using ITnet2.Server.Data.Tables;
using ITnet2.Server.Dialogs;
using ITnet2.Server.Session;
using ITnet2.Server.Session.ServiceFunctions;

#region SCENARIOS

internal class JoinContractsWizard : ScenarioBase<JoinContractsParameters, EventProvider>
{
	protected override bool Exec()
	{
		if (Settings.Environment.LocalSession && (Debugger.IsAttached || InfoManager.YesNo("Debug?")) && Debugger.Launch())
		{
			Debugger.Break();
		}
		var repository = new ContractRepository();
		var param = new ContractsRepositoryParams(Params.ContractIds.ToArray());
		var contracts = repository.Retrieve(param);
		var newContract = contracts.Join();
		if (newContract != null && repository.LoadToDb(new[] {newContract}))
		{
			//repository.Delete(param);
			return true;
		}
		return false;
	}
}

public class JoinContractsParameters : ScenarioParamsBase
{
	public IEnumerable<int> ContractIds { get; private set; }

	public JoinContractsParameters(params int[] ids)
	{
		ContractIds = ids;
	}

	public override IScenario Scenario
	{
		get { return new JoinContractsWizard(); }
	}
}


internal class CreateSupplyContractScenario : ScenarioBase<CreateSupplyContractParameters, EventProvider>
{
	protected override bool Exec()
	{

		int Undoc = Params.Undoc;

		var _purchaseDocument = new HeadersRepository().Get(Params.Undoc);
		//_purchaseDocument = new Document(Params.Undoc);

		Bidder supplier = null;
		var contracts = new Dictionary<int, int>();
		var sb = SqlClient.Main.CreateSelectBuilder();
		// From field
		sb.Fields.AddRange("DMZ", "KDM6", "ORG", "KFPU", "UNDOG", "KKVT", "KVAL", "KURS", "DDM");
		sb.Fields.AddRange("DMS",
			"NPP", "KMAT", "EDI", "KOL", "CENA_1", "CENA_1VAL", "SUMMA_1", "SUMMA_1VAL", "SUMMA_3", "SUMMA_3VAL", "DLIM", "DLIM2", "KDS1", "NOM_VM", "PRC_KOL", "KOL_DOP1");
		sb.Fields.Add("ORG", "KOR6");
		sb.Fields.Add("ORGK", "NOMPP");
		// Table
		sb.From.Table.Name = "DMS";
		//Joins ORG
		var orgJoin = sb.From.Joins.Add("ORG", JoinType.Inner);
		orgJoin.Condition.Add("ORG");
		//Joins DMZ
		var dmzJoin = sb.From.Joins.Add("DMZ", JoinType.Inner);
		dmzJoin.Condition.Add("UNDOC");
		//Joins ORGK
		var orgkJoin = sb.From.Joins.Add("ORGK", JoinType.Left);
		orgkJoin.Condition.Add("ORG", "ORG");
		orgkJoin.Condition.AddExpression("PR_OSN", "'*'");
		// Condition
		sb.Where = new SqlCmdText("DMS.UNDOC = @Undoc", new SqlParam("Undoc", Undoc));

		//TableViewer.Show(sb.GetCommand());

		using (var rdr = sb.GetCommand().ExecReader())
		{
			while (rdr.Read())
			{

				var bid = rdr.GetObject<Bid>();
				if (supplier == null)
				{
					supplier = new Bidder
					{
						BidderId = bid.BidderId,
						PersonId = rdr.GetFieldValue<int>("NOMPP"),
						VatPayerStatus = rdr.GetFieldValue<string>("KOR6")
					};
				}
				supplier.Bids.Add(bid);
				if (!contracts.ContainsKey(bid.BidderId))
				{
					contracts.Add(bid.BidderId, bid.ContractCode);
				}
			}
		}

		if (supplier == null)
		{
			InfoManager.ShowMessage(@"Немає доступних рядків для формування договору!");
			return false;
		}

		var screen = InputForm.Create("_TENDDOG");
		screen.SetValue("SuppliersId", new List<int>(new[] { supplier.BidderId }));
		screen.SetValue("Contracts", contracts);

		screen.SetValue("MODE", 2);
		screen.SetValue("KDMT", _purchaseDocument.DocumentConfig.Code);



		if (!screen.Activate())
		{
			return false;
		}
		var isUsingExistsContract = screen.GetValue<int>("MODE") == 1;

		

		if (isUsingExistsContract)
		{
			var undog = screen.GetValue<Dictionary<int, int>>("organizationContracts").First().Value;//TODO случай множественого выбора
			_purchaseDocument.ContractCode = undog; 

			//if (InfoManager.YesNo("Рамочный договор")) //TODO проверка на рамочный договор
			//{
			//	var param = new ContractsRepositoryParams(undog);
			//	var contract = new ContractRepository().Retrieve(param).Select(w=>(Contract)w).First();

			//	_purchaseDocument.AddPactCode = contract.CreateAdditionalAgreement();
			//	new HeadersRepository().Modify(_purchaseDocument, new[] { "UNDOG", "UNDOGDS" });
			//	return true;
			//}
			
			new HeadersRepository().Modify(_purchaseDocument, new[] { "UNDOG" });
			return true;
		}	
		else
		{
			var amount = supplier.Bids.Sum(e => e.Amount + e.AmountNDS);
			var amountCurrency = supplier.Bids.Sum(e => e.AmountCurrency + e.AmountCurrencyNDS);
			var firstBid = supplier.Bids.First();

			var contract = new Contract()
			{
				UniqueNumber = Lstn.GetNewNumber("DOG"),
				Contractor = supplier.BidderId,
				СontractTypeCode = screen.GetValue<string>("KDGT"),
				DateCommencementContract = Settings.DateNow,
				DateStartContract = Settings.DateNow,
				//MainOrgContactPersonId = mainOrgContactPersonId,
				PersonIdContragent = supplier.PersonId,
				PersonIdOur = screen.GetValue<int>("PersonIdOur"),
				CodeCurrency = Settings.Ust.NationalCurrencyCode,
				SummaN = amount,
				Summa = amount,
				CurrencyCalculationsCode = firstBid.CurrencyId,
				RateType = firstBid.CurrencyRateType,
				SummaNr = amountCurrency,
				SummaR = amountCurrency,
				MainContract = "*",
				ContractStatus = "1",//ContractLogic.ContractStatus.Project,
				IsCustomer = "+",
				//Route = "AGREE",
				//TemplateForm = false,
				TermOfPayment = firstBid.TermOfPayment,
				SelfOrganization = Settings.Ust.GlOrg.MainOrganization.Code,
				ObjectCode = Settings.Environment.ObjCode,
				Tobj = "O", // В БД написано что поле не используется
				Number = Contract.GetNewContractNumber(screen.GetValue<string>("KDGT")),
				VatPayerStatus = supplier.VatPayerStatus,
				BlstCode = Settings.Environment.BlstCode
			};
			var selectBuilder = SqlClient.Main.CreateSelectBuilder();
			selectBuilder.From.Table.Name = "ORB";
			selectBuilder.Fields.Add("ORB", "NBNK");
			selectBuilder.Where = new SqlCmdText("ORG = @org and KVAL = @kval", new SqlParam("kval", contract.CodeCurrency));
			selectBuilder.OrderBy.AddExpression(SqlClient.Main.Api.Iif("ORB.PR_OSN = '*'", "1", "2"));

			selectBuilder.Where.Parameters["org"].Value = contract.SelfOrganization;
			contract.SelfBankNumber = selectBuilder.GetCommand().ExecScalar<int>();

			selectBuilder.Where.Parameters["org"].Value = contract.Contractor;
			contract.ContractorBankNumber = selectBuilder.GetCommand().ExecScalar<int>();

			new ContractRepository().LoadToDb(new List<Contract>() { contract });
			ContractWorkflow.SetRoute(new HashSet<int>() { contract.UniqueNumber });

			//if (InfoManager.YesNo("Рамочный договор")) //TODO проверка на рамочный договор
			//{
			//	_purchaseDocument.AddPactCode = contract.CreateAdditionalAgreement();
			//	new HeadersRepository().Modify(_purchaseDocument, new[] { "UNDOG", "UNDOGDS" });
			//	return true;
			//}
			_purchaseDocument.ContractCode = contract.UniqueNumber;
			new HeadersRepository().Modify(_purchaseDocument, new[] { "UNDOG" });
			return true;
			
		}
	}
}

public class CreateSupplyContractParameters : ScenarioParamsBase
{
	public int Undoc { get; private set; }

	public CreateSupplyContractParameters(int undoc)
	{
		Undoc = undoc;
	}

	public override IScenario Scenario
	{
		get { return new CreateSupplyContractScenario(); }
	}
}

#endregion

#region ENTITIES
	#region Contract
	public interface IContract : IEntity
	{
		/// <summary>
		/// Уникальный номер договора
		/// </summary>
		[MapField("UNDOG")]
		int UniqueNumber { get; }

		/// <summary>
		/// Объекта
		/// </summary>
		[MapField("KOBJ")]
		string ObjectCode { get; }

		/// <summary>
		/// Тип контрагента
		/// </summary>
		[MapField("TOBJ")]
		string Tobj { get; }

		/// <summary>
		/// Контрагент (свой)
		/// </summary>
		[MapField("ORG_DOG")]
		int SelfOrganization { get; }

		/// <summary>
		/// Номер банка (свой)
		/// </summary>
		[MapField("NBNK2")]
		int SelfBankNumber { get; }

		/// <summary>
		/// Номер банка
		/// </summary>
		[MapField("NBNK")]
		int ContractorBankNumber { get; }

		/// <summary>
		/// Тема
		/// </summary>
		[MapField("KZAJNPP_")]
		string OrderItem { get; }

		/// <summary>
		/// Номер договора
		/// </summary>
		[MapField("KDOGP")]
		string AdditionalNumber { get; }

		/// <summary>
		/// Дата прихода (канцелярия)
		/// </summary>
		[MapField("DATE_P")]
		DateTime DateOfReceipt { get; }

		/// <summary>
		/// Тип договора
		/// </summary>
		[MapField("KDGT")]
		string СontractTypeCode { get; }

		/// <summary>
		/// Дата начала действия договора
		/// </summary>
		[MapField("DDOGN")]
		DateTime DateCommencementContract { get; }

		/// <summary>
		/// Дата окончания действия договора
		/// </summary>
		[MapField("DDOGK")]
		DateTime DateExpirationContract { get; }

		/// <summary>
		/// Номер дела
		/// </summary>
		[MapField("KDOG")]
		string Number { get; set; }

		/// <summary>
		/// Код валюты
		/// </summary>
		[MapField("KVAL")]
		int CodeCurrency { get; }

		/// <summary>
		/// Стаття бюджету
		/// </summary>
		[MapField("KAU")]
		string BudgetItem { get; }

		/// <summary>
		/// Контрагент
		/// </summary>
		[MapField("ORG")]
		int Contractor { get; }

		/// <summary>
		/// Предмет договора
		/// </summary>
		[MapField("SUBJECT_")]
		string Subject { get; }

		/// <summary>
		/// Подразделение
		/// </summary>
		[MapField("CEH")]
		int Department { get; }

		/// <summary>
		/// Сумма выполнения
		/// </summary>
		[MapField("SUMMA_SPF")]
		decimal CurrentAmount { get; }

		/// <summary>
		/// Тема основного договора
		/// </summary>
		[MapField("KZAJNPPOS_")]
		string OrderNumber { get; }

		/// <summary>
		/// KDGD
		/// </summary>
		[MapField("KDGD")]
		string Status { get; set; }

		/// <summary>
		/// Дата темы
		/// </summary>
		[MapField("N_KDK")]
		string Responsible { get; }

		/// <summary>
		/// D0
		/// </summary>
		[MapField("D0_")]
		DateTime DateBeginningWork { get; }

		/// <summary>
		/// Дата темы
		/// </summary>
		[MapField("DATEZAJOS_")]
		DateTime DateOrder { get; }

		/// <summary>
		/// UNDOG_PR
		/// </summary>
		int ParentContract //todo check
		{
			get;
		}

		string PaymentTerm { get; }

		[MapField(CanMissing = MissingType.Always)]
		ContractObjects CurrentObjects { get; set; }

		[MapField(CanMissing = MissingType.Always)]
		ContractPermissions CurrentPermissions { get; set; }

		//[MapField(CanMissing = MissingType.Always)]
		//SpecificationRows SpecificationRows { get; set; }
	}

/// <summary>
/// Класс для работы со стадийностью договоров
/// </summary>
public static class ContractWorkflow
{
	/// <summary>
	/// Установить маршрут стадийности
	/// </summary>
	/// <param name="contractsId">Перечень идентификаторов договоров</param>
	/// <param name="route">Код маршрута</param>
	public static void SetRoute(HashSet<int> contractsId, string route = null)
	{
		var startMode = new DataEditor.StartInfo.WorkflowStartMode(WorkflowProcessMode.SetRoute) 
			{
			BatchMode = true,
			AllRecord = true
		};
		if (!string.IsNullOrWhiteSpace(route))
		{
			startMode.Route = route;
		}
		var c = new ContractCondition {
			SkipConditionDialog = true
		};
		var stageDog = new DataEditor.StartInfo("DOG10") {
			StartMode = new DataEditor.StartInfo.DataEditorStartMode(startMode),
			Editable = true,
			SkipFilterDialogs = true,
			AdditionalFilter = new SqlCmdText(string.Format(
				"DOG.UNDOG in (@arrUndog) and not exists(select 1 from RS1 where RS1.ARSO  = '_DOG10' and RS1.KEYVALUE = {0})",
				TableInfo.Get("DOG").GetSqlStringPkExpression()), new SqlParam("arrUndog", contractsId) { Array = true }),
			TemplateId = "DOG"
		};
		stageDog.Cursors["DOG"].CustomProperties.Add("contractCondition", c);
		DataEditor.Call(stageDog);
	}
}


public class Contract : EntityBase, IContract
	{
		/// <summary>
		/// UNDOG
		/// </summary>
		public int UniqueNumber
		{
			get { return GetValue<int>("UNDOG"); }
			internal set { SetValue("UNDOG", value); }
		}
		/// <summary>
		/// KOBJ
		/// </summary>
		public string ObjectCode
		{
			get { return GetValue<string>("KOBJ"); }
			internal set { SetValue("KOBJ", value); }
		}
		/// <summary>
		/// TOBJ
		/// </summary>
		public string Tobj
		{
			get { return GetValue<string>("TOBJ"); }
			internal set { SetValue("TOBJ", value); }
		}
		/// <summary>
		/// ORG_DOG
		/// </summary>
		public int SelfOrganization
		{
			get { return GetValue<int>("ORG_DOG"); }
			internal set { SetValue("ORG_DOG", value); }
		}
		/// <summary>
		/// NBNK2
		/// </summary>
		public int SelfBankNumber
		{
			get { return GetValue<int>("NBNK2"); }
			internal set { SetValue("NBNK2", value); }
		}
		/// <summary>
		/// NBNK
		/// </summary>
		public int ContractorBankNumber
		{
			get { return GetValue<int>("NBNK"); }
			internal set { SetValue("NBNK", value); }
		}
		/// <summary>
		/// KZAJNPP_
		/// </summary>
		public string OrderItem
		{
			get { return GetValue<string>("KZAJNPP_"); }
			internal set { SetValue("KZAJNPP_", value); }
		}
		/// <summary>
		/// KDOGP
		/// </summary>
		public string AdditionalNumber
		{
			get { return GetValue<string>("KDOGP"); }
			internal set { SetValue("KDOGP", value); }
		}
		/// <summary>
		/// DATE_P
		/// </summary>
		public DateTime DateOfReceipt
		{
			get { return GetValue<DateTime>("DATE_P"); }
			internal set { SetValue("DATE_P", value); }
		}
		/// <summary>
		/// KDGT
		/// </summary>
		public string СontractTypeCode
		{
			get { return GetValue<string>("KDGT"); }
			internal set { SetValue("KDGT", value); }
		}

		/// <summary>
		/// DDOG
		/// </summary>
		public DateTime DateStartContract
		{
			get { return GetValue<DateTime>("DDOG"); }
			internal set { SetValue("DDOG", value); }
		}
		/// <summary>
		/// DDOGN
		/// </summary>
		public DateTime DateCommencementContract
		{
			get { return GetValue<DateTime>("DDOGN"); }
			internal set { SetValue("DDOGN", value); }
		}
		/// <summary>
		/// DDOGK
		/// </summary>
		public DateTime DateExpirationContract
		{
			get { return GetValue<DateTime>("DDOGK"); }
			internal set { SetValue("DDOGK", value); }
		}
		/// <summary>
		/// KDOG
		/// </summary>
		public string Number
		{
			get { return GetValue<string>("KDOG"); }
			set { SetValue("KDOG", value); }
		}
		/// <summary>
		/// KVAL
		/// </summary>
		public int CodeCurrency
		{
			get { return GetValue<int>("KVAL"); }
			internal set { SetValue("KVAL", value); }
		}
		/// <summary>
		/// KAU
		/// </summary>
		public string BudgetItem
		{
			get { return GetValue<string>("KAU"); }
			internal set { SetValue("KAU", value); }
		}
		/// <summary>
		/// ORG
		/// </summary>
		public int Contractor
		{
			get { return GetValue<int>("ORG"); }
			internal set { SetValue("ORG", value); }
		}
		/// <summary>
		/// SUBJECT_
		/// </summary>
		public string Subject
		{
			get { return GetValue<string>("SUBJECT_"); }
			internal set { SetValue("SUBJECT_", value); }
		}
		/// <summary>
		/// CEH
		/// </summary>
		public int Department
		{
			get { return GetValue<int>("CEH"); }
			internal set { SetValue("CEH", value); }
		}
		/// <summary>
		/// SUMMA_SPF
		/// </summary>
		public decimal CurrentAmount
		{
			get { return GetValue<decimal>("SUMMA_SPF"); }
			internal set { SetValue("SUMMA_SPF", value); }
		}
		/// <summary>
		/// KZAJNPPOS_
		/// </summary>
		public string OrderNumber
		{
			get { return GetValue<string>("KZAJNPPOS_"); }
			internal set { SetValue("KZAJNPPOS_", value); }
		}
		/// <summary>
		/// KDGD
		/// </summary>
		public string Status
		{
			get { return GetValue<string>("KDGD"); }
			set { SetValue("KDGD", value); }
		}
		/// <summary>
		/// N_KDK
		/// </summary>
		public string Responsible
		{
			get { return GetValue<string>("N_KDK"); }
			internal set { SetValue("N_KDK", value); }
		}
		/// <summary>
		/// D0_
		/// </summary>
		public DateTime DateBeginningWork
		{
			get { return GetValue<DateTime>("D0_"); }
			internal set { SetValue("D0_", value); }
		}
		/// <summary>
		/// DATEZAJOS_
		/// </summary>
		public DateTime DateOrder
		{
			get { return GetValue<DateTime>("DATEZAJOS_"); }
			internal set { SetValue("DATEZAJOS_", value); }
		}
		/// <summary>
		/// KFPU
		/// </summary>
		public string PaymentTerm
		{
			get { return GetValue<string>("KFPU"); }
			internal set { SetValue("KFPU", value); }
		}
		/// <summary>
		/// UNDOG_PR
		/// </summary>
		public int ParentContract //todo check
		{
			get { return GetValue<int>("UNDOG_PR"); }
			internal set { SetValue("UNDOG_PR", value); }
		}
		/// <summary>
		/// NOMPP_DOG
		/// </summary>
		public int PersonIdOur //todo check
		{
			get { return GetValue<int>("NOMPP_DOG"); }
			internal set { SetValue("NOMPP_DOG", value); }
		}
		/// <summary>
		/// NOMPP
		/// </summary>
		public int PersonIdContragent //todo check
		{
			get { return GetValue<int>("NOMPP"); }
			internal set { SetValue("NOMPP", value); }
		}
		
		/// <summary>
		/// KOR6
		/// </summary>
		public string VatPayerStatus //todo check
		{
			get { return GetValue<string>("KOR6"); }
			internal set { SetValue("KOR6", value); }
		}
		/// <summary>
		/// DDOGN
		/// </summary>
		public DateTime ContractDate//todo check
		{
			get { return GetValue<DateTime>("DDOG"); }
			internal set { SetValue("DDOG", value); }
		}

		/// <summary>
		/// SUMMA_N
		/// </summary>
		public decimal SummaN//todo check
		{
			get { return GetValue<decimal>("SUMMA_N"); }
			internal set { SetValue("SUMMA_N", value); }
		}

		/// <summary>
		/// SUMMA
		/// </summary>
		public decimal Summa//todo check
		{
			get { return GetValue<decimal>("SUMMA"); }
			internal set { SetValue("SUMMA", value); }
		}

		/// <summary>
		/// KVAL_R
		/// </summary>
		public int CurrencyCalculationsCode//todo check
		{
			get { return GetValue<int>("KVAL_R"); }
			internal set { SetValue("KVAL_R", value); }
		}

		/// <summary>
		/// KKVT_R
		/// </summary>
		public string RateType//todo check
		{
			get { return GetValue<string>("KKVT_R"); }
			internal set { SetValue("KKVT_R", value); }
		}

		/// <summary>
		/// SUMMA_NR
		/// </summary>
		public decimal SummaNr//todo check
		{
			get { return GetValue<decimal>("SUMMA_NR"); }
			internal set { SetValue("SUMMA_NR", value); }
		}

		/// <summary>
		/// SUMMA_R
		/// </summary>
		public decimal SummaR//todo check
		{
			get { return GetValue<decimal>("SUMMA_R"); }
			internal set { SetValue("SUMMA_R", value); }
		}

		/// <summary>
		/// PR_OSN
		/// </summary>
		public string MainContract//todo check
		{
			get { return GetValue<string>("PR_OSN"); }
			internal set { SetValue("PR_OSN", value); }
		}
		
		/// <summary>
		/// KDGD
		/// </summary>
		public string ContractStatus //todo check
		{
			get { return GetValue<string>("KDGD"); }
			internal set { SetValue("KDGD", value); }
		}

		/// <summary>
		/// PR_ZAKDOG ("+", "")
		/// </summary>
		public string IsCustomer //todo check
		{
			get { return GetValue<string>("PR_ZAKDOG"); }
			internal set { SetValue("PR_ZAKDOG", value); }
		}

		/// <summary>
		/// KFPU
		/// </summary>
		public string TermOfPayment //todo check
		{
			get { return GetValue<string>("KFPU"); }
			internal set { SetValue("KFPU", value); }
		}
		/// <summary>
		/// KBLS
		/// </summary>
		public string BlstCode //todo check
		{
			get { return GetValue<string>("KBLS"); }
			internal set { SetValue("KBLS", value); }
		}


		public ContractObjects CurrentObjects { get; set; }

		public ContractPermissions CurrentPermissions { get; set; }

		//public SpecificationRows SpecificationRows { get; set; }

		public override string LogicalKey
		{
			get { return UniqueKey; }
		}

		public override string UniqueKey
		{
			get { return UniqueNumber != 0 ? Text.Convert(UniqueNumber) : string.Empty; }
		}

		public Contract() : base()
		{
			CurrentObjects = new ContractObjects();
			CurrentPermissions= new ContractPermissions();
			//SpecificationRows = new SpecificationRows();
		}

		public Contract(Dictionary<string, object> row) : base(row)
		{
			CurrentObjects = new ContractObjects();
			CurrentPermissions = new ContractPermissions();
			//SpecificationRows = new SpecificationRows();
		}

		public static string GetNewContractNumber(string kdgt) // todo check
		{
			var dogType = DgtTable.GetRecord(kdgt).Kdgt_pr;
			if (dogType == "ЕКСПОРТ")
			{
				var contracts = SqlClient.Main.CreateCommand(@"select KDOG 
													from DOG 
													left join DGT on DOG.KDGT = DGT.KDGT 
													where DGT.KDGT_PR = 'ЕКСПОРТ'").ExecObjects(new { KDOG = string.Empty });

                int i;
                List<List<String>> numYear = contracts.Select(el => el.KDOG.Split('-').ToList()).ToList();
                numYear.RemoveAll(el => el.Count < 2);

                List<Int32> temp = numYear
                    .Where(w => w[1] == DateTime.Now.ToString("yyyy") || w[1] == DateTime.Now.ToString("yy"))
                    .Where(w => Int32.TryParse(w[0], out i))
                    .Select(w => Int32.Parse(w[0]))
                    .ToList();
                /*
				var temp = contracts.
					Where(w => Text.Substring(w.KDOG, w.KDOG.Length - 4, 4) == DateTime.Now.Year.ToString()).
					Where(w => Int32.TryParse(Text.Substring(w.KDOG, w.KDOG.Length - 9, 4), out i)).
					Select(w => Int32.Parse(Text.Substring(w.KDOG, w.KDOG.Length - 9, 4)));
                 */
				var maxkmfo = temp.Any() ? temp.Max() : 0;

				return string.Format("1.{0}-{1}", (maxkmfo + 1).ToString(), DateTime.Now.ToString("yy"));
			}
			else
			{
				var contracts = SqlClient.Main.CreateCommand(@"select KDOG 
													from DOG 
													left join DGT on DOG.KDGT = DGT.KDGT 
													where DGT.KDGT_PR <> 'ЕКСПОРТ'").ExecObjects(new { KDOG = string.Empty });

				int i;
                List<List<String>> numYear = contracts.Select(el => el.KDOG.Split('-').ToList()).ToList();
                numYear.RemoveAll(el => el.Count < 2);

                List<Int32> temp = numYear
                    .Where(w => w[1] == DateTime.Now.ToString("yyyy") || w[1] == DateTime.Now.ToString("yy"))
                    .Where(w => Int32.TryParse(w[0], out i))
                    .Select(w => Int32.Parse(w[0]))
                    .ToList();
                /*
				var temp = contracts.
					Where(w => Text.Substring(w.KDOG, w.KDOG.Length - 4, 4) == DateTime.Now.Year.ToString()).
					Where(w => Int32.TryParse(Text.Substring(w.KDOG, w.KDOG.Length - 9, 4), out i)).
					Select(w => Int32.Parse(Text.Substring(w.KDOG, w.KDOG.Length - 9, 4)));
				
                */
                var maxkmfo = temp.Any() ? temp.Max() : 0;
                if (DateTime.Now.Year == 2018)
                {
                    Int32 startValue = 483;
                    temp.Sort();
                    maxkmfo = (!temp.Contains(startValue)) ? startValue : (Int32)temp.First(f => f >= startValue && !temp.Contains(f + 1));
                    /*
                    if (!temp.Contains(startValue))
                    {
                        maxkmfo = startValue;
                    }
                    else
                    {
                        maxkmfo = temp.First(f => f >= startValue && !temp.Contains(f + 1));
                    }*/
                }
				return string.Format("{0}-{1}", (maxkmfo + 1).ToString(), DateTime.Now.ToString("yy"));
			}
		}
	/// <summary>
	/// Создать доп. соглашение
	/// </summary>
	/// <param name="undog">Уникальный номер к которому создается доп. соглашение</param>
	/// <returns>Уникальный номер доп. соглашения</returns>
	public int CreateAdditionalAgreement()
	{
		var param = new ContractsRepositoryParams(this.UniqueNumber);
		var newContractDs = new ContractRepository().Retrieve(param).Select(w => (Contract)w).First();
		newContractDs.ParentContract = this.UniqueNumber;
		newContractDs.UniqueNumber = Lstn.GetNewNumber("DOG");
		newContractDs.SetValue("UNIQGUID", Guid.NewGuid());
		new ContractRepository().LoadToDb(new List<Contract>() { newContractDs });
		return newContractDs.UniqueNumber;
	}

	}

	public class Contracts : CollectionBase<IContract, ContractsRepositoryParams>
	{
		private struct AskResult
		{
			public int Department;
			public string OrderItem;
			public string Responsible { get; set; }
			public string Subject { get; set; }
		}

		internal IContract Join()
		{
			var ask =  new AskResult();
			var form = new InputForm("");
			
			var nKdk = form.Controls.AddCodeNameBox("", "N_KDK");
			nKdk.Table = "KDK";
			nKdk.AutoCaptionFields = true;
			nKdk.CodeField = "N_KDK";
			nKdk.Text = "Ответственный";
			nKdk.RequiredField = true;

			var ceh = form.Controls.AddCodeNameBox(0, "CEH");
			ceh.Table = "POD";
			ceh.AutoCaptionFields = true;
			ceh.CodeField = "CEH";
			ceh.Text = "Подразделение";
			ceh.RequiredField = true;
			
			var kzaj = form.Controls.AddCodeNameBox("", "KZAJNPP");
			kzaj.Table = "ZAE";
			kzaj.AutoCaptionFields = true;
			kzaj.CodeField = "KZAJNPP";
			kzaj.Text = "Тема";
			kzaj.RequiredField = true;
			
			var subj = form.Controls.AddTextBox("", "SUBJECT");
			subj.MultiLine = true;
			subj.Text = "Предмет договора";
			subj.RequiredField = true;
			subj.Height = 3;

			nKdk.Width = ceh.Width = kzaj.Width = subj.Width = 50;
			nKdk.TextWidth = ceh.TextWidth = kzaj.TextWidth = subj.TextWidth = 15;

			if (form.Activate(true))
			{
				ask.Responsible = nKdk.Value;
				ask.Subject = subj.Value;
				ask.Department = ceh.Value;
				ask.OrderItem = kzaj.Value;
			}
			else
			{
				return null;
			}

			Contract newContract = null;
			var groups = Entities.Values.GroupBy(c =>
				new
				{
					ContractNumber = c.Number,
					c.DateOrder,
					c.ObjectCode,
					c.Contractor,
					c.SelfOrganization,
					c.Status,
					c.CodeCurrency,
					c.OrderNumber,
					c.Tobj,
					c.СontractTypeCode
				});

			if (groups.Count() > 1)
			{
				InfoManager.MessageBox(new MessageBoxParams("Нельзя сгруппировать данные договора! Часть показателей для группировки отличаются!")
				{
					AdditionalMessage = "Поля для группировки: Тип договора, Статус договора, № дела, Дата темы, Тема, Контрагент, Валюта.",
					Icon = MessageIcon.Warning
				});
				return null;
			}

			var group = groups.First();
			var contracts = Entities.Values;

			newContract = new Contract
			{
				Number = group.Key.ContractNumber,
				AdditionalNumber = contracts.First().AdditionalNumber,
				Status = group.Key.Status,
				BudgetItem = contracts.First().BudgetItem,
				CodeCurrency = group.Key.CodeCurrency,
				Contractor = group.Key.Contractor,
				ContractorBankNumber = contracts.First().ContractorBankNumber,
				DateOfReceipt = contracts.Min(i => i.DateOfReceipt),
				DateOrder = group.Key.DateOrder,
				ObjectCode = group.Key.ObjectCode,
				OrderItem = ask.OrderItem,
				OrderNumber = group.Key.OrderNumber,
				SelfOrganization = group.Key.SelfOrganization,
				SelfBankNumber = contracts.First().SelfBankNumber,
				Tobj = group.Key.Tobj,
				СontractTypeCode = group.Key.СontractTypeCode,
				//SpecificationRows = new SpecificationRows(),
				CurrentAmount = contracts.Sum(c => c.CurrentAmount),
				DateBeginningWork = contracts.Min(c => c.DateBeginningWork),
				DateCommencementContract = contracts.Min(c => c.DateCommencementContract),
				DateExpirationContract = contracts.Max(c => c.DateExpirationContract),
				Responsible = ask.Responsible,
				Subject = ask.Subject,
				Department = ask.Department,
				PaymentTerm = contracts.First().PaymentTerm
			};

        //foreach (var contract in contracts)
        //{
        //    var includedSpecRows = new SpecificationRows();
        //    includedSpecRows.AddRange(contract.SpecificationRows.Where(r => r.Type == "RDSPECIFIC").ToList());
        //    var notIncludedSpecRows = contract.SpecificationRows.Where(r => r.Type != "RDSPECIFIC").ToList();
        //    notIncludedSpecRows.ForEach(r => r.Contract = newContract);
        //    newContract.SpecificationRows.AddRange(notIncludedSpecRows);
        //    var specificationRow = new SpecificationRow
        //    {
        //        BeginStage = contract.DateCommencementContract,
        //        EndStage = contract.DateExpirationContract,
        //        DeliveryDate = DateTime.MinValue,
        //        CurrentAmount = contract.CurrentAmount,
        //        Order = contract.OrderItem,
        //        ProductName = contract.Subject,
        //        Resposible = contract.Responsible,
        //        Department = contract.Department,
        //        IncludedRows = includedSpecRows,
        //        Contract = newContract,
        //        PaymentTerm = contract.PaymentTerm,
        //        Currency = contract.CodeCurrency,
        //        Type = "RDSPECIFIC"
        //    };
        //    newContract.SpecificationRows.Add(specificationRow);
        //}

        return newContract;
		}
	}
	#endregion
	#region ContractPermission
	public interface IContractPermission : IEntity
	{
		[MapField("RROBJ")]
		string ObjectType { get; }

		[MapField("DOPKEY")]
		string KeyValue { get; }

		[MapField("KGRU")]
		string ItGroupCode { get; }

		[MapField("USERID")]
		string ItUserCode { get; }

		[MapField("KRRTZ")]
		string PermissionType { get; }
	}

	public sealed class ContractPermission : EntityBase, IContractPermission
	{
		public override string LogicalKey
		{
			get { return string.Concat(KeyValue, ItGroupCode, ItUserCode); }
		}

		public string ObjectType
		{
			get { return GetValue<string>("RROBJ"); }
		}
		public string KeyValue
		{
			get { return GetValue<string>("DOPKEY"); }
			internal set { SetValue("DOPKEY", value); }
		}
		public string ItGroupCode
		{
			get { return GetValue<string>("KGRU"); }
			internal set { SetValue("KGRU", value); }
		}
		public string ItUserCode
		{
			get { return GetValue<string>("USERID"); }
			internal set { SetValue("KOBUSERIDJ", value); }
		}
		public string PermissionType
		{
			get { return GetValue<string>("KRRTZ"); }
			internal set { SetValue("KRRTZ", value); }
		}

		public ContractPermission() : base()
		{
			SetValue("RROBJ", "DOG");
		}

		public ContractPermission(Dictionary<string, object> values) : base(values)
		{
		}
	}

	public class ContractPermissions : UserCollectionBase<IContractPermission, ContractPermissionRepositoryParams>
	{
	}

	#endregion
	#region ContractObject
	public interface IContractObject : IEntity
	{
		[MapField("KOBJ")]
		string ObjectCode { get; }

		[MapField("UNDOG")]
		int ContractID { get; }
	}

	public class ContractObject : EntityBase, IContractObject
	{
		public ContractObject(Dictionary<string, object> values) : base(values)
		{
		}

		public override string LogicalKey
		{
			get { return string.Concat(Text.Convert(ContractID, 10), ObjectCode); }
		}

		public string ObjectCode
		{
			get { return GetValue<string>("KOBJ"); }
			internal set { SetValue("KOBJ", value); }
		}

		public int ContractID
		{
			get { return GetValue<int>("UNDOG"); }
			internal set { SetValue("UNDOG", value); }
		}

		public ContractObject() : base()
		{
		}
	}

	public class ContractObjects : UserCollectionBase<IContractObject, ContractObjectRepositoryParams>
	{
		
	}
	#endregion
	#region SpecificationHeader

	//public interface ISpecificationHeader : IEntity
	//{
	//	int SpecificationId { get; }

	//	string DeliveryTerms { get; }

	//	int ContractorCode { get; }

	//	string PaymentTerms { get; }

	//	IContract Contract { get; set; }

	//	string CurrencyRateType { get; }

	//	int CurrencyType { get; }

	//	decimal Currency { get; }

	//	DateTime DocumentDate { get; }

	//	//SpecificationRows IncludedRows { get; set; }
	//}

	//public class SpecificationHeader : EntityBase, ISpecificationHeader
	//{
	//	public override string LogicalKey
	//	{
	//		get { return "???"; }//TODO
	//	}

	//	public int SpecificationId
	//	{
	//		get { return GetValue<int>("UNDOC"); }
	//		internal set { SetValue("UNDOC", value); }
	//	}

	//	public string DeliveryTerms
	//	{
	//		get { return GetValue<string>("KDM6"); }
	//		internal set { SetValue("KDM6", value); }
	//	}

	//	public int ContractorCode
	//	{
	//		get { return GetValue<int>("ORG"); }
	//		internal set { SetValue("ORG", value); }
	//	}

	//	public string PaymentTerms
	//	{
	//		get { return GetValue<string>("KFPU"); }
	//		internal set { SetValue("KFPU", value); }
	//	}

	//	public IContract Contract { get; set; }

	//	public string CurrencyRateType
	//	{
	//		get { return GetValue<string>("KKVT"); }
	//		internal set { SetValue("KKVT", value); }
	//	}

	//	public int CurrencyType
	//	{
	//		get { return GetValue<int>("KVAL"); }
	//		internal set { SetValue("KVAL", value); }
	//	}

	//	public decimal Currency
	//	{
	//		get { return GetValue<decimal>("KURS"); }
	//		internal set { SetValue("KURS", value); }
	//	}

	//	public DateTime DocumentDate
	//	{
	//		get { return GetValue<DateTime>("DDM"); }
	//		internal set { SetValue("DDM", value); }
	//	}
	//}
	//public class SpecificationHeaders : UserCollectionBase<ISpecificationHeader, SpecificationHeadersRepositoryParams>
	//{
		
	//}
	//public class SpecificationHeadersRepositoryParams : CollectionParamsBase
	//{
	//	public IEnumerable<int> SpecificationIds { get; private set; }

	//	public SpecificationHeadersRepositoryParams(params int[] undocs)
	//	{
	//		SpecificationIds = undocs;
	//	}

	//	public override SqlCmdText GetFullCondition()
	//	{
	//		var condition = new SqlCmdText();
	//		if (SpecificationIds.Any())
	//		{
	//			var processTypes = SqlClient.Main.CreateCommand("select kdmt_str from dog join dgt on dog.kdgt = dgt.kdgt where dog.undog in (@undogs)",
	//				new SqlParam("undogs", SpecificationIds) { Array = true }).ExecScalars<string>().SelectMany(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).Distinct();

	//			condition = SqlCmdText.ConcatCommands(SqlCmdText.ConcatCommandsType.And,
	//				new SqlCmdText("DMZ.KDMT in (@processTypes) and DMZ.UNDOG in (@undogs)", new SqlParam("processTypes", processTypes) { Array = true }, new SqlParam("undogs", SpecificationIds) { Array = true }));
	//		}
	//		return condition.IsEmpty() ? new SqlCmdText("1=0") : condition;
	//		//return new SqlCmdText("1=0");
	//	}
	//}

	//public class SpecificationHeaderRepository :
	//	UserRepositoryBase<SpecificationHeader, SpecificationHeaders, ISpecificationHeader, SpecificationHeadersRepositoryParams>
	//{
	//	protected override ColumnCollection RequiredFields
	//	{
	//		get
	//		{
	//			var fields = new ColumnCollection();
	//			fields.Add("DMZ", "UNDOG");
	//			fields.Add("DMZ", "KVAL");
	//			fields.Add("DMZ", "ORG");
	//			fields.Add("DMZ", "KDMT");
	//			fields.Add(TableName, TableInfo.Fields.Where(f => !new[] { "KVAL", "ORG", "KDMT" }.Contains(f.Name)).Select(f => f.Name).ToList());
	//			return fields;
	//		}
	//	}

	//	private string _tableName;

	//	protected override string TableName
	//	{
	//		get { return _tableName; }
	//	}

	//	public SpecificationHeaderRepository()
	//		: this("DMZ")
	//	{

	//	}

	//	internal SpecificationHeaderRepository(string table)
	//	{
	//		_tableName = table;
	//	}

	//	protected override JoinCollection AdditionalJoins
	//	{
	//		get
	//		{
	//			var joins = new JoinCollection();
	//			var dmzJoin = joins.Add("DMZ", JoinType.Inner);
	//			dmzJoin.Condition.Add("UNDOC");
	//			return joins;
	//		}
	//	}

	//	public override bool Delete(SpecificationHeadersRepositoryParams collectionParams)
	//	{
	//		var par = new DataEditor.EditRecordStartInfo("DMZ10_SIMP")
	//		{
	//			AdditionalFilter = collectionParams.GetFullCondition(),
	//			EditMode = EditMode.Delete,
	//			Editable = true,
	//			SkipEditScreen = true,
	//			Multi = true,

	//			//MultiCheckRowsCondition = collectionParams.GetFullCondition()
	//		};
	//		DataEditor.CallEditRecord(par);
	//		return par.HelpResult.Success;
	//	}


	//	public override bool LoadToDb(SpecificationHeaders collection)
	//	{
	//		return loadToDb(collection);
	//	}

	//	//public override bool LoadToDb(IEnumerable<SpecificationHeaders> entities)
	//	//{
	//	//	var collection = new SpecificationRows();
	//	//	collection.AddRange(entities);
	//	//	return loadToDb(collection);
	//	//}

	//	private bool loadToDb(SpecificationHeaders entities)
	//	{
	//		var hr = new HeadersRepository();
	//		bool ret = false;
	//		foreach (var group in entities.GroupBy(i => new { i.Contract, i.Type }))
	//		{

	//			var document = new Document();
	//			document.FillDocConfig(group.Key.Type);
	//			document.ContractNumber = group.Key.Contract.Number;
	//			document.ContractCode = group.Key.Contract.UniqueNumber;
	//			document.ContractorCode = group.Key.Contract.Contractor;
	//			document.DepartRecipient = group.Key.Contract.Department;
	//			document.Date = group.Key.Contract.DateOfReceipt;
	//			document.Responsible = group.Key.Contract.Responsible;
	//			document.Comment = group.Key.Contract.Subject;
	//			var numRes = document.GetNewNumber();
	//			document.Number = numRes.ToString();
	//			hr.Add(document);
	//			ret = document.Undoc != 0;
	//			if (ret)
	//			{
	//				var rr = new RowsRepository(document);
	//				_npp = 0;
	//				rr.Add(getRows(entities.GetForContractAndType(group.Key.Contract.UniqueNumber, group.Key.Type), _npp));
	//				ret = !rr.HasDbException;
	//			}
	//			if (!ret)
	//			{
	//				break;
	//			}
	//		}
	//		return ret;
	//	}

	//	private int _npp;

	//	private IEnumerable<DocumentRow> getRows(IEnumerable<ISpecificationRow> group, int parentRow)
	//	{
	//		List<DocumentRow> rows = new List<DocumentRow>();
	//		foreach (var specRow in group)
	//		{
	//			var row = new DocumentRow
	//			{
	//				Npp = ++_npp,
	//				ResourceCode = specRow.ResourceCode,
	//				ResourceTitle = specRow.ProductName,
	//				DateFrom = specRow.BeginStage,
	//				DateTo = specRow.EndStage,
	//				ProductLotDate = specRow.DeliveryDate,
	//				SertNumber = specRow.PaymentTerm,
	//				CurrencyCode = specRow.Currency,
	//			};
	//			row.SetValue("NPP_PR", parentRow);
	//			//row.SetBaseRow(parentRow);

	//			row.Prices.Sender = row.Prices.SenderCur = specRow.CurrentAmount;
	//			row.Amounts.Sender = row.Amounts.SenderCur = specRow.CurrentAmount;
	//			row.Accounts.Debit.Order = specRow.Order;
	//			rows.Add(row);
	//			if (specRow.IncludedRows != null && specRow.IncludedRows.Any())
	//			{
	//				rows.AddRange(getRows(specRow.IncludedRows, _npp));
	//			}
	//		}
	//		return rows;
	//	}
	//}


#endregion

	#region Specification
	public interface ISpecificationRow : IEntity
	{
		IContract Contract { get; set; }

		int ContractKey { get; }

		string ResourceCode { get; } 

		string ProductName { get; }

		DateTime BeginStage { get; }

		DateTime EndStage { get; }

		DateTime DeliveryDate { get; }

		decimal CurrentAmount { get; }

		string Order { get; }

		string Resposible { get; }

		int Department { get; }

		SpecificationRows IncludedRows { get; set; }
		
		string PaymentTerm { get; }

		string Type { get; }

		int Currency { get; }

	}

	public class SpecificationRow : EntityBase, ISpecificationRow 
	{
		private SpecificationRows _includedRows;
		public IContract Contract { get; set; }

		public int ContractKey
		{
			get
			{
				if (Contract != null && Contract.UniqueNumber != 0)
				{
					SetValue("UNDOG", Contract.UniqueNumber);
				}
				return GetValue<int>("UNDOG");
			}
		}

		public string ResourceCode
		{
			get { return GetValue<string>("KMAT"); }
			internal set { SetValue("KMAT", value); }
		}

		public decimal CurrentAmount
		{
			get { return GetValue<decimal>("SUMMA_1VAL"); }
			internal set { SetValue("SUMMA_1VAL", value); }
		}

		public DateTime BeginStage
		{
			get { return GetValue<DateTime>("DATE_1"); }
			internal set { SetValue("DATE_1", value); }
		}

		public DateTime EndStage
		{
			get { return GetValue<DateTime>("DATE_2"); }
			internal set { SetValue("DATE_2", value); }
		}

		public DateTime DeliveryDate
		{
			get { return GetValue<DateTime>("DLIM"); }
			internal set { SetValue("DLIM", value); }
		}

		public string ProductName
		{
			get { return GetValue<string>("NMAT_DOC"); }
			internal set { SetValue("NMAT_DOC", value); }
		}

		public string Order
		{
			get { return GetValue<string>("KZAJ_DB"); }
			internal set { SetValue("KZAJ_DB", value); }
		}

		public string Resposible
		{
			get { return GetValue<string>("N_KDK"); }
			internal set { SetValue("N_KDK", value); }
		}

		public int Department
		{
			get { return GetValue<int>("CEH_K"); }
			internal set { SetValue("CEH_K", value); }
		}

		public SpecificationRows IncludedRows { get; set; }

		public SpecificationRow() : base() {}

		public SpecificationRow(Dictionary<string, object> row) : base(row)
		{
			
		}

		public override string LogicalKey
		{
			get { return string.Concat(Text.Convert(ContractKey, 10), Type, ResourceCode, ProductName, Order, Text.Convert(Department,10), Resposible, Text.Convert(BeginStage, Text.DateTimeView.YyyyMmDd), Text.Convert(EndStage, Text.DateTimeView.YyyyMmDd)); }
		}

		public string PaymentTerm
		{
			get { return GetValue<string>("KFPU"); }
			internal set { SetValue("KFPU", value); }
		}

		public string Type
		{
			get { return GetValue<string>("KDMT"); }
			internal set { SetValue("KDMT", value); }
		}

		public int Currency
		{
			get { return GetValue<int>("KVAL"); }
			internal set { SetValue("KVAL", value); }
		}
}

	public class SpecificationRows : UserCollectionBase<ISpecificationRow, SpecificationRowsRepositoryParams>
	{
		public new IEnumerable<ISpecificationRow> this[int undog]
		{
			get { return Entities.Where(e => e.Key.StartsWith(Text.Convert(undog, 10))).Select(i => i.Value).ToList(); }
		}

		public IEnumerable<ISpecificationRow> GetForContractAndType(int undog, string type)
		{
			var ret = new List<ISpecificationRow>();
			foreach (var entity in Entities)
			{
				if (entity.Key.StartsWith(Text.Convert(undog, 10) + type))
				{
					ret.Add(entity.Value);
				}
			}
			return ret;
		}
	}
	#endregion

public class UserCollectionBase<TInterface, TCollectionParams> : CollectionBase<TInterface, TCollectionParams> 
	where TInterface : IEntity 
	where TCollectionParams : ICollectionParams
{
	public IEnumerable<TInterface> GetForContract(int undog)
	{
		var ret = new List<TInterface>();
		foreach (var entity in Entities)
		{
			if (entity.Key.StartsWith(Text.Convert(undog, 10)))
			{
				ret.Add(entity.Value);
			}
		}
		return ret;
	}
}
#endregion

#region REPOSITORIES

public class Bidder
	{
		/// <summary>
		/// Идентификатор поставщика
		/// </summary>
		public int BidderId { get; set; }

		/// <summary>
		/// Статус плательщика налога на прибыль
		/// </summary>
		public string VatPayerStatus { get; set; }

		/// <summary>
		/// Идентификатор договора
		/// </summary>
		public int ContractId { get; set; }

		/// <summary>
		/// Идентификатор доп. соглашения
		/// </summary>
		public int AgreementId { get; set; }

		/// <summary>
		/// Идентификатор контактного лица
		/// </summary>
		public int PersonId { get; set; }

		public List<Bid> Bids { get; private set; }

		public Bidder()
		{
			Bids = new List<Bid>();
		}

		public void AddBid(Bid bid)
		{
			Bids.Add(bid);
		}
	}

public class Bid
	{
		[MapField("NPP")]
		public int BidId;

		[MapField("DDM")]
		public DateTime CurrencyRateDate;

		[MapField("ORG")]
		public int BidderId;

		[MapField("KMAT")]
		public string ResourseCode;

		[MapField("EDI")]
		public int MeasureId;

		[MapField("KOL")]
		public decimal Quantity;

		[MapField("KVAL")]
		public int CurrencyId;

		[MapField("KKVT")]
		public string CurrencyRateType;

		[MapField("KURS")]
		public decimal CurrencyRate;

		[MapField("CENA_1")]
		public decimal Price;

		[MapField("CENA_1VAL")]
		public decimal PriceCurrency;

		/// <summary>
		/// Сумма предложения
		/// </summary>
		[MapField("SUMMA_1")]
		public decimal Amount;

		/// <summary>
		/// Сумма предложения в валюте
		/// </summary>
		[MapField("SUMMA_1VAL")]
		public decimal AmountCurrency;

		/// <summary>
		/// Сумма предложения НДС
		/// </summary>
		[MapField("SUMMA_3")]
		public decimal AmountNDS;

		/// <summary>
		/// Сумма предложения в валюте НДС
		/// </summary>
		[MapField("SUMMA_3VAL")]
		public decimal AmountCurrencyNDS;

		/// <summary>
		/// Место поставки
		/// </summary>
		[MapField("KDS1")]
		public string DeliveryPlace;

		/// <summary>
		/// Базис поставки
		/// </summary>
		[MapField("KDM6")]
		public string DeliveryType;

		/// <summary>
		/// Гарантия
		/// </summary>
		[MapField("PRC_KOL")]
		public decimal Warranty;

		/// <summary>
		/// Срок поставки
		/// </summary>
		[MapField("KOL_DOP1")]
		public decimal DeliveryTime;

		/// <summary>
		/// Дата поставки з
		/// </summary>
		[MapField("DLIM")]
		public DateTime DeliveryStartDate;
		/// <summary>
		/// Дата поставки по
		/// </summary>
		[MapField("DLIM2")]
		public DateTime DeliveryEndDate;

		/// <summary>
		/// Условия оплаты
		/// </summary>
		[MapField("KFPU")]
		public string TermOfPayment;

		/// <summary>
		/// Договір
		/// </summary>
		[MapField("UNDOG")]
		public int ContractCode;
	}

public class ContractsRepositoryParams :  CollectionParamsBase
{
	public IEnumerable<int> ContractIds { get; private set; }

	public ContractsRepositoryParams(params int[] ids)
	{
		ContractIds = ids;
	}

	public override SqlCmdText GetFullCondition()
	{
		var condition = new SqlCmdText();
		if (ContractIds.Any())
		{
			condition = SqlCmdText.ConcatCommands(SqlCmdText.ConcatCommandsType.And,
				new SqlCmdText("UNDOG in (@undogs)", new SqlParam("undogs", ContractIds) {Array = true}));
		}
		return condition.IsEmpty() ? new SqlCmdText("1=0") : condition;
	}
}

public class ContractRepository : RepositoryBase<Contract, Contracts, IContract, ContractsRepositoryParams>
{
	private ContractObjectRepository _contractObjectRepository;
	private ContractPermissionRepository _contractPermissionRepository;
	//private SpecificationRowsRepository _specificationRowsRepository;

	protected override string TableName
	{
		get { return "DOG"; }
	}

	protected override ColumnCollection RequiredFields
	{
		get
		{
			return new ColumnCollection(TableName, true);
		} 
	}

	protected override void FormUniqueKey(IEntityRecord entity)
	{
		entity.SetValue("UNDOG", Lstn.GetNewNumber("DOG"));
	}

	public override Contracts Retrieve(ContractsRepositoryParams collectionParams)
	{
		var res = base.Retrieve(collectionParams);
		if (res != null && res.Any())
		{
			var objects = _contractObjectRepository.Retrieve(new ContractObjectRepositoryParams(collectionParams.ContractIds.ToArray()));
			var permissions = _contractPermissionRepository.Retrieve(new ContractPermissionRepositoryParams(collectionParams.ContractIds.ToArray()));
			//var specifications = _specificationRowsRepository.Retrieve(new SpecificationRowsRepositoryParams(collectionParams.ContractIds.ToArray()));
			foreach (var contract in res)
			{
				contract.CurrentObjects.AddRange(objects.GetForContract(contract.UniqueNumber));
				contract.CurrentPermissions.AddRange(permissions.GetForContract(contract.UniqueNumber));
				//contract.SpecificationRows.AddRange(specifications.GetForContract(contract.UniqueNumber));
			}
		}
		return res;
	}

	public override bool LoadToDb(IEnumerable<IContract> entities)
	{
		var contracts = entities as IList<IContract> ?? entities.ToList();

		var ret = base.LoadToDb(contracts);
		if (ret)
		{
			foreach (var contract in contracts)
			{
				if (contract.CurrentObjects == null || !contract.CurrentObjects.Any())
				{
					(contract.CurrentObjects ?? (contract.CurrentObjects = new ContractObjects())).Add(ImportContractObjectFactory.Create(contract));
				}
				if (contract.CurrentPermissions == null)
				{
					(contract.CurrentPermissions ?? (contract.CurrentPermissions = new ContractPermissions())).AddRange(ImportContractPermissionFactory.Create(contract));
				}
			}
			_contractObjectRepository.LoadToDb(contracts.SelectMany(c => c.CurrentObjects));
			_contractPermissionRepository.LoadToDb(contracts.SelectMany(c => c.CurrentPermissions));
		    //var specifications = contracts.SelectMany(c => c.SpecificationRows).ToList();
		    //if (specifications.Any())
		    //{
		    //    _specificationRowsRepository.LoadToDb(specifications);
		    //}
		}
		return ret;
	}

	public override bool Delete(ContractsRepositoryParams collectionParams)
	{
		return
			//_specificationRowsRepository.Delete(new SpecificationRowsRepositoryParams(collectionParams.ContractIds.ToArray())) &&
			_contractPermissionRepository.Delete(new ContractPermissionRepositoryParams(collectionParams.ContractIds.ToArray())) &&
			_contractObjectRepository.Delete(new ContractObjectRepositoryParams(collectionParams.ContractIds.ToArray())) && 
			base.Delete(collectionParams);
	}

	public ContractRepository()
	{
		_contractObjectRepository = new ContractObjectRepository();
		_contractPermissionRepository = new ContractPermissionRepository();
		//_specificationRowsRepository = new SpecificationRowsRepository();
	}
}

public class ContractObjectRepositoryParams : CollectionParamsBase
{
	public IEnumerable<int> ContractIds { get; private set; }

	public ContractObjectRepositoryParams(params int[] ids)
	{
		ContractIds = ids;
	}

	public override SqlCmdText GetFullCondition()
	{
		var condition = new SqlCmdText();
		if (ContractIds.Any())
		{
			condition = SqlCmdText.ConcatCommands(SqlCmdText.ConcatCommandsType.And,
				new SqlCmdText("UNDOG in (@undogs)", new SqlParam("undogs", ContractIds) { Array = true }));
		}
		return condition.IsEmpty() ? new SqlCmdText("1=0") : condition;
	}
}

public class ContractObjectRepository : RepositoryBase<ContractObject, ContractObjects, IContractObject, ContractObjectRepositoryParams>
{
	protected override string TableName
	{
		get { return "DOG_OBJ"; }
	}

	protected override ColumnCollection RequiredFields
	{
		get
		{
			return new ColumnCollection(TableName, true);
		}
	}
}

public class ContractPermissionRepositoryParams : CollectionParamsBase
{
	public IEnumerable<int> ContractIds { get; private set; }

	public ContractPermissionRepositoryParams(params int[] ids)
	{
		ContractIds = ids;
	}

	public override SqlCmdText GetFullCondition()
	{
		var condition = new SqlCmdText();
		if (ContractIds.Any())
		{
			condition = SqlCmdText.ConcatCommands(SqlCmdText.ConcatCommandsType.And,
				new SqlCmdText("RROBJ = 'DOG' and DOPKEY in (@undogs)", new SqlParam("undogs", ContractIds.Select(i=>Text.Convert(i,10))) { Array = true }));
		}
		return condition.IsEmpty() ? new SqlCmdText("1=0") : condition;
	}
}

public class ContractPermissionRepository : RepositoryBase<ContractPermission, ContractPermissions, IContractPermission, ContractPermissionRepositoryParams>
{
	protected override ColumnCollection RequiredFields
	{
		get
		{
			return new ColumnCollection(TableName, true);
		}
	}

	protected override string TableName
	{
		get { return "RRTZ"; }
	}
}

public class SpecificationRowsRepositoryParams : CollectionParamsBase
{
	public IEnumerable<int> ContractIds { get; private set; }

	public SpecificationRowsRepositoryParams(params int[] ids)
	{
		ContractIds = ids;
	}

	public override SqlCmdText GetFullCondition()
	{
		var condition = new SqlCmdText();
		if (ContractIds.Any())
		{
			var processTypes = SqlClient.Main.CreateCommand("select kdmt_str from dog join dgt on dog.kdgt = dgt.kdgt where dog.undog in (@undogs)",
				new SqlParam("undogs", ContractIds) {Array = true}).ExecScalars<string>().SelectMany(s=>s.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries)).Distinct();

			condition = SqlCmdText.ConcatCommands(SqlCmdText.ConcatCommandsType.And,
				new SqlCmdText("DMZ.KDMT in (@processTypes) and DMZ.UNDOG in (@undogs)", new SqlParam("processTypes", processTypes) { Array = true }, new SqlParam("undogs", ContractIds) { Array = true }));
		}
		return condition.IsEmpty() ? new SqlCmdText("1=0") : condition;
	}
}

public class SpecificationRowsRepository :
	UserRepositoryBase<SpecificationRow, SpecificationRows, ISpecificationRow, SpecificationRowsRepositoryParams>
{
	protected override ColumnCollection RequiredFields
	{
		get
		{
			var fields =new ColumnCollection();
			fields.Add("DMZ", "UNDOG");
			fields.Add("DMZ", "KVAL");
			fields.Add("DMZ","ORG");
			fields.Add("DMZ","KDMT");
			fields.Add(TableName, TableInfo.Fields.Where(f=> !new[]{"KVAL","ORG","KDMT"}.Contains(f.Name)).Select(f=>f.Name).ToList());
			return fields;
		}
	}

	private string _tableName;

	protected override string TableName
	{
		get { return _tableName;  }
	}

	public SpecificationRowsRepository() : this("DMS")
	{
		
	}

	internal SpecificationRowsRepository(string table)
	{
		_tableName = table;
	}

	protected override JoinCollection AdditionalJoins
	{
		get
		{
			var joins = new JoinCollection();
			var dmzJoin = joins.Add("DMZ", JoinType.Inner);
			dmzJoin.Condition.Add("UNDOC");
			return joins;
		}
	}

	public override bool Delete(SpecificationRowsRepositoryParams collectionParams)
	{
		var par = new DataEditor.EditRecordStartInfo("DMZ10_SIMP")
		{
			AdditionalFilter = collectionParams.GetFullCondition(),
			EditMode = EditMode.Delete,
			Editable = true,
			SkipEditScreen = true,
			Multi = true,
			
			//MultiCheckRowsCondition = collectionParams.GetFullCondition()
		};
		DataEditor.CallEditRecord(par);
		return par.HelpResult.Success;
	}

	public override bool LoadToDb(SpecificationRows collection)
	{
		return loadToDb(collection);
	}

	public override bool LoadToDb(IEnumerable<ISpecificationRow> entities)
	{
		var collection = new SpecificationRows();
		collection.AddRange(entities);
		return loadToDb(collection);
	}

	private bool loadToDb(SpecificationRows entities)
	{
		var hr = new HeadersRepository();
		bool ret = false;
		foreach (var group in entities.GroupBy(i=>new {i.Contract, i.Type }))
		{

			var document = new Document();
			document.FillDocConfig(group.Key.Type);
			document.ContractNumber = group.Key.Contract.Number;
			document.ContractCode = group.Key.Contract.UniqueNumber;
			document.ContractorCode = group.Key.Contract.Contractor;
			document.DepartRecipient = group.Key.Contract.Department;
			document.Date = group.Key.Contract.DateOfReceipt;
			document.Responsible = group.Key.Contract.Responsible;
			document.Comment = group.Key.Contract.Subject;
			var numRes = document.GetNewNumber();
			document.Number = numRes.ToString();
			hr.Add(document);
			ret = document.Undoc != 0;
			if (ret)
			{
				var rr = new RowsRepository(document);
				_npp = 0;
				rr.Add(getRows(entities.GetForContractAndType(group.Key.Contract.UniqueNumber, group.Key.Type), _npp));
				ret = !rr.HasDbException;
			}
			if (!ret)
			{
				break;
			}
		}
		return ret;
	}

	private int _npp;

	private IEnumerable<DocumentRow> getRows(IEnumerable<ISpecificationRow> group, int parentRow)
	{
		List<DocumentRow> rows = new List<DocumentRow>();
		foreach (var specRow in group)
		{
			var row = new DocumentRow
			{
				Npp = ++_npp,
				ResourceCode = specRow.ResourceCode,
				ResourceTitle = specRow.ProductName,
				DateFrom = specRow.BeginStage,
				DateTo = specRow.EndStage,
				ProductLotDate = specRow.DeliveryDate,
				SertNumber = specRow.PaymentTerm,
				CurrencyCode = specRow.Currency,
			};
			row.SetValue("NPP_PR", parentRow);
			//row.SetBaseRow(parentRow);

			row.Prices.Sender = row.Prices.SenderCur = specRow.CurrentAmount;
			row.Amounts.Sender = row.Amounts.SenderCur = specRow.CurrentAmount;
			row.Accounts.Debit.Order = specRow.Order;
			rows.Add(row);
			if (specRow.IncludedRows != null && specRow.IncludedRows.Any())
			{
				rows.AddRange(getRows(specRow.IncludedRows, _npp));
			}
		}
		return rows;
	}
}

public abstract class UserRepositoryBase<TEntity, TEntityCollection, TIEntity, TRepositoryParams> : RepositoryBase<TEntity, TEntityCollection, TIEntity, TRepositoryParams> 
	where TEntity : EntityBase 
	where TEntityCollection : ICollection<TIEntity, TRepositoryParams> 
	where TIEntity : IEntity 
	where TRepositoryParams : ICollectionParams
{
	protected override ColumnCollection RequiredFields
	{
		get
		{
			return new ColumnCollection(TableName, true);
		}
	}
}

#endregion

#region FACTORIES

internal static class ImportContractPermissionFactory
{
	public static IEnumerable<IContractPermission> Create(IContract contract)
	{
		List<string> users;
		List<string> groups;
		
		AccessManager.GetAllowedUsersAndGroups("DGT_EDIT", contract.СontractTypeCode, out users, out groups);
		
		var keyValue = Text.Convert(contract.UniqueNumber, 10);
		var permissionList = new List<IContractPermission>();
		permissionList.AddRange(users.Select(user => new ContractPermission
		{
			KeyValue = keyValue,
			PermissionType = "+",
			ItUserCode = user
		}));
		permissionList.AddRange(groups.Select(gr => new ContractPermission
		{
			KeyValue = keyValue,
			PermissionType = "+",
			ItGroupCode = gr
		}));
		return permissionList;
	}
}

internal static class ImportContractObjectFactory
{
	public static IContractObject Create(IContract contract)
	{
		return new ContractObject
		{
			ObjectCode = contract.ObjectCode,
			ContractID = contract.UniqueNumber
		};
	}
}

#endregion

#region STATIC
public static class DefaultMethods
{
    private static int _undog;
    private static IContract _contract;
    private static ContractRepository _repository = new ContractRepository();
    static bool loadContract(int undog)
    {
        if (_undog != undog)
        {
            _undog = undog;
            _contract = _repository.Retrieve(new ContractsRepositoryParams(new[] { _undog })).FirstOrNull();
        }
        return _contract != null;
    }
    public static string GetContractStatus(int undog)
    {
        return !loadContract(undog) ? string.Empty : _contract.Status;
    }

    public static bool SetContractStatus(int undog, string status)
    {
        if (!loadContract(undog))
        {
            return false;
        }
        _contract.Status = status;
        return _repository.LoadToDb(new[] {_contract});
    }

    public static bool IsContractOnAgree(int undog)
    {
        return Sp2Table.GetRecord("DGD", GetContractStatus(undog)).Pr_sp1 == "A";
    }

    public static string NewNumber(string kdgt)
    {
        var dogType = DgtTable.GetRecord(kdgt);

        if (string.IsNullOrEmpty(dogType.Kdgt_pr))
        {
            return string.Empty;
        }
        return string.Format("{0}", DateTime.Now.Year);
    }
}
#endregion

/*4491
with d as (select undog from dog where kdgt = 'TEMP'),
s as (select undoc from dmz join d on d.undog = dmz.undog)
delete from dms where undoc in (select undoc from s);

with d as (select undog from dog where kdgt = 'TEMP')
delete from dmz where undog in (select undog from d);

with d as (select undog from dog where kdgt = 'TEMP')
delete from dog_obj where undog in (select undog from d);

with d as (select undog from dog where kdgt = 'TEMP')
delete from rrtz where rrobj = 'DOG' and dopkey in (select str(undog,10) from d);

delete from dog where kdgt = 'TEMP';
*/

/*
		var tName = "[ASU_IDY].[dbo].[OSVedZ_IT_dog]";
		var loc = SqlClient.Main.CreateCommand("SELECT * FROM " + tName).LoadToLocalDb();
		var dbfClientFileName = OSDialogs.SaveFile("Сохранить в файл...", "EXP", new DialogFilterCollection("DBF"));
		var fptClientFileName = System.IO.Path.ChangeExtension(dbfClientFileName, "FPT") ?? string.Empty;
		
		var tmpDir = Settings.Environment.TempDir;
		var dbfServerFileName = System.IO.Path.Combine(tmpDir, System.IO.Path.GetFileName(dbfClientFileName));
		var fptServerFileName = System.IO.Path.Combine(tmpDir, System.IO.Path.GetFileName(fptClientFileName));
		ExternalTables.SqlToFile(dbfServerFileName, loc);

		FileManager.SendFile(dbfServerFileName, dbfClientFileName);
		if (System.IO.File.Exists(fptClientFileName))
		{
			FileManager.SendFile(fptServerFileName, fptClientFileName);
		}

		SqlClient.Local.DropTempTable(loc);
*/