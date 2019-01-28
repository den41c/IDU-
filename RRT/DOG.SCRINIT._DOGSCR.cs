using System;
using System.Collections.Generic;
using ITnet2.Server.BusinessLogic.Core.Clobbi;
using ITnet2.Server.Controls;
using ITnet2.Server.Data;
using ITnet2.Server.Data.Tables;
using ITnet2.Server.Dialogs.InputFormCore;
using ITnet2.Server.Runtime.Methods;
using ITnet2.Server.Session;
using ITnet2.Server.UserBusinessLogic.Resources;


/// <summary>
/// Бізнес-логіка методу _DOGSCR
/// </summary>
public class RrtMethodLogic : ScreenStartMethodBusinessLogic
{

	private CodeNameBoxWrapper<int> _org
	{
		get { return ScreenControls.GetControl("ORG") as CodeNameBoxWrapper<int>; }
	}

	private CodeNameBoxWrapper<int> _ceh
	{
		get { return ScreenControls.GetControl("CEH") as CodeNameBoxWrapper<int>; }
	}
	private TextBox<System.DateTime> _ddogp
	{
		get { return ScreenControls.GetControl("DDOGN") as TextBox<System.DateTime>; }
	}
	private TextBox<System.DateTime> _date_p
	{
		get { return ScreenControls.GetControl("DATE_P") as TextBox<System.DateTime>; }
	}

	private CodeNameBoxWrapper<int> _vp_z_
	{
		get { return ScreenControls.GetControl("VP_Z_") as CodeNameBoxWrapper<int>; }
	}
	private Label _vp_z_label
	{
		get { return ScreenControls.GetControl("LABELVPZ") as Label; }
	}
	private CodeNameBoxWrapper<int> _vp_i_
	{
		get { return ScreenControls.GetControl("VP_I_") as CodeNameBoxWrapper<int>; }
	}
	private Label _vp_i_label
	{
		get { return ScreenControls.GetControl("LABELVPI") as Label; }
	}

	private CodeNameBoxWrapper<string> _kdgt
	{
		get { return ScreenControls.GetControl("KDGT") as CodeNameBoxWrapper<string>; }
	}

	private CheckBox<bool> _isAddPact
	{
		get { return ScreenControls.GetControl("ISADDPACT") as CheckBox<bool>; }
	}

	private Label _kdog_Label
	{
		get { return ScreenControls.GetControl("KDOG_LABEL") as Label; }
	}

    private CodeNameBoxWrapper<Int32> _undogOsn
    {
        get { return ScreenControls.GetControl("UNDOGOSN_") as CodeNameBoxWrapper<Int32>; }
    }


    private CodeNameBoxWrapper<string> _kzajnpp
    {
        get { return ScreenControls.GetControl("KZAJNPP_") as CodeNameBoxWrapper<string>; }
    }

    private CodeNameBoxWrapper<string> _n_kdk_ved
    {
        get { return ScreenControls.GetControl("N_KDK_VED_") as CodeNameBoxWrapper<string>; }
    }

    private CodeNameBoxFilter _filter;
	

	public override void Call()
	{
		_filter = _kzajnpp.AddFilter(new SqlCmdText());
		//_kzajnpp.When += _kzajnpp_When;
		_kzajnpp.ValueChanged += _kzajnpp_Valid;
		//_kzajnpp.VarHelp = "KZAJNPPNOCOND";
		_date_p.ValueChanged += _date_p_ValueChanged;
		//_kdgt.ValueChanged += _kdgt_ValueChanged;
		//_kdgt_ValueChanged();
		_isAddPact.ValueChanged += _isAddPact_ValueChanged;
		_isAddPact_ValueChanged();
        Cursor.ReRead();
	}

	void _isAddPact_ValueChanged()
	{
		_kdog_Label.Text = !_isAddPact.Value
			? Resources.GetResourceByName("ContractNumberLabelName")
			: Resources.GetResourceByName("AddPacktNumberLabelName");
	}

	void _kdgt_ValueChanged()
	{
		var prVp = SqlClient.Main.CreateCommand("select PR_VP_ from DGT where KDGT = @kdgt", new SqlParam("kdgt",_kdgt.Value)).ExecScalar<string>();
		_vp_z_.Visible = _vp_z_label.Visible = _vp_i_.Visible = _vp_i_label.Visible = prVp == "+";
	}

	void _date_p_ValueChanged()
	{
		if (_ddogp.Value == DateTime.MinValue)
		{
			_ddogp.Value = _date_p.Value;
		}
	}

	protected bool _kzajnpp_When()
	{
		var cmd = new List<SqlCmdText>();
		
		if (_org.Value != 0)
		{
			cmd.Add(new SqlCmdText("ZAE.ORG=@org or ZAE.ORG is null", new SqlParam("org", _org.Value)));
		}
		if (_ceh.Value != 0)
		{
			//cmd.Add(new SqlCmdText("ZAE.CEH_K=@ceh", new SqlParam("ceh", _ceh.Value)));
		}
		_filter.Filter = SqlCmdText.ConcatCommands(cmd.ToArray());	
		
		return true;
	}
	protected void _kzajnpp_Valid()
	{

        var zaeRecord = ZaeTable.GetRecord(_kzajnpp.Value, new[] { "ORG", "CEH_K", "N_KDK_M", "UNDOG", "UNDOGDS" });
		if (!zaeRecord.IsEmpty())
		{
            _undogOsn.Value = zaeRecord.Undog;
			//_org.Value = zaeRecord.Org != 0 ? zaeRecord.Org : _org.Value;
			//_ceh.Value = zaeRecord.Ceh_k != 0 ? zaeRecord.Ceh_k : _ceh.Value;
			_n_kdk_ved.Value = zaeRecord.N_kdk_m; //!= string.Empty ? zaeRecord.N_kdk_m : _n_kdk_ved.Value;
		}
	}

}