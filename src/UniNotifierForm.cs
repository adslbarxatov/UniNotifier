using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму приложения
	/// </summary>
	public partial class UniNotifierForm:Form
		{
		// Индикатор обновлений
		private NotifyIcon ni = new NotifyIcon ();

		private bool allowExit = false;
		private string helpShownAt = "", currentLink = "";

		private string[] regParameters = new string[] { "Left", "Top", "Width", "Height", "Read", "HelpShownAt" };

		private List<Notification> notifications;
		private int currentNotification = 0;

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		public UniNotifierForm ()
			{
			// Инициализация
			InitializeComponent ();
			this.Text = ProgramDescription.AssemblyTitle;
			this.CancelButton = BClose;
			MainText.Font = new Font (SystemFonts.DialogFont.FontFamily.Name, 13);

			notifications = Notification.LoadNotifications ();

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

			// Настройка иконки в трее
			ni.Icon = Properties.GMJNotifier.GMJNotifier16;
			ni.Visible = true;

			ni.ContextMenu = new ContextMenu ();

			ni.ContextMenu.MenuItems.Add (new MenuItem ("Посмотреть полностью", ShowFullText));
			ni.DoubleClick += ShowFullText;
			ni.ContextMenu.MenuItems[0].DefaultItem = true;
			ni.ContextMenu.MenuItems.Add (new MenuItem ("Перейти на страницу последнего оповещения", GoToLink));

			ni.ContextMenu.MenuItems.Add ("-");
			ni.ContextMenu.MenuItems.Add (new MenuItem ("Настройки оповещений", ShowSettings));
			ni.ContextMenu.MenuItems.Add (new MenuItem ("О приложении", AboutService));
			ni.ContextMenu.MenuItems.Add (new MenuItem ("Закрыть", CloseService));

			// Добавление первого оповещения
			if (helpShownAt == "")
				{
				notifications.Add (new Notification ("RD AAOW FUPL", "https://vk.com/rdaaow_fupl", "pi_text\">", "</div", 3));
				//notifications.Add (new Notification ("GMJ", "https://vk.com/grammarmustjoy", "pi_text\">", "</div", 3));
				//notifications.Add (new Notification ("GMJ", "https://vk.com/grammarmustjoy", "pi_text zoom_text\">", "</div", 3));
				}

			// Запуск
			MainTimer.Interval = 15 * 1000;
			MainTimer.Enabled = true;
			}

		private void UniNotifierForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			this.Hide ();

			// Отображение справки при смене версий
			if (helpShownAt != ProgramDescription.AssemblyVersion)
				AboutService (null, null);
			}

		// Завершение работы службы
		private void CloseService (object sender, EventArgs e)
			{
			allowExit = true;
			this.Close ();
			}

		private void UniNotifierForm_FormClosing (object sender, FormClosingEventArgs e)
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

				Notification.SaveNotifications (notifications);
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
				"Данная служба предоставляет возможность получать оповещения об изменении состояния отслеживаемых веб-страниц в виде " +
				"сообщений в трее.\r\n\r\n" +
				"Контекстное меню, вызываемое по правому щелчку на значке приложения в трее, позволяет управлять оповещениями " +
				"и конструировать их, просматривать их в большом окне и запрашивать информацию о приложении");
			}

		//Итерация таймера обновления
		private void MainTimer_Tick (object sender, EventArgs e)
			{
			// Обновление
			if ((currentNotification < notifications.Count) && notifications[currentNotification].Update ())
				{
				string newText = "- От " + notifications[currentNotification].Name + " в " + DateTime.Now.ToString ("HH:mm") +
					" -\r\n\r\n" + notifications[currentNotification].CurrentText;

				if (!this.Visible)
					{
					if (newText.Length < 210)
						ni.ShowBalloonTip (10000, "", newText, ToolTipIcon.Info);
					else
						ni.ShowBalloonTip (10000, "", newText.Substring (0, 210) + "...", ToolTipIcon.Info);
					}

				if (MainText.Text.Length + newText.Length > 20000)
					MainText.Text = MainText.Text.Substring (newText.Length, MainText.Text.Length - newText.Length);
				if (MainText.Text.Length > 0)
					MainText.AppendText ("\r\n\r\n");
				MainText.AppendText (newText);

				currentLink = notifications[currentNotification].Link;
				}

			if (++currentNotification >= Notification.MaxNotifications)
				currentNotification = 0;
			}

		// Отображение полного списка оповещений
		private void ShowFullText (object sender, EventArgs e)
			{
			this.Show ();
			}

		// Отображение окна настроек
		private void ShowSettings (object sender, EventArgs e)
			{
			SettingsForm sf = new SettingsForm (notifications,
				(uint)MainTimer.Interval * Notification.MaxNotifications / 60000);
			}

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			try
				{
				Process.Start (currentLink);
				}
			catch
				{
				}
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

		// Изменение размера формы
		private void UniNotifierForm_Resize (object sender, EventArgs e)
			{
			MainText.Width = this.Width - 30;
			MainText.Height = this.Height - 80;

			BClose.Top = BGo.Top = ReadMode.Top = this.Height - 60;
			}
		}
	}
