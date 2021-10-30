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
	public partial class UniNotifierForm:Form
		{
		// Переменные
		private NotifyIcon ni = new NotifyIcon ();
		private SupportedLanguages al = Localization.CurrentLanguage;
		private CultureInfo ci;

		private bool allowExit = false;
		private string[] regParameters = new string[] { "Left", "Top", "Width", "Height", "Read" };

		private NotificationsSet ns = new NotificationsSet (true);

		private string startupLink = Environment.GetFolderPath (Environment.SpecialFolder.CommonStartup) + "\\" +
			ProgramDescription.AssemblyMainName + ".lnk";

		private List<string> texts = new List<string> ();
		private List<int> notNumbers = new List<int> ();

#if TG
		private uint currentTGCount = 0;
		private DateTime currentTGTimeStamp = new DateTime (2021, 1, 1, 0, 0, 0);
		private uint currentTGOffset = 0;
		private const uint TGTimerOffset = 117 + 240; // 19,5 минут + 40 минут
		private const uint TGMessagesPerDay = 4;
#endif

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		public UniNotifierForm ()
			{
			// Инициализация
			InitializeComponent ();

			this.Text = ProgramDescription.AssemblyVisibleName;
			this.CancelButton = BClose;
			MainText.Font = new Font (SystemFonts.DialogFont.FontFamily.Name, 13);

			ReloadNotificationsList ();
			ResetCulture ();
#if TGB
			GetGMJ.Visible = (al == SupportedLanguages.ru_ru);
#else
			GetGMJ.Visible = false;
#endif

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

#if TG
				currentTGCount = uint.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, "TGCount",
					"").ToString ());
				currentTGTimeStamp = DateTime.Parse (Registry.GetValue (ProgramDescription.AssemblySettingsKey, "TGTimeStamp",
					"").ToString ());
#endif
				}
			catch
				{
				}

			// Настройка иконки в трее
			ni.Icon = Properties.GMJNotifier.GMJNotifier16;
			ni.Text = ProgramDescription.AssemblyVisibleName;
			ni.Visible = true;

			ni.ContextMenu = new ContextMenu ();

			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption02", al), ShowSettings));
			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption03", al), AboutService));
			ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption04", al), CloseService));

			ni.MouseDown += ShowHideFullText;
			ni.ContextMenu.MenuItems[2].DefaultItem = true;

			if (!File.Exists (startupLink))
				ni.ContextMenu.MenuItems.Add (new MenuItem (Localization.GetText ("MainMenuOption05", al), AddToStartup));
			}

		private void UniNotifierForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			this.Hide ();

			// Запуск
			MainTimer.Interval = (int)ProgramDescription.MasterFrameLength * 4;
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

#if TG
		private string[][] webReplacements = new string[][] {
			new string[] { "%", "%25" },

			new string[] { "\t", "%20" },
			new string[] { " ", "%20" },
			new string[] { "\n", "%0A" },
			new string[] { "\r", "" },

			new string[] { "!", "%21" },
			new string[] { "\"", "%22" },
			new string[] { "&", "%26" },
			new string[] { "'", "%27" },
			new string[] { "(", "%28" },
			new string[] { ")", "%29" },
			new string[] { "*", "%2A" },
			new string[] { ",", "%2C" },
			new string[] { "-", "%2D" },
			new string[] { ".", "%2E" },
			new string[] { "/", "%2F" },
			new string[] { ":", "%3A" },
			new string[] { ";", "%3B" },
			new string[] { "<", "%3C" },
			new string[] { "=", "%3D" },
			new string[] { ">", "%3E" },
			new string[] { "?", "%3F" },
			new string[] { "\\", "%5C" },
			new string[] { "_", "%5F" },

			new string[] { "%20%0A", "%0A" },
			};
#endif

		// Итерация таймера обновления
		private void MainTimer_Tick (object sender, EventArgs e)
			{
			// Запуск запроса
			HardWorkExecutor hwe = new HardWorkExecutor (DoUpdate, null, null, false, false);
			hwe.Dispose ();

			// Обновление очереди отображения
			if (texts.Count > 0)
				{
				// Добавление в главное окно
				if (MainText.Text.Length + texts[0].Length > ProgramDescription.MasterLogMaxLength)
					MainText.Text = MainText.Text.Substring (texts[0].Length, MainText.Text.Length - texts[0].Length);
				if (MainText.Text.Length > 0)
					MainText.AppendText ("\r\n\r\n");
				if (DateTime.Today > ProgramDescription.LastNotStamp)
					{
					ProgramDescription.LastNotStamp = DateTime.Today;
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
#if TG
			else if (currentTGOffset++ >= TGTimerOffset)
				{
				// Контроль состояния переменных
				currentTGOffset = 0;
				if (currentTGTimeStamp != DateTime.Today)
					{
					currentTGTimeStamp = DateTime.Today;
					try
						{
						Registry.SetValue (ProgramDescription.AssemblySettingsKey, "TGTimeStamp",
							currentTGTimeStamp.ToString ());
						}
					catch { }
					currentTGCount = 0;
					}

				if (currentTGCount >= TGMessagesPerDay)
					return;

				// Запрос сообщения
				string s = GMJ.GetRandomGMJ ();
				if (s == "")
					{
					texts.Add ("Сбой ретрансляции. Проверьте соединение с интернетом");
					notNumbers.Add (0);
					return;
					}

				if (s.Contains ("joy не вернула сообщение"))
					{
					StreamWriter SW = File.AppendText (AboutForm.AppStartupPath + "TG.log");
					SW.Write (s + "\r\n\r\n");
					SW.Close ();

					texts.Add ("При ретрансляции найдено проблемное сообщение. Проверьте лог приложения");
					notNumbers.Add (0);
					return;
					}

				// Ретрансляция
				s = NotificationsSet.HeaderBeginning + s.Substring (s.IndexOf (NotificationsSet.HeaderMiddle) +
					NotificationsSet.HeaderMiddle.Length);
				for (int i = 0; i < webReplacements.Length; i++)
					s = s.Replace (webReplacements[i][0], webReplacements[i][1]);

				AboutForm.GetHTML (ProgramDescription.TGTokenLine + s);

				// Сохранение состояния
				currentTGCount++;
				try
					{
					Registry.SetValue (ProgramDescription.AssemblySettingsKey, "TGCount", currentTGCount.ToString ());
					}
				catch { }
				}
#endif
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

		// Отображение / скрытие полного списка оповещений
		private void ShowHideFullText (object sender, MouseEventArgs e)
			{
			if (e.Button != MouseButtons.Left)
				return;

			if (this.Visible)
				{
				this.Close ();
				}
			else
				{
				this.Show ();
				this.TopMost = true;
				this.TopMost = false;
				}
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

			for (int i = 0; i < ni.ContextMenu.MenuItems.Count; i++)
				ni.ContextMenu.MenuItems[i].Text = Localization.GetText ("MainMenuOption" + (i + 2).ToString ("D02"), al);

			// Перезапуск
			ns.ResetTimer (true);
			MainTimer.Enabled = true;
			}

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.GoToButton);
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
#if TGB
			GetGMJ.Enabled = false;
			string s = GMJ.GetRandomGMJ ();

			if (s != "")
				texts.Add (s);
			else
				texts.Add ("Новый список сформирован"); //("GMJ не вернула сообщение. Проверьте интернет-соединение");
			notNumbers.Add (0);
#endif
			}
		}
	}
