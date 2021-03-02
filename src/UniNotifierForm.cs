using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму приложения
	/// </summary>
	public partial class UniNotifierForm: Form
		{
		// Переменные
		private NotifyIcon ni = new NotifyIcon ();
		private SupportedLanguages al = Localization.CurrentLanguage;
		private CultureInfo ci;
		private DateTime lastNotStamp = new DateTime (2000, 1, 1, 0, 0, 0);

		private bool allowExit = false;
		private string[] regParameters = new string[] { "Left", "Top", "Width", "Height", "Read" };

		private NotificationsSet ns = new NotificationsSet (true);

		private string startupLink = Environment.GetFolderPath (Environment.SpecialFolder.CommonStartup) + "\\" +
			ProgramDescription.AssemblyMainName + ".lnk";

		private List<string> texts = new List<string> ();
		private List<int> notNumbers = new List<int> ();

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
			ResetCulture ();
			GetGMJ.Visible = (al == SupportedLanguages.ru_ru);

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

			if (!File.Exists (startupLink))
				ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption05", al), AddToStartup));
			}

		private void UniNotifierForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			this.Hide ();

			// Запуск
			MainTimer.Interval = (int)ProgramDescription.MasterTimerDelay;
			MainTimer.Enabled = true;
			}

		// Обновление списка оповещений в главном окне
		private void ReloadNotificationsList ()
			{
			NamesCombo.Items.Clear ();
			for (int i = 0; i < ns.Notifications.Count; i++)
				NamesCombo.Items.Add (ns.Notifications[i].Name);
			for (int i = 0; i < ns.SpecialNotifications.Count; i++)
				NamesCombo.Items.Add (ns.SpecialNotifications[i].Name);

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

		// Установка текущей культуры представления даты
		private void ResetCulture ()
			{
			try
				{
				if (al == SupportedLanguages.ru_ru)
					ci = new CultureInfo ("ru-ru");
				else
					ci = new CultureInfo ("en-us");
				}
			catch
				{
				ci = CultureInfo.InstalledUICulture;
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

				// Освобождение ресурсов
				ns.Dispose ();
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

		// Добавление в автозапуск
		private void AddToStartup (object sender, EventArgs e)
			{
			// Попытка создания
			WindowsShortcut.CreateStartupShortcut (Application.ExecutablePath, ProgramDescription.AssemblyMainName, "");

			// Контроль
			ni.ContextMenu.MenuItems[ni.ContextMenu.MenuItems.Count - 1].Enabled = !File.Exists (startupLink);
			}

		// Итерация таймера обновления
		private void MainTimer_Tick (object sender, EventArgs e)
			{
			// Запуск запроса
			HardWorkExecutor hwe = new HardWorkExecutor (DoUpdate, null, null);
			hwe.Dispose ();

			// Обновление очереди отображения
			if (texts.Count > 0)
				{
				// Добавление в главное окно
				if (MainText.Text.Length + texts[0].Length > ProgramDescription.MasterLogMaxLength)
					MainText.Text = MainText.Text.Substring (texts[0].Length, MainText.Text.Length - texts[0].Length);
				if (MainText.Text.Length > 0)
					MainText.AppendText ("\r\n\r\n");
				if (DateTime.Today > lastNotStamp)
					{
					lastNotStamp = DateTime.Today;
					MainText.AppendText ("\r\n--- " + DateTime.Today.ToString (ci.DateTimeFormat.LongDatePattern, ci) +
						" ---\r\n\r\n");
					}
				MainText.AppendText (texts[0]);

				// Отображение всплывающего сообщения
				if (!this.Visible)
					{
					if (texts[0].Length > 210)
						texts[0] = texts[0].Substring (0, 210) + "...";

					ni.ShowBalloonTip (10000, "", texts[0], ToolTipIcon.Info);
					}

				// Обновление прочих полей
				NamesCombo.SelectedIndex = notNumbers[0];

				texts.RemoveAt (0);
				notNumbers.RemoveAt (0);

				GetGMJ.Enabled = true;
				}
			}

		private void DoUpdate (object sender, DoWorkEventArgs e)
			{
			string newText = ns.GetNextNotification ();
			if (newText != "")
				{
				texts.Add (newText);
				notNumbers.Add (ns.CurrentNotificationNumberInList);
				}

			e.Result = null;
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
			ResetCulture ();
			GetGMJ.Visible = (al == SupportedLanguages.ru_ru);

			for (int i = 0; i < ni.ContextMenu.MenuItems.Count; i++)
				ni.ContextMenu.MenuItems[i].Text = Localization.GetText ("MainMenuOption" + (i + 1).ToString ("D02"), al);

			// Перезапуск
			ns.ResetTimer (true);
			MainTimer.Enabled = true;
			}

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			try
				{
				if (NamesCombo.SelectedIndex < ns.Notifications.Count)
					Process.Start (ns.Notifications[NamesCombo.SelectedIndex].Link);
				else
					Process.Start (ns.SpecialNotifications[NamesCombo.SelectedIndex - ns.Notifications.Count].Link);
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

			BClose.Top = BGo.Top = ReadMode.Top = GetGMJ.Top = this.Height - 60;
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

		// Запрос сообщения от GMJ
		private void GetGMJ_Click (object sender, EventArgs e)
			{
			GetGMJ.Enabled = false;
			string s = GMJ.GetRandomGMJ ();

			if (s != "")
				texts.Add (s);
			else
				texts.Add ("GMJ не вернула сообщение. Проверьте интернет-соединение");
			notNumbers.Add (0);
			}
		}
	}
