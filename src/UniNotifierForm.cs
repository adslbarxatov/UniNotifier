﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private bool allowExit = false;

		private NotificationsSet ns = new NotificationsSet (true);
		private List<string> texts = new List<string> ();
		private List<int> notNumbers = new List<int> ();
		private List<int> senders = new List<int> ();
		private bool hideWindow;

		private ContextMenu bColorContextMenu;
		private ContextMenu notContextMenu;
		private ContextMenu textContextMenu;
		private int textContextSender;

		private const string fontFamily = "Calibri";
		private const int transculencyAmount = 15;

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

			if (!RDGenerics.AppHasAccessRights (false, false))
				this.Text += RDLocale.GetDefaultText (RDLDefaultTexts.Message_LimitedFunctionality);
			hideWindow = HideWindow;

			ReloadNotificationsList ();

			// Получение настроек
			RDGenerics.LoadWindowDimensions (this);

			BColor_ItemClicked (null, null);    // Подгрузка настройки
			try
				{
				FontSizeField.Value = NotificationsSupport.LogFontSize / 10.0m;
				}
			catch { }

			// Настройка иконки в трее
			ni.Icon = Properties.UniNotifier.UniNotifierTrayN;
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

#if TGB
			GetGMJ_Click (null, null);
#endif
			}

		// Обновление списка оповещений в главном окне
		private void ReloadNotificationsList ()
			{
			// Локализация зависимой части интерфейса
			BGo.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo);
			FontLabel.Text = RDLocale.GetText ("FontLabel");

			if (textContextMenu == null)
				textContextMenu = new ContextMenu ();

			textContextMenu.MenuItems.Clear ();
			textContextMenu.MenuItems.Add (new MenuItem (BGo.Text,
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetDefaultText (RDLDefaultTexts.Button_Copy),
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem ("-"));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("DeleteContextMenu"),
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("DisableContextMenu"),
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("SetupContextMenu"),
				TextContext_ItemClicked));

			if (notContextMenu == null)
				notContextMenu = new ContextMenu ();

			notContextMenu.MenuItems.Clear ();
			foreach (Notification n in ns.Notifications)
				notContextMenu.MenuItems.Add (new MenuItem (n.Name, GoToLink_ItemClicked));
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
				// Добавление и форматирование
				AddTextToLayout (texts[0].Replace (NotificationsSupport.MainLogItemSplitter.ToString (),
					RDLocale.RN));

				// Отображение всплывающего сообщения
				if (!this.Visible)
					{
					try
						{
						spl = texts[0].IndexOf (NotificationsSupport.MainLogItemSplitter);
						hdr = texts[0].Substring (0, spl);
						txt = texts[0].Substring (spl + 1);

						if (txt.Length > 210)
							txt = txt.Substring (0, 210) + "...";

						if (ns.HasUrgentNotifications)
							{
							ni.ShowBalloonTip (10000, hdr, txt, ToolTipIcon.Warning);
							ni.Icon = Properties.UniNotifier.UniNotifierTrayW;
							}
						else
							{
							ni.ShowBalloonTip (10000, hdr, txt, ToolTipIcon.Info);
							ni.Icon = Properties.UniNotifier.UniNotifierTrayN;
							}
						}
					catch { }
					}

				texts.RemoveAt (0);
				senders.Add (notNumbers[0]);
				notNumbers.RemoveAt (0);
				}

			// Срочные оповещения
			if (ns.HasUrgentNotifications && NotificationsSupport.CallWindowOnUrgents)
				{
				SetUrgency (false);

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

		// Метод устанавливает новое состояние срочности для набора уведомлений
		private void SetUrgency (bool Urgent)
			{
			ns.HasUrgentNotifications = Urgent;
			try
				{
				ni.Icon = Urgent ? Properties.UniNotifier.UniNotifierTrayW : Properties.UniNotifier.UniNotifierTrayN;
				}
			catch { }
			}

		// Отображение / скрытие полного списка оповещений
		private void ShowHideFullText (object sender, MouseEventArgs e)
			{
			// Работа только с левой кнопкой мыши
			if (e.Button != MouseButtons.Left)
				return;

			// Отмена состояния сообщений
			SetUrgency (false);

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
				ScrollLog ();
				}
			}

		// Метод прокручивает журнал к последней записи
		private void ScrollLog ()
			{
			if (MainLayout.Controls.Count > 0)
				MainLayout.ScrollControlIntoView (MainLayout.Controls[MainLayout.Controls.Count - 1]);
			}

		// Отображение окна настроек
		private void ShowSettings (object sender, EventArgs e)
			{
			// Пауза
			MainTimer.Enabled = false;

			// Настройка
			SettingsForm sf = new SettingsForm (ns, (uint)MainTimer.Interval *
				NotificationsSet.MaxNotifications / 60000,
				sender == null ? senders[textContextSender] : -1);

			// Запоминание настроек
			bool complete = sf.CompleteUpdate;
			sf.Dispose ();

			// Обработка случая закрытия основного окна из трея
			if (allowExit)
				return;

			// Обновление настроек
			ReloadNotificationsList ();
			SetUrgency (false);

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

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.GoToButton);
			notContextMenu.Show (BGo, Point.Empty);
			}

		private void GoToLink_ItemClicked (object sender, EventArgs e)
			{
			int idx = notContextMenu.MenuItems.IndexOf ((MenuItem)sender);
			if (idx >= 0)
				RDGenerics.RunURL (ns.Notifications[idx].Link);

			this.Close ();
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			// Отмена состояния сообщений
			SetUrgency (false);

			this.Close ();
			}

		// Изменение размера формы
		private void UniNotifierForm_Resize (object sender, EventArgs e)
			{
			MainLayout.Width = this.Width - 38;
			MainLayout.Height = this.Height - ButtonsPanel.Height - 53;

			ButtonsPanel.Top = MainLayout.Top + MainLayout.Height + 1;
			}

		// Сохранение размера формы
		private void UniNotifierForm_ResizeEnd (object sender, EventArgs e)
			{
			// Сохранение настроек
			RDGenerics.SaveWindowDimensions (this);

			// Пересчёт размеров элементов
			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];
				l.AutoSize = false;
				l.MaximumSize = l.MinimumSize = LogSizeLimit;
				l.AutoSize = true;
				}
			}
		private Size LogSizeLimit
			{
			get
				{
				return new Size (MainLayout.Width - 6 - 18, 0);
				}
			}

		// Запрос сообщения от GMJ
		private void GetGMJ_Click (object sender, EventArgs e)
			{
#if TGB || TGT
			string s = GMJ.GetRandomGMJ ();

			if (s != "")
				{
#if TGT
				if (s.Contains (GMJ.SourceNoReturnPattern))
					{
					texts.Add ("!!! " + s + " !!!");
					SetUrgency (true);
					}
				else
					{
					texts.Add (s);
					}
#else
				texts.Add (s);
#endif
				}
			else
				{
#if TGB
				texts.Add ("Новый список сформирован");
#else
				texts.Add ("GMJ не отвечает на запрос. Проверьте интернет-соединение");
#endif
				}
			notNumbers.Add (0);

#if TGB
			ni.ShowBalloonTip (10000, ProgramDescription.AssemblyVisibleName,
				texts[texts.Count - 1], ToolTipIcon.Info);
			CloseService (null, null);
#endif
#endif
			}

		// Метод добавляет этемент в MainLayout
		private void AddTextToLayout (string Text)
			{
			// Формирование контрола
			Label l = new Label ();
			l.AutoSize = false;

			if (Text.Contains (NotificationsSet.EmergencySign))
				{
				l.BackColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.WarningMessage);
				l.ForeColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.DefaultText);

				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BorderStyle = BorderStyle.FixedSingle;
				}
			else
				{
				l.ForeColor = NotificationsSupport.LogColors.CurrentColor.MainTextColor;

				int amount = NotificationsSupport.TranslucentLogItems ? transculencyAmount : 0;
				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BackColor = Color.FromArgb (amount, 0, 0, 0);
				else
					l.BackColor = Color.FromArgb (amount, 255, 255, 255);
				}

			l.Click += TextLabel_Clicked;
			l.Font = new Font (fontFamily, (float)FontSizeField.Value);
			l.Text = Text;
			l.Margin = LogItemMargin;

			l.MaximumSize = l.MinimumSize = LogSizeLimit;
			l.AutoSize = true;

			// Добавление
			MainLayout.Controls.Add (l);

			while (MainLayout.Controls.Count > NotificationsSupport.MasterLogMaxItems)
				{
				MainLayout.Controls.RemoveAt (0);
				senders.RemoveAt (0);
				}

			// Прокрутка
			ScrollLog ();
			}

		// Нажатие на элемент журнала
		private void TextLabel_Clicked (object sender, EventArgs e)
			{
			Label l = (Label)sender;
			textContextSender = MainLayout.Controls.IndexOf (l);

			textContextMenu.Show (l, Point.Empty);
			}

		// Выбор варианта в меню
		private void TextContext_ItemClicked (object sender, EventArgs e)
			{
			int idx = textContextMenu.MenuItems.IndexOf ((MenuItem)sender);
			if (textContextSender < 0)
				return;

			switch (idx)
				{
				// Переход по ссылке
				case 0:
					RDGenerics.RunURL (ns.Notifications[senders[textContextSender]].Link);
					this.Close ();
					break;

				// Копирование текста
				case 1:
					RDGenerics.SendToClipboard (((Label)MainLayout.Controls[textContextSender]).Text +
						RDLocale.RNRN + ns.Notifications[senders[textContextSender]].Link, true);
					break;

				// Удаление текста
				case 3:
					MainLayout.Controls.RemoveAt (textContextSender);
					senders.RemoveAt (textContextSender);
					break;

				// Отключение уведомления
				case 4:
					ns.Notifications[senders[textContextSender]].IsEnabled = false;
					break;

				// Настройка уведомления
				case 5:
					ShowSettings (null, null);
					break;
				}
			}

		// Изменение размера шрифта
		private void FontSizeField_ValueChanged (object sender, EventArgs e)
			{
			NotificationsSupport.LogFontSize = (uint)(FontSizeField.Value * 10.0m);
			Font fnt = new Font (fontFamily, (float)FontSizeField.Value);

			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];
				l.Font = fnt;
				l.Margin = LogItemMargin;
				}
			}
		private Padding LogItemMargin
			{
			get
				{
				return new Padding (3, 3, 3, (int)NotificationsSupport.LogFontSize /
					(NotificationsSupport.TranslucentLogItems ? 8 : 4));
				}
			}

		// Выбор цвета журнала
		private void BColor_Clicked (object sender, EventArgs e)
			{
			// Создание вызывающего контекстного меню
			if (bColorContextMenu == null)
				{
				bColorContextMenu = new ContextMenu ();

				for (int i = 0; i < NotificationsSupport.LogColors.ColorNames.Length; i++)
					bColorContextMenu.MenuItems.Add (new MenuItem (NotificationsSupport.LogColors.ColorNames[i],
						BColor_ItemClicked));

				bColorContextMenu.MenuItems.Add (new MenuItem ("-"));
				bColorContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("TransculentContextMenu"),
					BColor_ItemClicked));
				bColorContextMenu.MenuItems[bColorContextMenu.MenuItems.Count - 1].Checked =
					NotificationsSupport.TranslucentLogItems;
				}

			// Вызов
			if (sender != null)
				bColorContextMenu.Show (LogColor, Point.Empty);
			}

		private void BColor_ItemClicked (object sender, EventArgs e)
			{
			// Извлечение индекса
			int idx;
			if (sender == null)
				idx = (int)NotificationsSupport.LogColor;
			else
				idx = bColorContextMenu.MenuItems.IndexOf ((MenuItem)sender);

			// Сохранение
			if (idx < NotificationsSupport.LogColors.ColorNames.Length)
				{
				NotificationsSupport.LogColor = (uint)idx;
				}
			else
				{
				MenuItem mi = bColorContextMenu.MenuItems[bColorContextMenu.MenuItems.Count - 1];
				mi.Checked = !mi.Checked;
				NotificationsSupport.TranslucentLogItems = mi.Checked;

				FontSizeField_ValueChanged (null, null);
				}

			// Установка значений
			MainLayout.BackColor = NotificationsSupport.LogColors.CurrentColor.BackColor;
			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];

				int amount = NotificationsSupport.TranslucentLogItems ? transculencyAmount : 0;
				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BackColor = Color.FromArgb (amount, 0, 0, 0);
				else
					l.BackColor = Color.FromArgb (amount, 255, 255, 255);

				l.ForeColor = NotificationsSupport.LogColors.CurrentColor.MainTextColor;
				}
			}
		}
	}
