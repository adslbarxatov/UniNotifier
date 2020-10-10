using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс предоставляет доступ к шаблонам оповещений
	/// </summary>
	public class NotificationsTemplatesProvider:IDisposable
		{
		// Переменные
		private List<string[]> templatesElements = new List<string[]> ();

		/// <summary>
		/// Конструктор. Инициализирует список шаблонов
		/// </summary>
		public NotificationsTemplatesProvider ()
			{
			// Получение файла
#if !ANDROID
			byte[] s = Properties.GMJNotifier.Templates;
#else
			byte[] s = Properties.Resources.Templates;
#endif
			string buf = Encoding.UTF8.GetString (s);
			StringReader SR = new StringReader (buf);

			// Разбор
			char[] splitters = new char[] { '\x9' };

			// Формирование массива 
			string str = "";
			while ((str = SR.ReadLine ()) != null)
				{
				string[] values = str.Split (splitters, StringSplitOptions.RemoveEmptyEntries);
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
		}
	}
