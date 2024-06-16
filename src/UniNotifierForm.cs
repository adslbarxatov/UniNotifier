using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
		private bool callWindowOnUrgents = false;
		private bool allowExit = false;
		/*private string[] regParameters = new string[] {
			"Left",
			"Top",
			"Width",
			"Height",
			"Read",
			"CallOnUrgents",
			"FontSize",
			};*/

		private NotificationsSet ns = new NotificationsSet (true);
		private List<string> texts = new List<string> ();
		private List<int> notNumbers = new List<int> ();
		private bool hideWindow;

#if TGT
		private uint tgtCounter = 0;
#endif

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		public UniNotifierForm (bool HideWindow)
			{
			// Инициализация
			InitializeComponent ();

			this.Text = ProgramDescription.AssemblyVisibleName;
			this.CancelButton = BClose;
			MainText.Font = new Font ("Calibri", 13);
			if (!RDGenerics.AppHasAccessRights (false, false))
				this.Text += RDLocale.GetDefaultText (RDLDefaultTexts.Message_LimitedFunctionality);
			hideWindow = HideWindow;

			ReloadNotificationsList ();
#if TGT
			GetGMJ.Visible = false;
#else
			GetGMJ.Visible = RDLocale.IsCurrentLanguageRuRu;
#endif

			// Получение настроек
			RDGenerics.LoadWindowDimensions (this);
			ReadMode.Checked = RDGenerics.GetSettings (readPar, false);
			callWindowOnUrgents = RDGenerics.GetSettings (callWindowOnUrgentsPar, false);
			try
				{
				FontSizeField.Value = RDGenerics.GetSettings (fontSizePar, 130) / 10.0m;
				}
			catch { }

			// Настройка иконки в трее
			ni.Icon = Properties.GMJNotifier.GMJNotifier16;
			ni.Text = ProgramDescription.AssemblyVisibleName;
			ni.Visible = true;

			ni.ContextMenu = new ContextMenu ();

			ni.ContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("MainMenuOption02"), ShowSettings));
			ni.ContextMenu.MenuItems[0].Enabled = RDGenerics.AppHasAccessRights (false, true);

			ni.ContextMenu.MenuItems.Add (new MenuItem (
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout), AboutService));
			ni.ContextMenu.MenuItems.Add (new MenuItem (
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit), CloseService));

			ni.MouseDown += ShowHideFullText;
			ni.ContextMenu.MenuItems[2].DefaultItem = true;
			}

		private void UniNotifierForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			UniNotifierForm_Resize (null, null);
			if (hideWindow)
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
			RDGenerics.ShowAbout (false);
			}

		// Итерация таймера обновления
#if TGT
		private bool tgtInProgress = false;
#endif

		private void MainTimer_Tick (object sender, EventArgs e)
			{
			// Переменные
			int spl;
			string hdr, txt;

#if TGT
			if (tgtInProgress)
				return;
#endif

			// Запуск запроса
			RDGenerics.RunWork (DoUpdate, null, null, RDRunWorkFlags.DontSuspendExecution);

#if TGT
			// Раз в 13 минут (1000 * 60 * 13)
			if (++tgtCounter * MainTimer.Interval >= 780000)
				{
				tgtInProgress = true;
				tgtCounter = 0;

				GetGMJ_Click (null, null);
				tgtInProgress = false;
				}
#endif

			// Обновление очереди отображения
			if (texts.Count > 0)
				{
				// Добавление в главное окно
				if ((MainText.Text.Length + texts[0].Length > ProgramDescription.MasterLogMaxLength) &&
					(MainText.Text.Length > texts[0].Length))   // Бывает и так
					MainText.Text = MainText.Text.Substring (texts[0].Length, MainText.Text.Length - texts[0].Length);
				if (MainText.Text.Length > 0)
					MainText.AppendText (RDLocale.RNRN + RDLocale.RN);

				if (DateTime.Today > ProgramDescription.LastNotStamp)
					{
					ProgramDescription.LastNotStamp = DateTime.Today;

					var ci = RDLocale.GetCulture (RDLocale.CurrentLanguage);
					MainText.AppendText (RDLocale.RN + "--- " +
						DateTime.Today.ToString (ci.DateTimeFormat.LongDatePattern, ci) +
						" ---" + RDLocale.RNRN);
					}

				// Добавление и форматирование
				MainText.AppendText (texts[0].Replace (NotificationsSet.MainLogItemSplitter.ToString (),
					RDLocale.RN));

				// Отображение всплывающего сообщения
				if (!this.Visible)
					{
					try
						{
						spl = texts[0].IndexOf (NotificationsSet.MainLogItemSplitter);
						hdr = texts[0].Substring (0, spl);
						txt = texts[0].Substring (spl + 1);

						if (txt.Length > 210)
							txt = txt.Substring (0, 210) + "...";

						ni.ShowBalloonTip (10000, hdr, txt, ns.HasUrgentNotifications ? ToolTipIcon.Warning :
							ToolTipIcon.Info);
						}
					catch { }
					}

				// Обновление прочих полей
				NamesCombo.SelectedIndex = notNumbers[0];

				texts.RemoveAt (0);
				notNumbers.RemoveAt (0);

				GetGMJ.Enabled = true;
				}

			// Срочные оповещения
			if (ns.HasUrgentNotifications && callWindowOnUrgents)
				{
				ns.HasUrgentNotifications = false;

				this.Show ();
				this.TopMost = true;
				this.TopMost = false;
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

		// Отображение / скрытие полного списка оповещений
		private void ShowHideFullText (object sender, MouseEventArgs e)
			{
			// Работа только с левой кнопкой мыши
			if (e.Button != MouseButtons.Left)
				return;

			// Отмена состояния сообщений
			ns.HasUrgentNotifications = false;

			// Обработка состояния
			if (this.Visible)
				{
				this.Close ();
				}
			else
				{
				this.Show ();
				this.TopMost = true;
				this.TopMost = false;
				MainText.ScrollToCaret ();
				}
			}

		// Отображение окна настроек
		private void ShowSettings (object sender, EventArgs e)
			{
			// Пауза
			MainTimer.Enabled = false;

			// Настройка
			SettingsForm sf = new SettingsForm (ns, (uint)MainTimer.Interval *
				NotificationsSet.MaxNotifications / 60000, callWindowOnUrgents);

			// Запоминание настроек
			callWindowOnUrgents = sf.CallWindowOnUrgents;
			RDGenerics.SetSettings (callWindowOnUrgentsPar, callWindowOnUrgents);

			bool complete = sf.CompleteUpdate;
			sf.Dispose ();

			// Обработка случая закрытия основного окна из трея
			if (allowExit)
				return;

			// Обновление настроек
			ReloadNotificationsList ();
			ns.HasUrgentNotifications = false;

			ni.ContextMenu.MenuItems[0].Text = RDLocale.GetText ("MainMenuOption02");
			ni.ContextMenu.MenuItems[1].Text =
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout);
			ni.ContextMenu.MenuItems[2].Text =
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);
			if (ni.ContextMenu.MenuItems.Count > 3)
				ni.ContextMenu.MenuItems[3].Text = RDLocale.GetText ("MainMenuOption05");

			// Перезапуск
			ns.ResetTimer (complete);   // Раньше имел смысл обязательный полный сброс. Теперь это уже неактуально
			MainTimer.Enabled = true;
			}
		private const string callWindowOnUrgentsPar = "CallOnUrgents";

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.GoToButton);
			RDGenerics.RunURL (ns.Notifications[NamesCombo.SelectedIndex].Link);
			this.Close ();
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			// Отмена состояния сообщений
			ns.HasUrgentNotifications = false;

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
			RDGenerics.SetSettings (readPar, ReadMode.Checked);
			}
		private const string readPar = "Read";

		// Изменение размера формы
		private void UniNotifierForm_Resize (object sender, EventArgs e)
			{
			MainText.Width = this.Width - 38;
			MainText.Height = this.Height - 87;

			ButtonsPanel.Top = MainText.Top + MainText.Height - 1;
			}

		// Сохранение размера формы
		private void UniNotifierForm_ResizeEnd (object sender, EventArgs e)
			{
			RDGenerics.SaveWindowDimensions (this);
			}

		// Запрос сообщения от GMJ
		private void GetGMJ_Click (object sender, EventArgs e)
			{
			GetGMJ.Enabled = false;
			string s = GMJ.GetRandomGMJ ();

			if (s != "")
				{
#if TGT
				if (s.Contains (GMJ.SourceNoReturnPattern))
					texts.Add ("!!! " + s + " !!!");
				else
					texts.Add (s);
#else
				texts.Add (s);
#endif
				}
			else
				{
#if TGB
				texts.Add ("Новый список сформирован");
#else
				texts.Add ("GMJ не вернула сообщение. Проверьте интернет-соединение");
#endif
				}
			notNumbers.Add (0);

#if TGT
			if (s.Contains (GMJ.SourceNoReturnPattern))
				ns.HasUrgentNotifications = true;
#endif
			}

		// Изменение размера шрифта
		private void FontSizeField_ValueChanged (object sender, EventArgs e)
			{
			MainText.Font = new Font (MainText.Font.FontFamily, (float)FontSizeField.Value);
			RDGenerics.SetSettings (fontSizePar, (uint)(FontSizeField.Value * 10.0m));
			}
		private const string fontSizePar = "FontSize";
		}
	}
