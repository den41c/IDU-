//������� ���� ����� � ��� ������ ����. ������ ������ ����� �������� � namespace ITnet2.Server.UserBusinessLogic.Resources.

using System;
using System.Collections.Generic;
using ITnet2.Server.BusinessLogic.FI.AccountingRegisters.EntriesCreation.Algorithms;
using ITnet2.Server.Controls;
using ITnet2.Server.Data;
using ITnet2.Server.Dialogs.InputFormCore;
using ITnet2.Server.Session;

public static class Resources
{
	private static Dictionary<string, Dictionary<string, string>> _res =
		new Dictionary<string, Dictionary<string, string>>() {
			{
				"ContractNumberLabelName",
				new Dictionary<string, string>() {
					{ "RU", "� ����" },
					{ "UK", "� ������" },
					{ "EN", "� work" }
				}
			}, {
				"AddPacktNumberLabelName",
				new Dictionary<string, string>() {
					{ "RU", "� ���. ����������" },
					{ "UK", "� ���. �����" },
					{ "EN", "� add. pact" }
				}
			}, {
				"_SUMSCR_DOG_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� �������" },
					{ "RU", "����� ��������" },
					{ "EN", "" }
				}
			}, {
				"DMS_KMATP_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "��� �������/�������" },
					{ "RU", "��� �������/������" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_DO�_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� ���������" },
					{ "RU", "����� ���������" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_YM_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "̳���� �� ��" },
					{ "RU", "����� � ���" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_SUM_ALL_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "���� ��������" },
					{ "RU", "����� ��������" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_SUM_OWN_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����������������" },
					{ "RU", "����������������" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_SUM_PR_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "��������" },
					{ "RU", "�������" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_ST_N_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� �����" },
					{ "RU", "����� �����" },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_ST_N_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� �����" },
					{ "RU", "����� �����" },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_DOG_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� ��������" },
					{ "RU", "����� ��������" },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_DOC_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� ���������" },
					{ "RU", "����� ���������" },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_Y_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "г� " },
					{ "RU", "��� " },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_S_CALC_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "������ �����������" },
					{ "RU", "������ �����������" },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_SUM_CALC_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "���� �����������" },
					{ "RU", "����� �����������" },
					{ "EN", "" }
				}
			}, {
				"_ESTISCR_L_LINE",
				new Dictionary<string, string>() {
					{ "UK", "��������ֲ�" },
					{ "RU", "�����������" },
					{ "EN", "" }
				}
			}, {
				"_SUMSCR_L_LINE",
				new Dictionary<string, string>() {
					{ "UK", "�����Ĳ� ���" },
					{ "RU", "������������� ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_SUM_ALL_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "���� ����" },
					{ "RU", "����� ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_SUM_OWN_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� ����" },
					{ "RU", "������. ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_SUM_SUB_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "������. ����" },
					{ "RU", "�������� ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_SUM_ALL_F_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "���� ����" },
					{ "RU", "����� ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_SUM_OWN_F_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����� ����" },
					{ "RU", "������. ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_SUM_SUB_F_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "������. ����" },
					{ "RU", "�������� ����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_YM_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "г� �� �����" },
					{ "RU", "��� � �����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_KZAJNPP_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����������" },
					{ "RU", "�����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_SUM_L_LINE",
				new Dictionary<string, string>() {
					{ "UK", "�����Ĳ� ���" },
					{ "RU", "������������� ���" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_EST_L_LINE",
				new Dictionary<string, string>() {
					{ "UK", "��������ֲ�" },
					{ "RU", "�����������" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_EST_KZAJNPP_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "����������" },
					{ "RU", "�����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_EST_YM_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "г� �� �����" },
					{ "RU", "��� � �����" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_EST_S_CALC_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "������ �����������" },
					{ "RU", "������ �����������" },
					{ "EN", "" }
				}
			}, {
				"_ZAE_EST_SUM_CALC_LABEL",
				new Dictionary<string, string>() {
					{ "UK", "���� �����������" },
					{ "RU", "C���� �����������" },
					{ "EN", "" }
				}
			},
		};


	public static string GetResourceByName(string name)
	{
		var ret = string.Empty;
		Dictionary<string, string> d;
		if (!_res.TryGetValue(name, out d))
		{
			return ret;
		}
		d.TryGetValue(Settings.Environment.ItLanguage, out ret);
		return ret;
	}

	public static void MakeActionWithControls(IEnumerable<ElementBase> controls, Action<ElementBase> action)
	{
		foreach (var control in controls)
		{
			action(control);
		}
	}

	public static void TranslateEverythingThatCouldBeTranslated(IEnumerable<ElementBase> Controls, string screenName)
	{
		foreach (var inputFormControl in Controls)
		{
			if (inputFormControl is Label)
			{
				var l = ((Label)inputFormControl);
				var newText = GetResourceByName(screenName + "_" + l.Name);
				l.Text = string.IsNullOrWhiteSpace(newText) ? l.Text : newText; 
			}
			else if (inputFormControl is Line)
			{
				var l = ((Line)inputFormControl);
				var newText = GetResourceByName(screenName + "_" + l.Name);
				l.Text = string.IsNullOrWhiteSpace(newText) ? l.Text : newText; 
			}
		}
	}

	public static string GetCorrectTranslate(string ua = "", string ru = "", string en = "")
	{
		switch (Settings.Environment.ItLanguage)
		{
			case "UA":
			{
				return	!string.IsNullOrWhiteSpace(ua) ? ua :
						!string.IsNullOrWhiteSpace(ru) ? ru :
						!string.IsNullOrWhiteSpace(en) ? en : ua;
			}
			case "RU":
			{
				return	!string.IsNullOrWhiteSpace(ru) ? ru : 
						!string.IsNullOrWhiteSpace(ua) ? ua :
						!string.IsNullOrWhiteSpace(en) ? en : ru;
			}
			case "EN":
			{
				return	!string.IsNullOrWhiteSpace(en) ? en :
						!string.IsNullOrWhiteSpace(ua) ? ua :
						!string.IsNullOrWhiteSpace(ru) ? ru : en;
			}
			default:
			{
				return ua;
			}
		}

	}

}