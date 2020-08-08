using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму приложения
	/// </summary>
	public partial class GMJNotifierForm:Form
		{
		// Индикатор обновлений
		private NotifyIcon ni = new NotifyIcon ();

		private string currentPostID = "",
			lastPostID = "";
		private uint currentOffset = 1;		// Перекрытие начального отображения
		private bool allowExit = false;
		private string helpShownAt = "";
		private const string articlePrefix = "\x13";

		private string[] regParameters = new string[] { "Left", "Top", "Width", "Height", "Read", "HelpShownAt" };

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		public GMJNotifierForm ()
			{
			// Инициализация
			InitializeComponent ();
			this.Text = ProgramDescription.AssemblyTitle;
			this.CancelButton = BClose;
			this.AcceptButton = BNext;
			MainText.Font = new Font (SystemFonts.DialogFont.FontFamily.Name, 13);

			// Получение настроек
			try
				{
				this.Left = int.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, regParameters[0],
					"").ToString ());
				this.Top = int.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, regParameters[1],
					"").ToString ());
				this.Width = int.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, regParameters[2],
					"").ToString ());
				this.Height = int.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, regParameters[3],
					"").ToString ());
				this.ReadMode.Checked = bool.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, regParameters[4],
					"").ToString ());
				helpShownAt = Registry.GetValue (ProgramDescription.AssemblySettingsKey, regParameters[5],
					"").ToString ();
				}
			catch
				{
				}
			ReadMode_CheckedChanged (null, null);

			// Настройка иконки в трее
			ni.Icon = Properties.GMJNotifier.GMJNotifier16;
			ni.Visible = true;

			ni.ContextMenu = new ContextMenu ();

			ni.ContextMenu.MenuItems.Add (new MenuItem ("Последнее обновление: при запуске в " + DateTime.Now.ToString ("HH:mm"), ShowFullText));
			ni.ContextMenu.MenuItems[0].Enabled = false;
			ni.ContextMenu.MenuItems.Add ("-");

			ni.ContextMenu.MenuItems.Add (new MenuItem ("Полный текст", ShowFullText));
			ni.DoubleClick += ShowFullText;
			ni.ContextMenu.MenuItems[2].DefaultItem = true;
			ni.ContextMenu.MenuItems.Add (new MenuItem ("Следующий пост", UpdateMessages));
			ni.ContextMenu.MenuItems.Add ("-");

			ni.ContextMenu.MenuItems.Add (new MenuItem ("Перейти на страницу сообщества", GoToCommunity));
			ni.ContextMenu.MenuItems.Add (new MenuItem ("О приложении", AboutService));
			ni.ContextMenu.MenuItems.Add (new MenuItem ("Закрыть", CloseService));

			// Запуск
			UpdateMsg (0);
			MainTimer.Interval = 15 * 60 * 1000;
			MainTimer.Enabled = true;
			}

		private void GMJNotifierForm_Shown (object sender, EventArgs e)
			{
			this.Hide ();

			if (helpShownAt != ProgramDescription.AssemblyVersion)
				AboutService (null, null);
			}

		// Завершение работы службы
		private void CloseService (object sender, EventArgs e)
			{
			allowExit = true;
			this.Close ();
			}

		private void GMJNotifierForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Остановка службы
			if (allowExit)
				{
				// Остановка
				ni.Visible = false;
				MainTimer.Enabled = false;

				// Сохранение настроек
				try
					{
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[0], this.Left.ToString ());
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[1], this.Top.ToString ());
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[2], this.Width.ToString ());
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[3], this.Height.ToString ());
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[4], this.ReadMode.Checked.ToString ());
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[5], ProgramDescription.AssemblyVersion);
					}
				catch
					{
					}
				}

			// Скрытие окна просмотра
			else
				{
				this.Hide ();
				e.Cancel = true;
				}
			}

		// О приложении
		private void AboutService (object sender, EventArgs e)
			{
			AboutForm af = new AboutForm (SupportedLanguages.ru_ru, "*", "*", "",
				"Данная служба предоставляет возможность получать новые записи сообщества Grammar must joy в виде " +
				"сообщений в трее.\r\n\r\n" +
				"Контекстное меню, вызываемое по правому щелчку на значке приложения в трее, позволяет управлять " +
				"просмотром постов, просматривать их в большом окне (в порядке от новых к старым) и запрашивать информацию " +
				"о приложении.\r\n\r\nКроме того, приложение раз в " + (MainTimer.Interval / 60000).ToString () + " минут самостоятельно " +
				"запрашивает новые записи и при их наличии отображает соответствующее оповещение.\r\n\r\n" +
				"Будьте всегда веселы и бодры! Вместе в GMJ");
			}

		// Обновление сообщений / переход к следующему посту
		private void UpdateMessages (object sender, EventArgs e)
			{
			ni.ContextMenu.MenuItems[0].Text = "Последнее обновление: вручную в " + DateTime.Now.ToString ("HH:mm");
			UpdateMsg (currentOffset++);

			if (this.Visible)
				CheckArticle ();
			}

		//Итерация таймера обновления
		private void MainTimer_Tick (object sender, EventArgs e)
			{
			ni.ContextMenu.MenuItems[0].Text = "Последнее обновление: таймер в " + DateTime.Now.ToString ("HH:mm");
			UpdateMsg (0);
			}

		// Главный метод запроса сообщений
		private void UpdateMsg (uint Offset)
			{
			// Настройка безопасности соединения
			ServicePointManager.SecurityProtocol = (SecurityProtocolType)0xFC0;
			// Принудительно открывает TLS1.0, TLS1.1 и TLS1.2; блокирует SSL3

			// Запрос обновлений
			HttpWebRequest rq = (HttpWebRequest)WebRequest.Create (string.Format (ProgramDescription.ArticleQuery, Offset));
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
				ni.ShowBalloonTip (3000, "- " + ProgramDescription.AssemblyTitle + " -",
					"\nСообщество Grammar must joy недоступно.\n\nПроверьте подключение к сети интернет", ToolTipIcon.Error);
				return;
				}

			// Чтение ответа
			StreamReader SR = new StreamReader (resp.GetResponseStream (), Encoding.UTF8);
			html = SR.ReadToEnd ();
			SR.Close ();
			resp.Close ();

			// Разбор ответа
			int idLeft = 0, idRight = 0,
				textLeft = 0, textRight = 0;
			if (((idLeft = html.IndexOf ("id")) < 0) || ((idRight = html.IndexOf (",", idLeft + 4)) < 0) ||
				((textLeft = html.IndexOf ("text")) < 0) || ((textRight = html.IndexOf ("\"", textLeft + 7)) < 0))
				{
				ni.ShowBalloonTip (3000, "- " + ProgramDescription.AssemblyTitle + " -",
					"\nВозможно, мы достигли конца новостной ленты.\n\nПопробуем начать сначала", ToolTipIcon.Info);
				currentOffset = 0;
				return;
				}

			// Получение ID 
			idLeft += 4;
			textLeft += 7;
			currentPostID = html.Substring (idLeft, idRight - idLeft);

			// Контроль
			if (Offset == 0)
				{
				if (lastPostID == currentPostID)
					return;
				else
					lastPostID = currentPostID;
				}

			// Получение текста
			MainText.Text = html.Substring (textLeft, textRight - textLeft).Replace ("\\n", "\r\n").Replace ("\\/", "/").
				Replace ("\\\\", "\\");

			// Отображение
			if (MainText.Text.Length < 1)
				{
				MainText.Text = articlePrefix + " Пост с номером " + currentPostID +
					" – это длинная история; откройте страницу сообщества, чтобы прочесть его";
				ni.ShowBalloonTip (3000, "- " + ProgramDescription.AssemblyTitle + " -", "\n" + MainText.Text, ToolTipIcon.Info);
				return;
				}

			// Отображение
			if (!this.Visible)
				ni.ShowBalloonTip (30000, "- " + ProgramDescription.AssemblyTitle + " -",
					"\n" + (MainText.Text.Length > 200 ? MainText.Text.Substring (0, 200) + "..." : MainText.Text), ToolTipIcon.None);
			}

		// Отображение полного текста
		private void ShowFullText (object sender, EventArgs e)
			{
			this.Show ();
			CheckArticle ();
			}

		// Метод проверяет, следует ли открыть статью в браузере
		private void CheckArticle ()
			{
			if (MainText.Text.StartsWith (articlePrefix) && (MessageBox.Show (this, "Открыть статью в браузере?",
				ProgramDescription.AssemblyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
				{
				try
					{
					Process.Start (ProgramDescription.ArticleLink + currentPostID);
					}
				catch
					{
					}
				}
			}

		// Переход на страницу сообщества
		private void GoToCommunity (object sender, EventArgs e)
			{
			try
				{
				Process.Start (ProgramDescription.MasterCommunityLink);
				}
			catch
				{
				}
			}

		// Изменение размера формы
		private void GMJNotifierForm_Resize (object sender, EventArgs e)
			{
			MainText.Width = this.Width - 30;
			MainText.Height = this.Height - 80;

			BClose.Top = BNext.Top = ReadMode.Top = this.Height - 60;
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Переход в режим чтения и обратно
		private void ReadMode_CheckedChanged (object sender, EventArgs e)
			{
			if (ReadMode.Checked)
				{
				MainText.ForeColor = Color.FromArgb (163, 163, 163);
				MainText.BackColor = Color.FromArgb (17, 17, 17);
				}
			else
				{
				MainText.ForeColor = Color.FromArgb (36, 36, 36);
				MainText.BackColor = Color.FromArgb (255, 255, 255);
				}
			}
		}
	}
