using Microsoft.Win32;
using System;
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
		// Переменные
		private NotifyIcon ni = new NotifyIcon ();
		private SupportedLanguages al = Localization.CurrentLanguage;

		private bool allowExit = false;
		private string[] regParameters = new string[] { "Left", "Top", "Width", "Height", "Read" };

		private NotificationsSet ns = new NotificationsSet ();

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

			ReloadNotificationsList ();

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
				}
			catch
				{
				}

			// Настройка иконки в трее
			ni.Icon = Properties.GMJNotifier.GMJNotifier16;
			ni.Visible = true;

			ni.ContextMenu = new ContextMenu ();

			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption01", al), ShowFullText));
			ni.DoubleClick += ShowFullText;
			ni.ContextMenu.MenuItems[0].DefaultItem = true;

			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption02", al), ShowSettings));
			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption03", al), AboutService));
			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption04", al), CloseService));
			}

		private void UniNotifierForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			this.Hide ();

			// Запуск
			MainTimer.Interval = 15 * 1000;
			MainTimer.Enabled = true;
			}

		// Обновление списка оповещений в главном окне
		private void ReloadNotificationsList ()
			{
			NamesCombo.Items.Clear ();
			for (int i = 0; i < ns.Notifications.Count; i++)
				NamesCombo.Items.Add (ns.Notifications[i].Name);

			if (NamesCombo.Items.Count > 0)
				{
				NamesCombo.Enabled = BGo.Enabled = true;
				NamesCombo.SelectedIndex = 0;
				}
			else
				{
				NamesCombo.Enabled = BGo.Enabled = false;
				}
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
			ProgramDescription.ShowAbout (false);
			}

		// Итерация таймера обновления
		private void MainTimer_Tick (object sender, EventArgs e)
			{
			// Обновление
			string newText;
			if ((newText = ns.GetNextNotification ()) != "")
				{
				// Отображение всплывающего сообщения
				if (!this.Visible)
					{
					if (newText.Length > 210)
						newText = newText.Substring (0, 210) + "...";

					ni.ShowBalloonTip (10000, "", newText, ToolTipIcon.Info);
					}

				// Добавление в главное окно
				if (MainText.Text.Length + newText.Length > 20000)
					MainText.Text = MainText.Text.Substring (newText.Length, MainText.Text.Length - newText.Length);
				if (MainText.Text.Length > 0)
					MainText.AppendText ("\r\n\r\n");
				MainText.AppendText (newText);

				// Обновление прочих полей
				NamesCombo.SelectedIndex = ns.CurrentNotificationNumber;
				}

			}

		// Отображение полного списка оповещений
		private void ShowFullText (object sender, EventArgs e)
			{
			this.Show ();
			}

		// Отображение окна настроек
		private void ShowSettings (object sender, EventArgs e)
			{
			// Пауза
			MainTimer.Enabled = false;

			// Настройка
			SettingsForm sf = new SettingsForm (ns,
				(uint)MainTimer.Interval * NotificationsSet.MaxNotifications / 60000);

			// Обновление настроек
			ReloadNotificationsList ();

			al = Localization.CurrentLanguage;
			for (int i = 0; i < ni.ContextMenu.MenuItems.Count; i++)
				ni.ContextMenu.MenuItems[i].Text = Localization.GetText ("MainMenuOption" + (i + 1).ToString ("D02"), al);

			// Перезапуск
			MainTimer.Enabled = true;
			}

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			try
				{
				Process.Start (ns.Notifications[NamesCombo.SelectedIndex].Link);
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
			// Изменение состояния
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

			// Запоминание
			try
				{
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[4], ReadMode.Checked.ToString ());
				}
			catch
				{
				}
			}

		// Изменение размера формы
		private void UniNotifierForm_Resize (object sender, EventArgs e)
			{
			MainText.Width = this.Width - 30;
			MainText.Height = this.Height - 80;

			BClose.Top = BGo.Top = ReadMode.Top = this.Height - 60;
			NamesCombo.Top = BClose.Top + 2;
			}

		// Сохранение размера формы
		private void UniNotifierForm_ResizeEnd (object sender, EventArgs e)
			{
			try
				{
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[0], this.Left.ToString ());
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[1], this.Top.ToString ());
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[2], this.Width.ToString ());
				Registry.SetValue (ProgramDescription.AssemblySettingsKey, regParameters[3], this.Height.ToString ());
				}
			catch
				{
				}
			}
		}
	}
