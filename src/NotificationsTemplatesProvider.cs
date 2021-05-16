#if ANDROID
	using Xamarin.Essentials;
#else
	using Microsoft.Win32;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

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
		private const string listLink = "https://github.com/adslbarxatov/UniNotifier/blob/master/TemplatesList.md";
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
				{
				HardWorkExecutor hwe = new HardWorkExecutor (TemplatesListLoader, null, null);
				}
#else
			byte[] s = Properties.Resources.Templates;
			if (FullyInitializeTemplates)
				{
				TemplatesListLoader (null, null);
				}
#endif
			string buf = Encoding.UTF8.GetString (s);

			// Получение загруженных шаблонов
			string buf2 = "";
			try
				{
#if ANDROID
				buf2 = Preferences.Get (externalTemplatesSubkey, "");
#else
				buf2 = Registry.GetValue (ProgramDescription.AssemblySettingsKey, externalTemplatesSubkey, "").ToString ();
#endif
				}
			catch
				{
				}
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

		// Получение списка шаблонов
		private void TemplatesListLoader (object sender, DoWorkEventArgs e)
			{
			// Запрос списка пакетов
#if ANDROID
			string html = NotificationsSupport.GetHTML (listLink);
#else
			string html = AboutForm.GetHTML (listLink);
#endif
			if (html == "")
				{
				if (e != null)
					e.Result = -1;
				return;
				}

			// Разбор
			int left, right;
			if (((left = html.IndexOf ("<code>")) < 0) || ((right = html.IndexOf ("</code>", left)) < 0))
				{
				if (e != null)
					e.Result = -2;
				return;
				}
			html = html.Substring (left + 6, right - left - 6);

			// Получение списка
			StringReader SR = new StringReader (html);
			string newVersion = SR.ReadLine ();
			string oldVersion = "";
#if ANDROID
			oldVersion = Preferences.Get (externalTemplatesVersionSubkey, "");
#else
			oldVersion = Registry.GetValue (ProgramDescription.AssemblySettingsKey, externalTemplatesVersionSubkey,
				"").ToString ();
#endif
			if (oldVersion == newVersion)
				{
				if (e != null)
					e.Result = 1;
				return;
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
				tmp += (values[4] + "\n");
				}

			// Запись
			tmp = tmp.Replace ("&lt;", "<").Replace ("&gt;", ">");
			try
				{
#if ANDROID
				Preferences.Set (externalTemplatesVersionSubkey, newVersion);
				Preferences.Set (externalTemplatesSubkey, tmp);
#else
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, externalTemplatesVersionSubkey, newVersion);
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, externalTemplatesSubkey, tmp);
#endif
				}
			catch
				{
				}

			// Завершено
			SR.Close ();
			reloadRequired = true;
			if (e != null)
				e.Result = 0;
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
