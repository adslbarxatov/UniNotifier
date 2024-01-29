using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс предоставляет доступ к шаблонам оповещений
	/// </summary>
	public class NotificationsTemplatesProvider: IDisposable
		{
		// Переменные и константы
		private List<string[]> templatesElements = new List<string[]> ();
		private const string externalTemplatesSubkey = "ExternalTemplates";
		private const string externalTemplatesVersionSubkey = "ExternalTemplatesVersion";
		private const string listLink = RDGenerics.DefaultGitLink + ProgramDescription.AssemblyMainName +
			"/blob/master/TemplatesList.md";
		private char[] fileTemplateSplitter = new char[] { '\t' };

		/// <summary>
		/// Разделитель элементов в шаблоне уведомления, передаваемом через буфер обмена
		/// </summary>
		public static char[] ClipboardTemplateSplitter = new char[] { '|' };

		/// <summary>
		/// Конструктор. Инициализирует список шаблонов
		/// </summary>
		/// <param name="FullyInitializeTemplates">Флаг указывает, следует ли полностью инициализировать 
		/// шаблоны в данном экземпляре</param>
		public NotificationsTemplatesProvider (bool FullyInitializeTemplates)
			{
			// Получение встроенных шаблонов и попытка получения внешних шаблонов
#if !ANDROID
			byte[] s = Properties.GMJNotifier.Templates;
			if (FullyInitializeTemplates)
				RDGenerics.RunWork (TemplatesListLoader, null, null, RDRunWorkFlags.DontSuspendExecution);

#else
			byte[] s = Properties.Resources.Templates;
			if (FullyInitializeTemplates)
				TemplatesListLoader ();
#endif
			string buf = RDGenerics.GetEncoding (RDEncodings.UTF8).GetString (s);

			// Получение загруженных шаблонов
			string buf2 = RDGenerics.GetAppSettingsValue (externalTemplatesSubkey);
			buf += buf2;

			// Разбор
			StringReader SR = new StringReader (buf);

			// Формирование массива 
			string str = "";
			while ((str = SR.ReadLine ()) != null)
				{
				string[] values = str.Split (fileTemplateSplitter, StringSplitOptions.RemoveEmptyEntries);
				if (values.Length != 5)
					continue;

				templatesElements.Add (new string[] { values[0], values[1], values[2], values[3], values[4] });
				}

			// Завершено
			}

		/// <summary>
		/// Возвращает название шаблона по его номеру
		/// </summary>
		/// <param name="TemplateNumber">Номер шаблона</param>
		/// <returns>Название</returns>
		public string GetName (uint TemplateNumber)
			{
			return GetTemplateElement (TemplateNumber, 0);
			}

		/// <summary>
		/// Возвращает ссылку шаблона по его номеру
		/// </summary>
		/// <param name="TemplateNumber">Номер шаблона</param>
		/// <returns>Ссылка на веб-страницу</returns>
		public string GetLink (uint TemplateNumber)
			{
			return GetTemplateElement (TemplateNumber, 1);
			}

		/// <summary>
		/// Возвращает начало шаблона по его номеру
		/// </summary>
		/// <param name="TemplateNumber">Номер шаблона</param>
		/// <returns>Начало шаблона</returns>
		public string GetBeginning (uint TemplateNumber)
			{
			return GetTemplateElement (TemplateNumber, 2);
			}

		/// <summary>
		/// Возвращает окончание шаблона по его номеру
		/// </summary>
		/// <param name="TemplateNumber">Номер шаблона</param>
		/// <returns>Окончание шаблона</returns>
		public string GetEnding (uint TemplateNumber)
			{
			return GetTemplateElement (TemplateNumber, 3);
			}

		/// <summary>
		/// Возвращает номер вхождения шаблона от начала страницы
		/// </summary>
		/// <param name="TemplateNumber">Номер шаблона</param>
		/// <returns>Номер вхождения</returns>
		public uint GetOccurrenceNumber (uint TemplateNumber)
			{
			if (templatesElements == null)
				return 1;

			uint res = 1;
			try
				{
				res = uint.Parse (templatesElements[(int)TemplateNumber][4]);
				}
			catch
				{
				throw new Exception ("Invalid internal method call. Debug required");
				}

			return res;
			}

		private string GetTemplateElement (uint TemplateNumber, uint ElementNumber)
			{
			if (templatesElements == null)
				return "";

			if ((TemplateNumber > templatesElements.Count) || (ElementNumber > templatesElements[0].Length))
				throw new Exception ("Invalid internal method call. Debug required");

			return templatesElements[(int)TemplateNumber][(int)ElementNumber];
			}

		/// <summary>
		/// Возвращает флаг, указывающий на необходимость дополнения шаблона перед применением
		/// </summary>
		/// <param name="TemplateNumber">Номер шаблона</param>
		public bool IsTemplateIncomplete (uint TemplateNumber)
			{
			return (GetLink (TemplateNumber).Contains ("{") ||
				GetBeginning (TemplateNumber).Contains ("{") ||
				GetEnding (TemplateNumber).Contains ("{"));
			}

		/// <summary>
		/// Возвращает количество доступных шаблонов
		/// </summary>
		public uint TemplatesCount
			{
			get
				{
				if (templatesElements == null)
					return 0;

				return (uint)templatesElements.Count;
				}
			}

		/// <summary>
		/// Метод освобождает ресурсы, занятые данным экземпляром
		/// </summary>
		public void Dispose ()
			{
			templatesElements.Clear ();
			templatesElements = null;
			}

		/// <summary>
		/// Получение списка шаблонов
		/// </summary>
#if ANDROID
		public async Task<bool> TemplatesListLoader ()
#else
		private void TemplatesListLoader (object sender, DoWorkEventArgs e)
#endif
			{
			// Запрос списка пакетов
#if ANDROID
			string html = await RDGenerics.GetHTML (listLink);
			if (html == "")
				return false;
#else
			string html = RDGenerics.GetHTML (listLink);
			if (html == "")
				{
				if (e != null)
					e.Result = -1;
				return;
				}
#endif

			// Разбор
			int left, right;
			if (((left = html.IndexOf ("<code>")) < 0) || ((right = html.IndexOf ("</code>", left)) < 0))
				{
#if ANDROID
				return false;
#else
				if (e != null)
					e.Result = -2;
				return;
#endif
				}
			html = html.Substring (left + 6, right - left - 6);

			// Получение списка
			StringReader SR = new StringReader (html);
			string newVersion = SR.ReadLine ();
			string oldVersion = RDGenerics.GetAppSettingsValue (externalTemplatesVersionSubkey);

			if (oldVersion == newVersion)
				{
#if ANDROID
				return false;
#else
				if (e != null)
					e.Result = 1;
				return;
#endif
				}

			// Интерпретация (удаление лишних элементов)
			string tmp = "", str;
			while ((str = SR.ReadLine ()) != null)
				{
				string[] values = str.Split (fileTemplateSplitter, StringSplitOptions.RemoveEmptyEntries);
				if (values.Length != 5)
					continue;

				for (int i = 0; i < 4; i++)
					tmp += (values[i] + fileTemplateSplitter[0].ToString ());
				tmp += (values[4] + RDLocale.RN);
				}

			// Запись
			tmp = tmp.Replace ("&lt;", "<").Replace ("&gt;", ">");

			RDGenerics.SetAppSettingsValue (externalTemplatesVersionSubkey, newVersion);
			RDGenerics.SetAppSettingsValue (externalTemplatesSubkey, tmp);

			// Завершено
			SR.Close ();
			reloadRequired = true;
#if ANDROID
			return true;
#else
			if (e != null)
				e.Result = 0;
#endif
			}

		/// <summary>
		/// Возвращает флаг, указывающий на необходимость перезагрузки списка шаблонов после обновления
		/// </summary>
		public bool ReloadRequired
			{
			get
				{
				return reloadRequired;
				}
			}
		private bool reloadRequired = false;
		}
	}
