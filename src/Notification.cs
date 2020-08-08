using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает состав и функционал отдельного оповещения
	/// </summary>
	public class Notification
		{
		/// <summary>
		/// Максимальное количество напоминаний
		/// </summary>
		public const uint MaxNotifications = 20;

		/// <summary>
		/// Возвращает название оповещения
		/// </summary>
		public string Name
			{
			get
				{
				return name;
				}
			}
		private string name = "";

		/// <summary>
		/// Возвращает ссылку на отслеживаемый ресурс
		/// </summary>
		public string Link
			{
			get
				{
				return link;
				}
			}
		private string link = "";

		/// <summary>
		/// Возвращает признак начала искомого участка страницы
		/// </summary>
		public string Beginning
			{
			get
				{
				return beginning;
				}
			}
		private string beginning = "";

		/// <summary>
		/// Возвращает признак окончания искомого участка страницы
		/// </summary>
		public string Ending
			{
			get
				{
				return ending;
				}
			}
		private string ending = "";

		// Разделитель хранимых строк параметров
		private static char[] splitter = new char[] { '\x9' };

		/// <summary>
		/// Возвращает множитель частоты обновления оповещения
		/// </summary>
		public uint UpdateFrequency
			{
			get
				{
				return updateFrequency;
				}
			}
		private uint updateFrequency = 3;	// Предполагается раз в 15 минут (3 х 5 минут – шаг таймера)

		/// <summary>
		/// Возвращает или устанавливает флаг, указывающий на активность оповещения
		/// </summary>
		public bool IsEnabled
			{
			get
				{
				return isEnabled && isInited;
				}
			set
				{
				isEnabled = value;
				}
			}
		private bool isEnabled = false;

		/// <summary>
		/// Возвращает флаг, указывающий на успешную инициализацию оповещения
		/// </summary>
		public bool IsInited
			{
			get
				{
				return isInited;
				}
			}
		private bool isInited = false;

		/// <summary>
		/// Конструктор. Инициализирует оповещение
		/// </summary>
		/// <param name="SourceLink">Ссылка на отслеживаемый ресурс</param>
		/// <param name="WatchAreaBeginningSign">Признак начала отслеживаемого участка</param>
		/// <param name="WatchAreaEndingSign">Признак окончания отслеживаемого участка</param>
		/// <param name="UpdatingFrequency">Множитель частоты обновления</param>
		/// <param name="NotificationName">Название оповещения</param>
		public Notification (string NotificationName, string SourceLink, string WatchAreaBeginningSign,
			string WatchAreaEndingSign, uint UpdatingFrequency)
			{
			// Контроль
			try
				{
				HttpWebRequest rq = (HttpWebRequest)WebRequest.Create (SourceLink);
				}
			catch
				{
				return;	// Отмена загрузки при наличии ошибок в адресе ссылки
				}

			if ((WatchAreaBeginningSign == null) || (WatchAreaBeginningSign == "") ||
				(WatchAreaEndingSign == null) || (WatchAreaEndingSign == "") ||
				(NotificationName == null) || (NotificationName == ""))
				return;

			// Настройка безопасности соединения
			ServicePointManager.SecurityProtocol = (SecurityProtocolType)0xFC0;
			// Принудительно открывает TLS1.0, TLS1.1 и TLS1.2; блокирует SSL3

			// Инициализация
			link = SourceLink;
			beginning = WatchAreaBeginningSign;
			ending = WatchAreaEndingSign;
			updateFrequency = UpdatingFrequency;
			if (updateFrequency == 0)
				updateFrequency++;
			updatesCounter = updateFrequency;	// Первое обновление принудительное
			name = NotificationName;

			// Завершено
			isInited = isEnabled = true;
			}

		/// <summary>
		/// Метод загружает оповещения из реестра или хранилища настроек
		/// </summary>
		/// <returns>Список сформированных оповещений</returns>
		public static List<Notification> LoadNotifications ()
			{
			// Переменные
			List<Notification> notifications = new List<Notification> ();
			uint lineNumber = 1;

			// Выгрузка
			while (notifications.Count < MaxNotifications)
				{
				// Запрос
				string line = "";
				try
					{
#if ANDROID
					line = Preferences.Get ("Not" + lineNumber.ToString ("D03"), "");
#else
					line = Registry.GetValue (ProgramDescription.AssemblySettingsKey,
						"Not" + lineNumber.ToString ("D03"), "").ToString ();
#endif
					}
				catch
					{
					break;
					}
				if (line == "")
					break;

				// Разбор
				lineNumber++;
				string[] values = line.Split (splitter, StringSplitOptions.RemoveEmptyEntries);

				if (values.Length != 6)
					continue;
				uint updatingFrequency = 3;
				bool isEnabled = false;
				try
					{
					updatingFrequency = uint.Parse (values[4]);
					isEnabled = bool.Parse (values[5]);
					}
				catch
					{
					continue;
					}

				// Формирование
				notifications.Add (new Notification (values[0], values[1], values[2], values[3], updatingFrequency));
				notifications[notifications.Count - 1].IsEnabled = isEnabled;
				}

			// Завершение
			return notifications;
			}

		/// <summary>
		/// Метод сохраняет оповещения в реестр или хранилище настроек
		/// </summary>
		/// <param name="Notifications">Список активных оповещений</param>
		public static void SaveNotifications (List<Notification> Notifications)
			{
			// Контроль
			if (Notifications == null)
				return;

			// Сохранение
			uint lineNumber = 1;
			for (int i = 0; i < Notifications.Count; i++)
				{
				// Контроль
				if (!Notifications[i].IsInited)
					continue;

				// Сборка
				string s = Notifications[i].Name + splitter[0].ToString () +
					Notifications[i].Link + splitter[0].ToString () +
					Notifications[i].Beginning + splitter[0].ToString () +
					Notifications[i].Ending + splitter[0].ToString () +
					Notifications[i].UpdateFrequency.ToString () + splitter[0].ToString () +
					Notifications[i].IsEnabled.ToString ();

				// Запись
				try
					{
#if ANDROID
					Preferences.Set ("Not" + lineNumber.ToString ("D03"), s);
#else
					Registry.SetValue (ProgramDescription.AssemblySettingsKey,
						"Not" + lineNumber.ToString ("D03"), s);
#endif
					}
				catch
					{
					}
				lineNumber++;
				}

			// Забой пропущенных настроек
			for (uint i = lineNumber; i <= MaxNotifications; i++)
				{
				try
					{
#if ANDROID
					Preferences.Set ("Not" + i.ToString ("D03"), "");
#else
					Registry.SetValue (ProgramDescription.AssemblySettingsKey,
						"Not" + i.ToString ("D03"), "");
#endif
					}
				catch
					{
					}
				}
			}

		/// <summary>
		/// Возвращает последний успешно полученный участок текста
		/// </summary>
		public string CurrentText
			{
			get
				{
				string newText = currentText.Replace ("<br/>", "\r\n").Replace ("<br />", "\r\n").Replace ("</p>", "\r\n");

				int textLeft = 0, textRight = 0;
				/*while (((textLeft = newText.IndexOf ("<a")) >= 0) && ((textRight = newText.IndexOf ("</a>", textLeft)) >= 0))
					newText = newText.Replace (newText.Substring (textLeft, textRight - textLeft + 4), "");
				while (((textLeft = newText.IndexOf ("<span")) >= 0) && ((textRight = newText.IndexOf (">", textLeft)) >= 0))
					newText = newText.Replace (newText.Substring (textLeft, textRight - textLeft + 1), "");*/
				while (((textLeft = newText.IndexOf ("<")) >= 0) && ((textRight = newText.IndexOf (">", textLeft)) >= 0))
					newText = newText.Replace (newText.Substring (textLeft, textRight - textLeft + 1), "");

				return newText.Trim ();
				}
			}
		private string currentText = "";
		private uint updatesCounter = 0;

		/// <summary>
		/// Метод обновляет состояние оповещения
		/// </summary>
		/// <returns>Возвращает true в случае успеха</returns>
		public bool Update ()
			{
			// Контроль
			if (!IsEnabled)	// Запрашивает суммарное состояние оповещения
				return false;

			if (updateFrequency <= ++updatesCounter)
				updatesCounter = 0;
			else
				return false;

			// Запрос обновлений
			HttpWebRequest rq = (HttpWebRequest)WebRequest.Create (link);
			rq.Method = "GET";
			rq.KeepAlive = false;
			rq.Timeout = 10000;

			// Отправка запроса
			HttpWebResponse resp = null;
			string html = "";
			try
				{
				resp = (HttpWebResponse)rq.GetResponse ();
				}
			catch
				{
				// Любая ошибка здесь будет означать необходимость прекращения проверки
				return false;
				}

			// Чтение ответа
			StreamReader SR = new StreamReader (resp.GetResponseStream (), Encoding.UTF8);
			html = SR.ReadToEnd ();
			SR.Close ();
			resp.Close ();

			// Разбор ответа
			int textLeft = 0, textRight = 0;
			if (((textLeft = html.IndexOf (beginning)) < 0) || ((textRight = html.IndexOf (ending, textLeft + beginning.Length)) < 0))
				{
				return false;
				}

			// Получение ID 
			textLeft += beginning.Length;

			// Получение и обработка текста
			string newText = html.Substring (textLeft, textRight - textLeft);

			// Контроль
			if (currentText != newText)
				{
				currentText = newText;
				return true;
				}

			return false;
			}
		}
	}
