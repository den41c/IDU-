//Опишите свой класс и его методы ниже. Данная сборка будет доступна в namespace ITnet2.Server.UserBusinessLogic.Cache_Lib.

using System;
using System.Collections.Generic;
using ITnet2.Common.Tools;
using ITnet2.Server.BusinessLogic.Core.Analytics;
using ITnet2.Server.BusinessLogic.Runtime.Methods.KSM;
using ITnet2.Server.Data;
using ITnet2.Server.Data.Tables;
using ITnet2.Server.Session.SystemTools;

[Serializable]
public class AnalyticValue
{
	public Guid Uniqguid { get; set; }
	public string Code { get; set; }
}

public static class UserCacheManager
{
	private static Dictionary<string, Dictionary<string, object>> _cache;

	public static T Get<T>(string obj, string key)
	{
		if (!ContainesKey(obj, key))
		{
			return default(T);
		}
		return (T)_cache[obj][key];
	}

	public static void Set<T>(string obj, string key, T value)
	{
		if (!_cache.ContainsKey(obj))
		{
			_cache.Add(obj, new Dictionary<string, object>());
		}
		_cache[obj][key] = value;
	}

	public static bool ContainesKey(string obj, string key)
	{
		return _cache.ContainsKey(obj) && _cache[obj].ContainsKey(key);
	}

	static UserCacheManager()
	{
		_cache = new Dictionary<string, Dictionary<string, object>>();
	}

    public static void Clear()
    {
        _cache.Clear();
    }
}

public static class Org
{
    public static OrgTable.Record GetOrgByCode(int org)
    {
        if (!UserCacheManager.ContainesKey("USERCONTRACTORCACHE", Text.Convert(org)))
        {
            var orgRec = OrgTable.GetRecord(org);
            UserCacheManager.Set("USERCONTRACTORCACHE", Text.Convert(org), orgRec);
            return orgRec;
        }
        return UserCacheManager.Get<OrgTable.Record>("USERCONTRACTORCACHE", Text.Convert(org));
    }
}

public static class Pod
{
    public static PodTable.Record GetPodByRes(string res)
    {
        if (!UserCacheManager.ContainesKey("USERCEH_NAMECACHE", res))
        {
            var orgRec = PodTable.GetRecord(new SqlCmdText("CEH_NAME = @cehname and PR_DO = ''", new SqlParam("cehname", res)));
            UserCacheManager.Set("USERCEH_NAMECACHE", res, orgRec);
            return orgRec;
        }
        return UserCacheManager.Get<PodTable.Record>("USERCEH_NAMECACHE", Text.Convert(res));
    }
}