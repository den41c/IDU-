//Опишите свой класс и его методы ниже. Данная сборка будет доступна в namespace ITnet2.Server.UserBusinessLogic._Dump.

using ITnet2.Server.Session;
using Newtonsoft.Json;

public static class ExtensionMethods
{
	/// <summary>
	/// This is a simple (and lazy, read: effective) solution. Simply send your
	/// object to Newtonsoft serialize method, with the indented formatting, and
	/// you have your own Dump() extension method.
	/// </summary>
	/// <typeparam name="T">The object Type</typeparam>
	/// <param name="anObject">The object to dump</param>
	/// <param name="aTitle">Optional, will print this before the dump.</param>
	/// <returns>The object as you passed it</returns>
	public static T Dump<T>(this T anObject, string aTitle = "")
	{
		var pretty_json = JsonConvert.SerializeObject(anObject, Formatting.Indented);

		if (aTitle != "")
		{
			//System.Diagnostics.Debug.WriteLine(aTitle + ": "); //Console.Out.WriteLine(aTitle + ": ");
		}

		//System.Diagnostics.Debug.WriteLine(pretty_json.Replace("\"", "'"));//Console.Out.WriteLine(pretty_json);

		InfoManager.MessageBox(new MessageBoxParams(pretty_json.Replace("\"", "'"))
		{
			Caption = aTitle
		});

		return anObject;
	}
}